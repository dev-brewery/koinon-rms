"""Drift guard for the architect-ruling checker.

approval_check.py reimplements a digest that scripts/hooks/architect-review.mjs
owns. If that script ever changes what it hashes, this checker would silently
start covering nothing (blocking everything) or — worse — mis-validate. These
tests bind the two together: they run against the REAL records in
.claude/approvals/, so drift breaks the build instead of the gate.
"""

from __future__ import annotations

import json
import sys
import tempfile
import unittest
from datetime import datetime, timedelta, timezone
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import approval_check as ac  # noqa: E402
from paths import repo_root  # noqa: E402


def _record(files, deduced="d", proposed="p", ruling="APPROVED", at=None):
    body = {"files": files, "deduced": deduced, "proposed": proposed}
    sha = ac.hashlib.sha256(
        json.dumps(body, separators=(",", ":"), ensure_ascii=False).encode()
    ).hexdigest()
    return sha, {
        "sha": sha,
        "ruling": ruling,
        "files": files,
        "deduced": deduced,
        "proposed": proposed,
        "at": (at or datetime.now(timezone.utc)).isoformat().replace("+00:00", "Z"),
    }


class DigestMatchesTheRealScript(unittest.TestCase):
    """The binding test: our digest must reproduce what architect-review.mjs wrote."""

    def test_real_records_validate(self):
        directory = repo_root() / ".claude" / "approvals"
        records = sorted(directory.glob("*.json")) if directory.exists() else []
        if not records:
            self.skipTest("no real approval records present to bind against")
        for path in records:
            data = json.loads(path.read_text(encoding="utf-8"))
            self.assertEqual(
                ac._digest(data), data["sha"],
                f"{path.name}: our digest no longer reproduces architect-review.mjs's sha — "
                "the script's hashed inputs changed and this checker must be updated.",
            )
            self.assertEqual(path.name, f"{data['sha']}.json")

    def test_real_records_are_covered_at_their_own_timestamp(self):
        directory = repo_root() / ".claude" / "approvals"
        records = sorted(directory.glob("*.json")) if directory.exists() else []
        if not records:
            self.skipTest("no real approval records present")
        data = json.loads(records[0].read_text(encoding="utf-8"))
        at = datetime.fromisoformat(data["at"].replace("Z", "+00:00"))
        covered = ac.approved_files(directory, now=at + timedelta(minutes=1))
        self.assertIn(ac.normalize_key(data["files"][0]["path"]), covered)


class Validation(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.dir = Path(self.tmp.name)

    def tearDown(self):
        self.tmp.cleanup()

    def _write(self, sha, record, name=None):
        (self.dir / (name or f"{sha}.json")).write_text(json.dumps(record), encoding="utf-8")

    def test_fresh_approved_record_covers_its_files(self):
        sha, rec = _record([{"path": "src/A.cs", "hash": "x"}])
        self._write(sha, rec)
        self.assertTrue(ac.is_approved("src/A.cs", self.dir))
        self.assertTrue(ac.is_approved("src/a.cs", self.dir))  # case-insensitive

    def test_conditions_ruling_also_approves(self):
        sha, rec = _record([{"path": "src/A.cs"}], ruling="APPROVED_WITH_CONDITIONS")
        self._write(sha, rec)
        self.assertTrue(ac.is_approved("src/A.cs", self.dir))

    def test_rejected_covers_nothing(self):
        sha, rec = _record([{"path": "src/A.cs"}], ruling="REJECTED")
        self._write(sha, rec)
        self.assertFalse(ac.is_approved("src/A.cs", self.dir))

    def test_expired_record_covers_nothing(self):
        old = datetime.now(timezone.utc) - timedelta(hours=25)
        sha, rec = _record([{"path": "src/A.cs"}], at=old)
        self._write(sha, rec)
        self.assertFalse(ac.is_approved("src/A.cs", self.dir))

    def test_tampered_content_voids_the_record(self):
        """Editing the proposal after approval must not keep the unlock."""
        sha, rec = _record([{"path": "src/A.cs"}])
        rec["proposed"] = "something else entirely"
        self._write(sha, rec)
        self.assertFalse(ac.is_approved("src/A.cs", self.dir))

    def test_widening_the_file_set_voids_the_record(self):
        """Adding a file to an approved ruling must not silently unlock it."""
        sha, rec = _record([{"path": "src/A.cs"}])
        rec["files"] = [{"path": "src/A.cs"}, {"path": "src/Secret.cs"}]
        self._write(sha, rec)
        self.assertFalse(ac.is_approved("src/A.cs", self.dir))
        self.assertFalse(ac.is_approved("src/Secret.cs", self.dir))

    def test_renamed_record_covers_nothing(self):
        sha, rec = _record([{"path": "src/A.cs"}])
        self._write(sha, rec, name="anything-else.json")
        self.assertFalse(ac.is_approved("src/A.cs", self.dir))

    def test_malformed_record_covers_nothing(self):
        (self.dir / "broken.json").write_text("{not json", encoding="utf-8")
        self.assertFalse(ac.is_approved("src/A.cs", self.dir))

    def test_missing_directory_covers_nothing(self):
        self.assertFalse(ac.is_approved("src/A.cs", self.dir / "nope"))

    def test_unrelated_file_not_covered(self):
        sha, rec = _record([{"path": "src/A.cs"}])
        self._write(sha, rec)
        self.assertFalse(ac.is_approved("src/B.cs", self.dir))


if __name__ == "__main__":
    unittest.main(verbosity=2)
