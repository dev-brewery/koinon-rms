#!/usr/bin/env python3
"""
RAG-based semantic architecture validation.

Primary enforcer - blocks PRs with architectural violations.
This script is called by .claude/hooks/gate-pr BEFORE creating a PR.

Exit codes:
    0: All validations passed
    2: Violations detected (blocks PR)
    1: Script error
"""
import sys
import subprocess
from pathlib import Path

# Import validators
from validators.critical import (
    validate_no_business_logic_in_controllers,
    validate_no_direct_api_calls_in_components,
    detect_n_plus_one_queries,
    detect_missing_async
)
from validators.cross_layer import (
    validate_dto_coverage,
    validate_controller_uses_services
)


def reindex_changed_files():
    """Incrementally reindex only changed files."""
    print("Reindexing changed files...")
    subprocess.run([
        sys.executable,
        str(Path(__file__).parent / "reindex-changes.py")
    ], check=True)


def main():
    print("=" * 60)
    print("RAG SEMANTIC VALIDATION (Primary Enforcer)")
    print("=" * 60)

    # Reindex changed files only (fast ~5s)
    try:
        reindex_changed_files()
    except Exception as e:
        print(f"⚠️  Warning: Reindexing failed: {e}")
        print("Continuing with existing index...")

    # Run all critical validators
    all_violations = []

    print("\n🔍 Checking for business logic in controllers...")
    all_violations.extend(validate_no_business_logic_in_controllers())

    print("🔍 Checking for direct API calls in components...")
    all_violations.extend(validate_no_direct_api_calls_in_components())

    print("🔍 Detecting N+1 query patterns...")
    all_violations.extend(detect_n_plus_one_queries())

    print("🔍 Detecting missing async/await...")
    all_violations.extend(detect_missing_async())

    print("🔍 Validating DTO coverage...")
    all_violations.extend(validate_dto_coverage())

    print("🔍 Validating controllers use services...")
    all_violations.extend(validate_controller_uses_services())

    # Report results
    if all_violations:
        print("\n" + "=" * 60)
        print(f"❌ BLOCKED: {len(all_violations)} violations detected")
        print("=" * 60)

        for i, v in enumerate(all_violations, 1):
            severity = v.get('severity', 'MEDIUM')
            print(f"\n{i}. [{severity}] {v['file']}")
            print(f"   {v['message']}")
            if 'snippet' in v:
                snippet = v['snippet'][:100] if len(v['snippet']) > 100 else v['snippet']
                print(f"   Snippet: {snippet}...")

        print("\n" + "=" * 60)
        print("Fix these violations before creating PR.")
        print("=" * 60)
        sys.exit(2)  # Block PR

    print("\n" + "=" * 60)
    print("✅ RAG validation passed - no violations detected")
    print("=" * 60)
    sys.exit(0)


if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        print(f"\n❌ FATAL ERROR: Validation script failed")
        print(f"Error: {e}")
        print("\nThis is a bug in the validation script, not your code.")
        print("Please report this issue.")
        sys.exit(1)
