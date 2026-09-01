"""Architect-ruling verification — the Python mirror of pre-tool-guard.mjs.

Reimplements exactly the record validation `scripts/hooks/architect-review.mjs`
writes and `scripts/hooks/pre-tool-guard.mjs` enforces:

  sha = sha256(JSON.stringify({files, deduced, proposed}))

A record counts only when its filename AND its ``sha`` field both equal that
digest, its ruling approves, and it is inside the TTL. Anything else (missing,
malformed, renamed, stale, REJECTED) covers nothing — closed by construction.

DRIFT GUARD: tests/test_approval_check.py verifies this against the real
records in .claude/approvals/. If architect-review.mjs ever changes its digest
inputs, that test fails loudly instead of this checker silently passing or
blocking everything.
"""

from __future__ import annotations

import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path

from paths import repo_root

APPROVAL_TTL_SECONDS = 24 * 60 * 60
APPROVING_RULINGS = frozenset({"APPROVED", "APPROVED_WITH_CONDITIONS"})


def normalize_key(rel: str) -> str:
    """Match normalizeKey() in impact-common.mjs: \\ -> /, drop one leading ./, lowercase."""
    key = rel.replace("\\", "/")
    if key.startswith("./"):
        key = key[2:]
    return key.lower()


def _digest(record: dict) -> str:
    payload = json.dumps(
        {
            "files": record["files"],
            "deduced": record["deduced"],
            "proposed": record["proposed"],
        },
        separators=(",", ":"),
        ensure_ascii=False,
    )
    return hashlib.sha256(payload.encode("utf-8")).hexdigest()


def _parse_at(value: str) -> datetime | None:
    try:
        return datetime.fromisoformat(str(value).replace("Z", "+00:00"))
    except Exception:
        return None


def approved_files(approvals_dir: Path | None = None, now: datetime | None = None) -> set[str]:
    """Every repo-relative path covered by a fresh, integrity-valid ruling."""
    directory = approvals_dir or (repo_root() / ".claude" / "approvals")
    now = now or datetime.now(timezone.utc)
    covered: set[str] = set()
    try:
        names = sorted(p for p in directory.iterdir() if p.suffix == ".json")
    except Exception:
        return covered
    for path in names:
        try:
            record = json.loads(path.read_text(encoding="utf-8"))
            if record.get("ruling") not in APPROVING_RULINGS:
                continue
            at = _parse_at(record.get("at", ""))
            if at is None:
                continue
            if at.tzinfo is None:
                at = at.replace(tzinfo=timezone.utc)
            if (now - at).total_seconds() >= APPROVAL_TTL_SECONDS:
                continue
            sha = _digest(record)
            if sha != record.get("sha") or path.name != f"{sha}.json":
                continue
            for entry in record["files"]:
                covered.add(normalize_key(str(entry["path"])))
        except Exception:
            continue  # malformed record covers nothing
    return covered


def is_approved(rel_path: str, approvals_dir: Path | None = None) -> bool:
    """True when a fresh APPROVED ruling covers this repo-relative path."""
    if not rel_path:
        return False
    return normalize_key(rel_path) in approved_files(approvals_dir)
