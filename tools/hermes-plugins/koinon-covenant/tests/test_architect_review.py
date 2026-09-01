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


class ArchitectReviewBoundaryTests(unittest.TestCase):
    def test_mandates_are_a_separate_argument_not_part_of_diagnosis(self):
        completed = subprocess.CompletedProcess([], 0, stdout="approved", stderr="")
        with (
            patch.object(plugin.mandates, "architect_digest", return_value="COMPACT MANDATES"),
            patch.object(plugin.mandates, "source", return_value="committed-canon"),
            patch.object(plugin.subprocess, "run", return_value=completed) as run,
        ):
            result = plugin._architect_review("example.py", "focused diagnosis", "focused proposal")

        command = run.call_args.args[0]
        self.assertEqual(command[command.index("--deduced") + 1], "focused diagnosis")
        self.assertEqual(command[command.index("--mandates") + 1], "COMPACT MANDATES")
        self.assertIn("APPROVED", result)


if __name__ == "__main__":
    unittest.main()
