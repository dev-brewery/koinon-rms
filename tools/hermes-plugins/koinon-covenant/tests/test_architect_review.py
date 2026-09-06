import importlib.util
import subprocess
import sys
import unittest
from pathlib import Path
from unittest.mock import patch

PLUGIN = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(PLUGIN))

spec = importlib.util.spec_from_file_location("koinon_covenant_plugin", PLUGIN / "__init__.py")
plugin = importlib.util.module_from_spec(spec)
spec.loader.exec_module(plugin)


def _ok_process():
    return subprocess.CompletedProcess([], 0, stdout="approved", stderr="")


class ArchitectReviewBoundaryTests(unittest.TestCase):
    def test_mandates_are_a_separate_argument_not_part_of_diagnosis(self):
        completed = _ok_process()
        with (
            patch.object(plugin.mandates, "architect_digest", return_value="COMPACT MANDATES"),
            patch.object(plugin.mandates, "source", return_value="committed-canon"),
            patch.object(plugin.subprocess, "run", return_value=completed) as run,
        ):
            result = plugin._architect_review(
                {"files": "example.py", "deduced": "focused diagnosis", "proposed": "focused proposal"}
            )

        command = run.call_args.args[0]
        self.assertEqual(command[command.index("--deduced") + 1], "focused diagnosis")
        self.assertEqual(command[command.index("--mandates") + 1], "COMPACT MANDATES")
        self.assertIn("APPROVED", result)


class DispatchConventionTests(unittest.TestCase):
    """#753: the registry calls plugin tools as handler(args_dict, **kwargs) —
    the whole arguments object as ONE positional parameter. These tests pin
    that convention so the TypeError class can never return."""

    def test_dispatch_style_call_succeeds(self):
        """Exactly how tools/registry.py dispatches: handler(args, **kwargs)."""
        with (
            patch.object(plugin.mandates, "architect_digest", return_value="M"),
            patch.object(plugin.mandates, "source", return_value="s"),
            patch.object(plugin.subprocess, "run", return_value=_ok_process()) as run,
        ):
            result = plugin._architect_review(
                {"files": "a.py", "deduced": "d", "proposed": "p", "issue": "753"},
                session_id="xyz",  # **kwargs must be tolerated
            )
        self.assertIn("APPROVED", result)
        command = run.call_args.args[0]
        self.assertEqual(command[command.index("--files") + 1], "a.py")
        self.assertEqual(command[command.index("--issue") + 1], "753")

    def test_missing_required_arguments_refuse_without_running_script(self):
        """Empty files/deduced/proposed must HALT before any script invocation —
        preserving the validation the old Python signature provided."""
        with patch.object(plugin.subprocess, "run") as run:
            result = plugin._architect_review({"files": "a.py", "deduced": "", "proposed": "p"})
        self.assertIn("HALT", result)
        self.assertIn("deduced", result)
        run.assert_not_called()

    def test_non_dict_dispatch_refuses(self):
        with patch.object(plugin.subprocess, "run") as run:
            result = plugin._architect_review("not-a-dict")
        self.assertIn("HALT", result)
        self.assertIn("malformed dispatch", result)
        run.assert_not_called()


if __name__ == "__main__":
    unittest.main()
