"""Dogfood tests for the koinon-covenant enforcement rules.

Every rule is exercised on BOTH paths — the call it must block and the
neighbouring call it must allow — because an unexercised gate silently breaks
(drift #6, and the reason `dev` ran months without CI).

Run: ~/hermes-agent/venv/bin/python -m unittest discover -s tests -v
"""

from __future__ import annotations

import os
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import enforcement as e  # noqa: E402


def rule_of(verdict):
    """Extract the rule tag ('R1'...) from a block directive, or None."""
    if not verdict:
        return None
    msg = verdict.get("message", "")
    if msg.startswith("[koinon-covenant "):
        return msg[len("[koinon-covenant "):].split("]")[0]
    return "?"


class BlockShape(unittest.TestCase):
    def test_block_directive_matches_hermes_contract(self):
        v = e.evaluate("terminal", {"command": "gh pr merge 1"})
        self.assertEqual(v["action"], "block")
        self.assertIsInstance(v["message"], str)
        self.assertTrue(v["message"])

    def test_covenant_value_rides_on_every_block(self):
        v = e.evaluate("terminal", {"command": "gh pr merge 1"})
        self.assertIn("Quality is the invariant", v["message"])


class R1MergeAndPush(unittest.TestCase):
    def test_blocks_gh_pr_merge(self):
        self.assertEqual(rule_of(e.evaluate("terminal", {"command": "gh pr merge 736 --squash"})), "R1")

    def test_blocks_merge_hidden_behind_benign_prefix(self):
        cmd = "cd /repo && echo starting && gh pr merge 736"
        self.assertEqual(rule_of(e.evaluate("terminal", {"command": cmd})), "R1")

    def test_blocks_pr_self_approval(self):
        self.assertEqual(rule_of(e.evaluate("terminal", {"command": "gh pr review 12 --approve"})), "R1")

    def test_blocks_push_to_protected_branch(self):
        self.assertEqual(rule_of(e.evaluate("terminal", {"command": "git push origin main"})), "R1")
        self.assertEqual(rule_of(e.evaluate("terminal", {"command": "git push origin dev"})), "R1")

    def test_blocks_force_push_anywhere(self):
        self.assertEqual(rule_of(e.evaluate("terminal", {"command": "git push --force origin feat/x"})), "R1")

    def test_blocks_git_merge_of_protected_branch(self):
        self.assertEqual(rule_of(e.evaluate("terminal", {"command": "git merge origin/main"})), "R1")

    def test_allows_feature_branch_push(self):
        self.assertIsNone(e.evaluate("terminal", {"command": "git push origin chore/my-feature"}))

    def test_allows_read_only_gh(self):
        self.assertIsNone(e.evaluate("terminal", {"command": "gh pr list --state open"}))
        self.assertIsNone(e.evaluate("terminal", {"command": "gh pr view 736"}))


class R2HarnessTamper(unittest.TestCase):
    def test_blocks_write_file_to_hooks(self):
        v = e.evaluate("write_file", {"path": "scripts/hooks/pre-tool-guard.mjs", "content": "x"})
        self.assertEqual(rule_of(v), "R2")

    def test_blocks_patch_of_approval_record(self):
        v = e.evaluate("patch", {"path": ".claude/approvals/abc.json", "new_string": "APPROVED"})
        self.assertEqual(rule_of(v), "R2")

    def test_blocks_shell_redirect_into_approval_store(self):
        cmd = "echo forged " + "> " + ".claude/approvals/forged.json"
        self.assertEqual(rule_of(e.evaluate("terminal", {"command": cmd})), "R2")

    def test_blocks_signing_key_overwrite(self):
        cmd = "cp /tmp/new.pem ~/.koinon/architecture-review-agent-ed25519.pem"
        self.assertEqual(rule_of(e.evaluate("terminal", {"command": cmd})), "R2")

    def test_blocks_settings_tamper(self):
        v = e.evaluate("write_file", {"path": ".claude/settings.json", "content": "{}"})
        self.assertEqual(rule_of(v), "R2")

    def test_allows_reading_protected_paths(self):
        self.assertIsNone(e.evaluate("terminal", {"command": "cat .claude/approvals/abc.json"}))
        self.assertIsNone(e.evaluate("terminal", {"command": "grep -r ruling .claude/approvals/"}))


class R3WorkPreservation(unittest.TestCase):
    def setUp(self):
        self._real = e.git_status_porcelain

    def tearDown(self):
        e.git_status_porcelain = self._real

    def _status(self, value):
        e.git_status_porcelain = lambda *a, **k: value

    def test_blocks_checkout_when_tree_dirty(self):
        self._status(" M src/Koinon.Api/Program.cs\n?? new.py\n")
        self.assertEqual(rule_of(e.evaluate("terminal", {"command": "git checkout ."})), "R3")

    def test_blocks_hard_reset_when_dirty(self):
        self._status(" M a.cs\n")
        self.assertEqual(rule_of(e.evaluate("terminal", {"command": "git reset " + "--hard HEAD"})), "R3")

    def test_blocks_forced_clean_when_dirty(self):
        self._status(" M a.cs\n")
        self.assertEqual(rule_of(e.evaluate("terminal", {"command": "git clean -fd"})), "R3")

    def test_blocks_stash_drop_when_dirty(self):
        self._status(" M a.cs\n")
        self.assertEqual(rule_of(e.evaluate("terminal", {"command": "git stash drop"})), "R3")

    def test_blocks_when_git_state_unreadable(self):
        self._status(None)
        self.assertEqual(rule_of(e.evaluate("terminal", {"command": "git checkout ."})), "R3")

    def test_allows_checkout_when_tree_clean(self):
        self._status("")
        self.assertIsNone(e.evaluate("terminal", {"command": "git checkout main"}))

    def test_allows_branch_creation_even_when_dirty(self):
        self._status(" M a.cs\n")
        self.assertIsNone(e.evaluate("terminal", {"command": "git checkout -b feat/new"}))
        self.assertIsNone(e.evaluate("terminal", {"command": "git switch -c feat/new"}))


class R4ArchitectGate(unittest.TestCase):
    def setUp(self):
        self._real_approved = e.is_approved
        self._real_in_repo = e.in_repo
        self._real_rel = e.rel_to_repo
        e.in_repo = lambda p: not p.startswith("/outside")
        e.rel_to_repo = lambda p: p.lstrip("/")

    def tearDown(self):
        e.is_approved = self._real_approved
        e.in_repo = self._real_in_repo
        e.rel_to_repo = self._real_rel

    def test_blocks_unapproved_code_write(self):
        e.is_approved = lambda rel, *a, **k: False
        v = e.evaluate("write_file", {"path": "src/Koinon.Api/Controllers/X.cs", "content": "c"})
        self.assertEqual(rule_of(v), "R4")
        self.assertIn("architect-review.mjs", v["message"])

    def test_allows_approved_code_write(self):
        e.is_approved = lambda rel, *a, **k: True
        self.assertIsNone(e.evaluate("write_file", {"path": "src/Koinon.Api/Controllers/X.cs", "content": "c"}))

    def test_ignores_markdown_and_docs(self):
        e.is_approved = lambda rel, *a, **k: False
        self.assertIsNone(e.evaluate("write_file", {"path": "docs/reference/notes.md", "content": "c"}))

    def test_ignores_files_outside_repo(self):
        e.is_approved = lambda rel, *a, **k: False
        self.assertIsNone(e.evaluate("write_file", {"path": "/outside/tmp/scratch.py", "content": "c"}))


class FailClosed(unittest.TestCase):
    def test_internal_error_blocks_gated_tool(self):
        real = e.RULES

        def explode(tool, args):
            raise RuntimeError("boom")

        e.RULES = (explode,)
        try:
            v = e.evaluate("terminal", {"command": "echo hi"})
            self.assertEqual(v["action"], "block")
            self.assertIn("fails closed", v["message"])
        finally:
            e.RULES = real

    def test_ungated_tools_are_never_evaluated(self):
        self.assertIsNone(e.evaluate("web_search", {"query": "gh pr merge"}))
        self.assertIsNone(e.evaluate("read_file", {"path": "scripts/hooks/x.mjs"}))


if __name__ == "__main__":
    unittest.main(verbosity=2)
