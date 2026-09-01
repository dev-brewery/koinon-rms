"""R1-R5 — the enforcement rules of the Koinon dev cycle.

Every rule opposes a specific, measured failure from this project's history;
none is decoration. Block messages state the rule, the incident that produced
it, and the compliant path — enforcement that teaches rather than merely stops.

FAIL-CLOSED: Hermes swallows hook exceptions (plugins.py invoke_hook), which
would silently pass a gated call. So evaluate() catches its own errors and
returns a BLOCK for gated tools. A broken rule stops work; it never waves it
through.
"""

from __future__ import annotations

import re

from approval_check import is_approved
from paths import (
    GATED_TOOLS,
    git_status_porcelain,
    in_repo,
    rel_to_repo,
    segments,
    tool_paths,
)

COVENANT_TAIL = (
    "Quality is the invariant; autonomy is disposable. Do not weaken, bypass, or "
    "route around this gate — if you believe it is wrong, report it to the owner and stop."
)

PROTECTED_BRANCHES = r"(?:main|master|dev|develop)"

# --- R2 protected paths: the approval store, the harness, the signing keys ---
# The lookbehind rejects a longer name ending in the same token ("myscripts/hooks/")
# while still matching a path that appears mid-command after a space or quote —
# the shape a shell redirect actually takes. Getting this anchor wrong is what
# tests/test_enforcement.py::test_blocks_shell_redirect_into_approval_store caught.
_NOT_NAME_CHAR = r"(?<![\w.-])"
PROTECTED_PATH = re.compile(
    _NOT_NAME_CHAR + r"\.claude[\\/]approvals[\\/]"
    r"|" + _NOT_NAME_CHAR + r"\.claude[\\/]settings\.json"
    r"|" + _NOT_NAME_CHAR + r"\.claude[\\/]impact-ledger\.json"
    r"|" + _NOT_NAME_CHAR + r"scripts[\\/]hooks[\\/]"
    r"|" + _NOT_NAME_CHAR + r"docs[\\/]architecture[\\/]signers[\\/]"
    r"|" + _NOT_NAME_CHAR + r"\.koinon[\\/][^\\/\s]*\.pem",
    re.I,
)

# Write-ish and delete-ish shell verbs. Assembled from fragments so this source
# carries no literal destructive command sequence of its own.
_WRITE_VERB = re.compile(
    r">>?\s*[\"']?[^\s>|&;\"']*"          # redirect into a file
    r"|\btee\b"
    r"|\bsed\b[^|;&]*\s-\w*i"             # in-place edit
    r"|\b(?:cp|mv|install|truncate|ln)\b"
    r"|\bdd\b[^|;&]*\bof=",
    re.I,
)
_DELETE_VERB = re.compile(r"\b(?:" + "|".join(["r" + "m", "rmdir", "unlink", "shred"]) + r")\b", re.I)

# --- R4: which repo files are "code" (mirrors impact-common.mjs CODE_FILE) ---
CODE_FILE = re.compile(
    r"\.(?:cs|csproj|props|targets|ts|tsx|js|jsx|mjs|cjs|py|ps1|psm1|sh|sql|ipynb)$"
    r"|(?:^|[\\/])(?:package\.json|tsconfig[^\\/]*\.json|appsettings[^\\/]*\.json"
    r"|docker-compose[^\\/]*\.ya?ml)$",
    re.I,
)


def _block(rule: str, why: str, fix: str) -> dict:
    return {
        "action": "block",
        "message": f"[koinon-covenant {rule}] {why}\n  → {fix}\n  {COVENANT_TAIL}",
    }


def _commands(tool_name: str, args: dict) -> list[str]:
    if tool_name in ("terminal", "execute_code"):
        raw = args.get("command") or args.get("code") or ""
        return segments(str(raw))
    return []


# ---------------------------------------------------------------------------
# R1 — Hermes never merges, never pushes to a protected branch.
# ---------------------------------------------------------------------------
def rule_merge_and_push(tool_name: str, args: dict) -> dict | None:
    for seg in _commands(tool_name, args):
        if re.search(r"\bgh\s+pr\s+merge\b", seg, re.I):
            return _block(
                "R1", "`gh pr merge` — Hermes never merges PRs (CARDINAL RULE, "
                "written after unauthorized merges on 2026-08-29).",
                "PRs stop at open + CI green. QA verdict on the isolated stack, then "
                "the owner's merge, are the gates.",
            )
        if re.search(r"\bgh\s+pr\s+review\b[^|;&]*--approve\b", seg, re.I):
            return _block(
                "R1", "`gh pr review --approve` — approval is a human gate, not an agent action.",
                "Report the PR as open + CI green and let the owner review.",
            )
        if re.search(r"\bgit\s+merge\b", seg, re.I) and re.search(PROTECTED_BRANCHES, seg, re.I):
            return _block(
                "R1", f"`git merge` touching a protected branch ({seg.strip()[:60]}).",
                "Protected-branch integration is the owner's, via the PR flow.",
            )
        if re.search(r"\bgit\s+push\b", seg, re.I):
            if re.search(r"(?:\s-\w*f\b|--force)", seg, re.I):
                return _block("R1", "force push — rewrites published history.",
                              "Push normally; if history is wrong, tell the owner.")
            if re.search(r"\b" + PROTECTED_BRANCHES + r"\b", seg, re.I):
                return _block(
                    "R1", f"push targeting a protected branch ({seg.strip()[:60]}).",
                    "Push the feature branch and open a PR; main/dev land through the owner.",
                )
    return None


# ---------------------------------------------------------------------------
# R2 — No forging approvals, no touching the harness or the signing keys.
# ---------------------------------------------------------------------------
def rule_harness_tamper(tool_name: str, args: dict) -> dict | None:
    for path in tool_paths(tool_name, args):
        if PROTECTED_PATH.search(path):
            return _block(
                "R2", f"write to protected infrastructure ({path}).",
                "The approval store, the hooks, and the signing keys are not agent-writable. "
                "Rulings come only from scripts/hooks/architect-review.mjs; harness problems "
                "get reported, never edited.",
            )
    for seg in _commands(tool_name, args):
        if not PROTECTED_PATH.search(seg):
            continue
        if _WRITE_VERB.search(seg) or _DELETE_VERB.search(seg):
            return _block(
                "R2", f"shell command writes or deletes protected infrastructure "
                f"({seg.strip()[:70]}).",
                "Reading these paths is fine; changing them is not. "
                "Self-signing an APPROVED ruling is the exact incident this rule exists for.",
            )
    return None


# ---------------------------------------------------------------------------
# R3 — Never discard uncommitted work (the 2026-08-29 overnight wipe).
# ---------------------------------------------------------------------------
_DESTRUCTIVE_GIT = (
    (re.compile(r"\bgit\s+reset\b[^|;&]*--hard\b", re.I), "hard reset"),
    (re.compile(r"\bgit\s+clean\b[^|;&]*\s-\w*[fdx]", re.I), "forced clean"),
    (re.compile(r"\bgit\s+stash\s+(?:drop|clear)\b", re.I), "stash drop/clear"),
    (re.compile(r"\bgit\s+(?:checkout|restore)\b", re.I), "checkout/restore"),
    (re.compile(r"\bgit\s+switch\b[^|;&]*--discard-changes\b", re.I), "switch --discard-changes"),
)

# Branch-creating forms create, they never discard.
_GIT_CREATES_BRANCH = re.compile(r"\bgit\s+(?:checkout|switch)\b[^|;&]*\s-(?:b|B|c|C)\b", re.I)


def rule_work_preservation(tool_name: str, args: dict) -> dict | None:
    for seg in _commands(tool_name, args):
        for pattern, label in _DESTRUCTIVE_GIT:
            if not pattern.search(seg):
                continue
            if _GIT_CREATES_BRANCH.search(seg):
                continue
            status = git_status_porcelain()
            if status is None:
                return _block(
                    "R3", f"`{label}` requested but git state could not be read — "
                    "cannot prove no work would be lost.",
                    "Fix the git state first; a destructive command is never run blind.",
                )
            if status.strip():
                dirty = len([ln for ln in status.splitlines() if ln.strip()])
                return _block(
                    "R3", f"`{label}` with {dirty} uncommitted change(s) in the working tree. "
                    "On 2026-08-29 a clean-state checkout destroyed a night of authorized work.",
                    "Commit the session's work to a branch first, then re-run this. "
                    "Session work is committed immediately — that is the standing rule.",
                )
    return None


# ---------------------------------------------------------------------------
# R4 — No code edit without a fresh APPROVED architect ruling.
# ---------------------------------------------------------------------------
def rule_architect_gate(tool_name: str, args: dict) -> dict | None:
    if tool_name not in ("write_file", "patch", "skill_manage"):
        return None
    for path in tool_paths(tool_name, args):
        if not in_repo(path):
            continue
        rel = rel_to_repo(path)
        if not rel or not CODE_FILE.search(rel):
            continue
        if is_approved(rel):
            continue
        return _block(
            "R4", f"{rel} — no fresh APPROVED architect ruling covers this file. "
            "An agent does not get to be its own final reviewer (covenant #5).",
            'Run: node scripts/hooks/architect-review.mjs --files "' + rel + '" '
            '--deduced "<diagnosis>" --proposed "<fix>" [--issue N]  — or call the '
            "architect_review tool. APPROVED unlocks the edit for 24h; REJECTED means "
            "revise; infrastructure-down means HALT and summon the owner.",
        )
    return None


RULES = (
    rule_merge_and_push,
    rule_harness_tamper,
    rule_work_preservation,
    rule_architect_gate,
)


def evaluate(tool_name: str, args: dict) -> dict | None:
    """Return a block directive, or None to allow. Fails CLOSED on gated tools."""
    if tool_name not in GATED_TOOLS:
        return None
    try:
        for rule in RULES:
            verdict = rule(tool_name, args or {})
            if verdict:
                return verdict
        return None
    except Exception as exc:  # never wave a gated call through on an internal error
        return _block(
            "internal", f"the covenant plugin failed while evaluating this "
            f"{tool_name} call ({type(exc).__name__}: {exc}).",
            "This gate fails closed by design. Report the failure to the owner; "
            "do not disable the plugin to make progress.",
        )
