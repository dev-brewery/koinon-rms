# Proposal: Add Performance Validation to CI Pipeline

**Issue:** #166
**Branch:** `feature/issue-166-performance-benchmarks`
**Status:** Needs human approval to modify workflow file

## Overview

This proposal adds a performance validation step to the E2E job in `.github/workflows/ci.yml` to validate check-in timing targets (<200ms online, <50ms offline).

## Prerequisites

This proposal depends on the E2E job from Issue #164. Apply that proposal first.

## Proposed Changes to `.github/workflows/ci.yml`

### Add to E2E Job (after "Run E2E tests" step)

```yaml
    - name: Generate performance report
      if: always()
      working-directory: src/web
      run: npm run perf:report
      continue-on-error: true

    - name: Validate performance targets
      working-directory: src/web
      run: npm run perf:validate
      env:
        CI: true

    - name: Upload performance report
      if: always()
      uses: actions/upload-artifact@v4
      with:
        name: performance-report
        path: |
          src/web/playwright-report/performance-report.md
          src/web/playwright-report/performance-report.json
          src/web/playwright-report/performance-metrics.json
        retention-days: 30
```

### Optional: Add Performance Badge

For repository README visibility, consider adding a performance check badge:

```yaml
    - name: Update performance badge
      if: success()
      run: echo "Performance targets met" # Placeholder for badge update
```

## Application Instructions

Since workflow files are protected infrastructure (Rule 10), please apply these changes manually:

1. Ensure E2E job from Issue #164 is applied first
2. Open `.github/workflows/ci.yml`
3. Add the performance steps after the "Run E2E tests" step in the E2E job
4. Commit with message: `ci: add performance validation to pipeline (#166)`

## How It Works

1. **Performance Tests Run**: The existing E2E tests include performance measurements
2. **Custom Reporter**: Captures console output with timing metrics to JSON
3. **Report Generation**: `npm run perf:report` generates markdown and JSON reports
4. **Validation**: `npm run perf:validate` fails if targets exceeded or regression >20%
5. **Artifacts**: Reports uploaded for review regardless of pass/fail

## Performance Targets

| Metric | Target | Description |
|--------|--------|-------------|
| onlineSearch | <200ms | Family search with network |
| offlineSearch | <50ms | Cached family search |
| memberSelect | <100ms | UI selection response |
| confirmCheckIn | <200ms | Check-in API call |
| fullFlow | <600ms | End-to-end check-in |

## Notes

- Performance tests require authenticated user (depends on Issue #182)
- Initial baseline will be empty - first run establishes baseline
- Baseline updates should be committed intentionally, not auto-generated
- The `--fail-on-regression` flag enables >20% regression detection
- Reports are always uploaded for debugging even if tests fail
