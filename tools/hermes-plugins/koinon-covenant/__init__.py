"""koinon-covenant — Koinon RMS dev-cycle enforcement and guidance for Hermes.

Wires five surfaces:

* ``pre_tool_call``       R1-R4 blocks (fail-closed)
* ``tool_request``        path normalisation, so the rules see the real target
* ``pre_llm_call``        the dev-cycle mandates, from the OpenViking store
* ``pre_verify``          no finishing a code edit with nothing observed
* ``transform_tool_result`` R5 evidence warning (warn-mode)

and one tool, ``architect_review``, which carries the retrieved mandates into
the ruling so the architect judges against what the dev cycle actually requires.

The rules this enforces are not this plugin's opinion: they are the mandates in
``seed/dev-cycle-mandates.md``, each traceable to a specific incident.
"""

from __future__ import annotations

import logging
import os
import subprocess
import sys
from pathlib import Path

_HERE = Path(__file__).resolve().parent
if str(_HERE) not in sys.path:
    sys.path.insert(0, str(_HERE))

import enforcement  # noqa: E402
import guidance  # noqa: E402
import mandates  # noqa: E402
from paths import repo_root  # noqa: E402

logger = logging.getLogger(__name__)

ARCHITECT_TIMEOUT_SECONDS = 900


# ---------------------------------------------------------------------------
# Hooks
# ---------------------------------------------------------------------------
def _pre_tool_call(**kwargs):
    tool_name = str(kwargs.get("tool_name") or "")
    args = kwargs.get("args") or {}
    verdict = enforcement.evaluate(tool_name, args)
    if verdict:
        guidance.mark_blocked(str(kwargs.get("session_id") or ""))
        logger.warning("koinon-covenant blocked %s: %s", tool_name, verdict["message"][:160])
    return verdict


def _post_tool_call(**kwargs):
    """Observer only — an audit trail of what the gate saw."""
    if str(kwargs.get("status") or "") == "blocked":
        logger.info(
            "koinon-covenant audit: %s blocked (session=%s)",
            kwargs.get("tool_name"),
            kwargs.get("session_id"),
        )
    return None


def _tool_request(**kwargs):
    """Normalise path arguments before rules, approvals, and guardrails see them.

    Middleware runs BEFORE the pre-execution path by contract, so a relative or
    ``~``-prefixed path is resolved here and every downstream check evaluates the
    real target rather than the string the model happened to type.
    """
    tool_name = str(kwargs.get("tool_name") or "")
    if tool_name not in ("write_file", "patch", "skill_manage"):
        return None
    args = dict(kwargs.get("args") or {})
    changed = False
    for key in ("path", "file_path"):
        val = args.get(key)
        if not isinstance(val, str) or not val.strip():
            continue
        expanded = os.path.expanduser(val.strip())
        if not os.path.isabs(expanded):
            expanded = str((repo_root() / expanded))
        resolved = os.path.normpath(expanded)
        if resolved != val:
            args[key] = resolved
            changed = True
    if not changed:
        return None
    return {
        "args": args,
        "source": "koinon-covenant",
        "reason": "resolved path so policy evaluates the real target",
    }


# ---------------------------------------------------------------------------
# architect_review tool — the mandate feed into the ruling
# ---------------------------------------------------------------------------
ARCHITECT_SCHEMA = {
    "type": "object",
    "properties": {
        "files": {
            "type": "string",
            "description": "Comma-separated repo-relative paths the change touches.",
        },
        "deduced": {
            "type": "string",
            "description": "Your diagnosis: what is wrong and why, from what you retrieved.",
        },
        "proposed": {
            "type": "string",
            "description": "The specific fix you propose, concretely.",
        },
        "issue": {
            "type": "string",
            "description": "Linked GitHub issue number; the architect reads its acceptance criteria.",
        },
    },
    "required": ["files", "deduced", "proposed"],
}


def _architect_review(files: str, deduced: str, proposed: str, issue: str = "", **_):
    """Submit a change for isolated architect review, mandates attached."""
    script = repo_root() / "scripts" / "hooks" / "architect-review.mjs"
    if not script.exists():
        return (
            f"HALT: {script} not found. The architect review is required before any "
            "code change and cannot be skipped. Summon the owner."
        )

    try:
        mandate_text = mandates.architect_digest()
        source = mandates.source()
    except Exception as exc:
        mandate_text, source = "", f"unavailable ({exc})"

    cmd = [
        "node", str(script),
        "--files", files,
        "--deduced", deduced,
        "--proposed", proposed,
    ]
    if mandate_text:
        cmd += ["--mandates", mandate_text]
    if issue:
        cmd += ["--issue", str(issue)]

    try:
        out = subprocess.run(
            cmd, cwd=str(repo_root()), capture_output=True, text=True,
            timeout=ARCHITECT_TIMEOUT_SECONDS,
        )
    except subprocess.TimeoutExpired:
        return (
            "HALT: the architect review timed out. Do not proceed on presumption — "
            "quality is the invariant. Summon the owner."
        )
    except Exception as exc:
        return f"HALT: could not run the architect review ({exc}). Summon the owner."

    body = (out.stdout or "") + ("\n" + out.stderr if out.stderr else "")
    header = f"[architect-review exit={out.returncode}; mandates: {source}]\n"
    if out.returncode == 3:
        header += (
            "HARD STOP — required infrastructure is down. No ruling was written. "
            "Development on this change halts here; summon the owner. Do not degrade.\n"
        )
    elif out.returncode == 1:
        header += "REJECTED — revise the proposal and resubmit. Do not edit code.\n"
    elif out.returncode == 0:
        header += "APPROVED — the ruling now unlocks exactly these files for 24h.\n"
    return header + body


# ---------------------------------------------------------------------------
# Registration
# ---------------------------------------------------------------------------
def register(ctx):
    ctx.register_hook("pre_tool_call", _pre_tool_call)
    ctx.register_hook("post_tool_call", _post_tool_call)
    ctx.register_hook("pre_llm_call", guidance.pre_llm_call)
    ctx.register_hook("pre_verify", guidance.pre_verify)
    ctx.register_hook("transform_tool_result", guidance.transform_tool_result)
    ctx.register_middleware("tool_request", _tool_request)

    try:
        ctx.register_tool(
            name="architect_review",
            toolset="koinon",
            schema=ARCHITECT_SCHEMA,
            handler=_architect_review,
            description=(
                "Submit a diagnosis and proposed fix for isolated architect review. "
                "Required before editing repo code: only an APPROVED ruling unlocks "
                "the edit. The dev-cycle mandates are attached to the review."
            ),
            emoji="⚖️",
        )
    except Exception as exc:  # tool registry unavailable — hooks still enforce
        logger.warning("koinon-covenant: architect_review tool not registered: %s", exc)

    logger.info("koinon-covenant registered: R1-R4 blocking, R5 warning, mandates wired")
