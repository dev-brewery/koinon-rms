# Proposal: Coverage Enforcement in CI Pipeline

**Issue:** #165
**Status:** Pending Human Review
**Created:** 2025-12-14

## Summary

Add coverage threshold enforcement to the CI pipeline to ensure code quality standards are maintained automatically on all PRs.

## Current State

- Coverage tests run in CI (`npm test -- --coverage`)
- Coverage reports uploaded as artifacts
- No threshold enforcement - tests can pass with low coverage

## Proposed Changes

### 1. Update Frontend Test Job

Modify the "Frontend Build & Test" job in `.github/workflows/ci.yml` to enforce coverage thresholds configured in `vitest.config.ts`.

**Current (lines 112-129):**
```yaml
    - name: Run tests
      run: npm test -- --coverage

    - name: Build production bundle
      run: npm run build

    - name: Upload build artifacts
      uses: actions/upload-artifact@v4
      with:
        name: frontend-build
        path: src/web/dist

    - name: Upload coverage reports
      if: always()
      uses: actions/upload-artifact@v4
      with:
        name: frontend-coverage
        path: src/web/coverage
```

**Proposed (replace lines 112-129):**
```yaml
    - name: Run tests with coverage
      run: npm run test:coverage

    - name: Build production bundle
      run: npm run build

    - name: Upload build artifacts
      uses: actions/upload-artifact@v4
      with:
        name: frontend-build
        path: src/web/dist

    - name: Upload coverage reports
      if: always()
      uses: actions/upload-artifact@v4
      with:
        name: frontend-coverage
        path: src/web/coverage
        retention-days: 14
```

### 2. Coverage Thresholds Configured

The following thresholds are now configured in `src/web/vitest.config.ts`:

#### Global Thresholds (all files)
- Lines: 70%
- Statements: 70%
- Functions: 70%
- Branches: 60%

#### Critical Path Thresholds
**`src/services/offline/**` - Offline data sync layer (current baseline)**
- Lines: 55%
- Statements: 56%
- Functions: 90%
- Branches: 19%

**Note:** These thresholds are set at current coverage levels to prevent regression. Future work should incrementally increase these to the target of 85% as test coverage improves.

**Pending Critical Paths (not yet tested):**
- `src/hooks/useCheckin.ts` - Check-in workflow hook
- `src/hooks/useOfflineCheckin.ts` - Offline check-in hook

These hooks need test coverage added before thresholds can be enforced. See the tracking comments (marked with #165) in `vitest.config.ts`.

## Implementation Notes

### Why `npm run test:coverage` instead of `npm test -- --coverage`?

The `test:coverage` script uses `vitest run --coverage`, which:
1. Enforces coverage thresholds (fails if below configured thresholds)
2. Exits with non-zero code on threshold failure
3. Blocks PR merge when coverage drops

The current `npm test -- --coverage` command generates coverage but doesn't enforce thresholds in CI.

### Retention Period

Coverage artifacts are kept for 14 days to:
- Support historical trend analysis
- Allow developers to compare coverage between commits
- Balance storage costs with usefulness

## Validation

Before applying this proposal:

```bash
cd src/web
npm run test:coverage
```

Expected outcome: Tests pass with current coverage levels meeting all thresholds.

## Rollout Plan

1. Human reviewer applies the workflow change
2. Verify next PR triggers coverage enforcement
3. Monitor for false positives (files incorrectly flagged)
4. Adjust critical path thresholds if needed based on actual coverage capabilities

## Risks & Mitigation

| Risk | Mitigation |
|------|------------|
| PRs blocked by flaky coverage calculation | Test locally before pushing; coverage is deterministic with v8 provider |
| Critical path threshold too high (85%) | Can be lowered if proven unrealistic; current coverage already meets this |
| Developers bypass by removing tests | Code review enforces test quality; coverage is minimum bar, not goal |

## Alternative Approaches Considered

### Codecov Integration
**Pros:** Nice UI, trend tracking, PR comments
**Cons:** External dependency, cost, unnecessary for MVP
**Decision:** Use built-in vitest thresholds first; migrate to Codecov if needed later

### Separate coverage job
**Pros:** Clearer separation of test vs coverage concerns
**Cons:** Duplicates test execution, wastes CI time
**Decision:** Single job runs tests + enforces coverage

## Success Criteria

- CI fails when coverage drops below thresholds
- Coverage reports available as artifacts on all PRs
- No false positives in first 5 PRs after implementation
- Developers can run `npm run test:coverage` locally to validate before pushing

## Human Action Required

Apply the following change to `.github/workflows/ci.yml`:

```diff
- name: Run tests
-   run: npm test -- --coverage
+ name: Run tests with coverage
+   run: npm run test:coverage

# ... build and upload steps unchanged ...

  - name: Upload coverage reports
    if: always()
    uses: actions/upload-artifact@v4
    with:
      name: frontend-coverage
      path: src/web/coverage
+     retention-days: 14
```

This change is intentionally manual per Rule 10 (Infrastructure Read-Only).
