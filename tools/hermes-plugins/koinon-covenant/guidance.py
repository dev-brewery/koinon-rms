"""Guidance — the mandates a session sees, and R5's evidence warning.

Enforcement (enforcement.py) stops the wrong action. Guidance makes the right
action legible: the dev-cycle mandates are injected once per session and again
immediately after any block, so a blocked agent is told not just "no" but what
the cycle actually requires.

R5 ships in WARN mode by design. Completion-claim detection has a real
false-positive rate, and over-blocking is its own drift (#7 escalation-as-
avoidance). The warning is appended to the tool result, so the model confronts
it on the next turn. Promote to a block only once the signal is measured.
"""

from __future__ import annotations

import json
import os
import re
import sqlite3
import threading
from datetime import datetime, timedelta, timezone
from pathlib import Path

import mandates
from paths import repo_root

_state_lock = threading.Lock()
_injected_sessions: set[str] = set()
_replay_sessions: set[str] = set()

# --- R5: claims that assert a verification tier ------------------------------
_CLAIM = re.compile(
    r"\b(?:verified|merge[- ]ready|ready to merge|qa[- ]passed|"
    r"all tests? pass(?:ing|ed)?|works? (?:end[- ]to[- ]end|in the browser)|"
    r"confirmed working|done and tested|fully tested)\b",
    re.I,
)
_CLAIM_TOOLS = frozenset({"send_message", "kanban", "todo", "delegate_task"})

EVIDENCE_WINDOW = timedelta(hours=12)
DEMO_EVIDENCE_MAX_AGE = timedelta(days=3)


def mark_blocked(session_id: str) -> None:
    """Called by the enforcement hook so the next turn re-states the mandates."""
    with _state_lock:
        _replay_sessions.add(session_id or "")


# ---------------------------------------------------------------------------
# pre_llm_call — inject the mandates
# ---------------------------------------------------------------------------
def pre_llm_call(**kwargs):
    session_id = str(kwargs.get("session_id") or "")
    with _state_lock:
        first_time = session_id not in _injected_sessions
        replay = session_id in _replay_sessions
        if not first_time and not replay:
            return None
        _injected_sessions.add(session_id)
        _replay_sessions.discard(session_id)

    try:
        text = mandates.digest()
    except Exception:
        return None
    if not text:
        return None
    prefix = (
        "You were just blocked by the koinon-covenant harness. Re-read what the "
        "dev cycle requires before your next action:\n\n"
        if replay
        else ""
    )
    return {"context": prefix + text}


# ---------------------------------------------------------------------------
# pre_verify — do not finish a code edit with nothing observed
# ---------------------------------------------------------------------------
def _recent_evidence(session_id: str) -> list[tuple[str, str, str]]:
    """(kind, scope, status) rows recorded for this session inside the window."""
    home = os.environ.get("HERMES_HOME") or str(Path.home() / ".hermes")
    db = Path(home) / "verification_evidence.db"
    if not db.exists():
        return []
    cutoff = (datetime.now(timezone.utc) - EVIDENCE_WINDOW).isoformat()
    try:
        conn = sqlite3.connect(f"file:{db}?mode=ro", uri=True, timeout=5)
        try:
            rows = conn.execute(
                "SELECT kind, scope, status FROM verification_events "
                "WHERE session_id = ? AND created_at >= ? ORDER BY id DESC LIMIT 25",
                (session_id, cutoff),
            ).fetchall()
        finally:
            conn.close()
        return [(str(a), str(b), str(c)) for a, b, c in rows]
    except Exception:
        return []


def pre_verify(**kwargs):
    if not kwargs.get("coding"):
        return None
    if kwargs.get("attempt"):  # Hermes already nudged this turn; do not pile on
        return None
    session_id = str(kwargs.get("session_id") or "")
    changed = [p for p in (kwargs.get("changed_paths") or []) if p]
    if not changed:
        return None
    if _recent_evidence(session_id):
        return None
    listed = ", ".join(changed[:5]) + (" …" if len(changed) > 5 else "")
    return {
        "action": "continue",
        "message": (
            f"[koinon-covenant R5] You edited code ({listed}) and there is no "
            "verification evidence recorded for this session. Run the relevant "
            "check now — build, the targeted test, or the E2E path — and let it "
            "finish, so the evidence ledger records what you actually proved. "
            "If you have a reason to defer, say which check you are deferring and "
            "why. 'It looks right' is not verification."
        ),
    }


# ---------------------------------------------------------------------------
# R5 — warn when a completion claim has no evidence behind it
# ---------------------------------------------------------------------------
def _qa_callback_evidence() -> tuple[bool, str]:
    """Fresh QA-instance evidence in .qa-callbacks/? (fresh, detail)."""
    directory = repo_root() / ".qa-callbacks"
    try:
        entries = [p for p in directory.iterdir() if p.is_file()]
    except Exception:
        return False, "no .qa-callbacks/ directory"
    if not entries:
        return False, "no QA callbacks recorded"
    newest = max(entries, key=lambda p: p.stat().st_mtime)
    age = datetime.now(timezone.utc) - datetime.fromtimestamp(
        newest.stat().st_mtime, tz=timezone.utc
    )
    if age > DEMO_EVIDENCE_MAX_AGE:
        return False, f"newest QA callback {newest.name} is {age.days}d old (stale)"
    return True, f"{newest.name} ({int(age.total_seconds() // 3600)}h old)"


def _claim_text(tool_name: str, args: dict) -> str:
    if not isinstance(args, dict):
        return ""
    parts = []
    for key in ("message", "text", "content", "body", "summary", "note", "description"):
        val = args.get(key)
        if isinstance(val, str):
            parts.append(val)
    return "\n".join(parts)


def transform_tool_result(**kwargs):
    tool_name = str(kwargs.get("tool_name") or "")
    if tool_name not in _CLAIM_TOOLS:
        return None
    text = _claim_text(tool_name, kwargs.get("args") or {})
    if not text or not _CLAIM.search(text):
        return None

    session_id = str(kwargs.get("session_id") or "")
    dev_rows = _recent_evidence(session_id)
    qa_fresh, qa_detail = _qa_callback_evidence()
    if qa_fresh:
        return None  # QA-tier evidence exists; the claim is supported

    claim = _CLAIM.search(text).group(0)
    tier = (
        f"dev-tier only ({len(dev_rows)} recorded check(s) this session)"
        if dev_rows
        else "no verification evidence at all this session"
    )
    warning = (
        f"\n\n⚠️ [koinon-covenant R5 — WARNING, not a block] You claimed "
        f"\"{claim}\" but there is {tier}, and QA-instance evidence is missing: "
        f"{qa_detail}.\n"
        "Dev-VM tests are dev-tier evidence only. 'Verified' and 'merge-ready' "
        "require QA-stack evidence per the qa:koinon-pr-qa protocol (Phase 0-E), "
        "landed in .qa-callbacks/. Either run the QA path and re-state the claim "
        "with the evidence, or downgrade the claim to what you actually proved."
    )
    result = kwargs.get("result")
    if isinstance(result, str):
        return result + warning
    try:
        return json.dumps(result, ensure_ascii=False) + warning
    except Exception:
        return str(result) + warning
