import sys
import unittest
from pathlib import Path
from unittest.mock import patch

PLUGIN = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(PLUGIN))

import mandates  # noqa: E402


class ArchitectDigestTests(unittest.TestCase):
    def setUp(self):
        mandates._digest_cache.clear()

    def tearDown(self):
        mandates._digest_cache.clear()

    def test_is_compact_projection_of_mandate_canon(self):
        full = mandates.digest(force=True)
        compact = mandates.architect_digest(force=True)

        self.assertLess(len(compact), len(full) // 2)
        self.assertIn("Never merge a PR", compact)
        self.assertIn("Never discard uncommitted work", compact)
        self.assertIn("IdKey", compact)
        self.assertNotIn("The readback", compact)
        self.assertNotIn("The commitments (first person", compact)

    def test_changes_when_committed_mandate_changes(self):
        canon = """# Mandates
## 1. The top value
**Canonical value.**
## 2. What you may never do (mechanically enforced)
| # | Mandate | Why |
| R1 | **Canonical rule.** | incident |
## 6. Non-negotiable code invariants
- Canonical invariant.
"""
        with patch.object(mandates, "_read_repo_doc", return_value=canon):
            compact = mandates.architect_digest(force=True)

        self.assertIn("Canonical value.", compact)
        self.assertIn("Canonical rule.", compact)
        self.assertIn("Canonical invariant.", compact)


if __name__ == "__main__":
    unittest.main()
