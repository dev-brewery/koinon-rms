# Performance Reporting Integration

This directory contains scripts for generating performance reports from E2E test results.

## Overview

The performance report script (`performance-report.ts`) parses Playwright test results and generates comprehensive performance reports comparing against baseline targets and detecting regressions.

## Usage

### Generate Report (No Validation)
```bash
npm run perf:report
```
Generates markdown and JSON reports without failing on regressions.

### Validate Performance (Fail on Regression)
```bash
npm run perf:validate
```
Generates reports and exits with code 1 if:
- Any metric exceeds its target
- Any metric is >20% slower than baseline (regression)

### Direct Execution
```bash
npx tsx scripts/performance-report.ts
npx tsx scripts/performance-report.ts --fail-on-regression
```

## How It Works

### 1. Performance Baseline

The script reads from `e2e/metadata/performance-baseline.json`:

```json
{
  "version": "1.0.0",
  "lastUpdated": "2025-01-15T00:00:00Z",
  "targets": {
    "onlineSearch": 200,
    "offlineSearch": 50,
    "memberSelect": 100,
    "confirmCheckIn": 200,
    "fullFlow": 600
  },
  "baseline": {
    "onlineSearch": 145.23,
    "memberSelect": 67.89,
    "confirmCheckIn": 178.45,
    "fullFlow": 487.32,
    "offlineSearch": 32.11,
    "offlineCheckIn": 28.76
  }
}
```

- **targets**: Non-negotiable MVP requirements (hard limits)
- **baseline**: Previous successful run values (for regression detection)

### 2. Metrics Collection

Performance metrics are logged by E2E tests using `console.log()`:

```typescript
console.log(`Family search took: ${duration}ms`);
console.log(`Member selection took: ${duration}ms`);
console.log(`Check-in confirmation took: ${duration}ms`);
console.log(`Full check-in flow took: ${duration}ms`);
console.log(`Offline family search took: ${duration}ms`);
console.log(`Offline check-in queue took: ${duration}ms`);
```

### 3. Metrics Extraction (CI Integration Required)

The script expects metrics in `playwright-report/performance-metrics.json`:

```json
{
  "onlineSearch": 145.23,
  "memberSelect": 67.89,
  "confirmCheckIn": 178.45,
  "fullFlow": 487.32,
  "offlineSearch": 32.11,
  "offlineCheckIn": 28.76
}
```

**Note:** This file is NOT auto-generated. You need to:

#### Option A: Custom Playwright Reporter (Recommended)

Create a custom reporter that captures console output:

```typescript
// reporters/performance-metrics-reporter.ts
import { Reporter, TestResult } from '@playwright/test/reporter';
import * as fs from 'fs';
import * as path from 'path';

const METRIC_PATTERNS: Record<string, RegExp> = {
  onlineSearch: /Family search took: ([\d.]+)ms/,
  memberSelect: /Member selection took: ([\d.]+)ms/,
  confirmCheckIn: /Check-in confirmation took: ([\d.]+)ms/,
  fullFlow: /Full check-in flow took: ([\d.]+)ms/,
  offlineSearch: /Offline family search took: ([\d.]+)ms/,
  offlineCheckIn: /Offline check-in queue took: ([\d.]+)ms/,
};

class PerformanceMetricsReporter implements Reporter {
  private metrics: Record<string, number> = {};

  onTestEnd(test: TestCase, result: TestResult) {
    // Parse stdout for performance metrics
    for (const output of result.stdout) {
      const text = output.toString();
      for (const [key, pattern] of Object.entries(METRIC_PATTERNS)) {
        const match = text.match(pattern);
        if (match) {
          this.metrics[key] = parseFloat(match[1]);
        }
      }
    }
  }

  onEnd() {
    const outputPath = path.join('playwright-report', 'performance-metrics.json');
    fs.mkdirSync(path.dirname(outputPath), { recursive: true });
    fs.writeFileSync(outputPath, JSON.stringify(this.metrics, null, 2));
  }
}

export default PerformanceMetricsReporter;
```

Add to `playwright.config.ts`:

```typescript
reporter: [
  ['html'],
  ['json', { outputFile: 'e2e-results.json' }],
  ['./reporters/performance-metrics-reporter.ts'],
],
```

#### Option B: Parse Test Output in CI

In your CI workflow:

```yaml
- name: Run E2E Tests
  run: npm run e2e 2>&1 | tee e2e-output.log

- name: Extract Performance Metrics
  run: |
    mkdir -p playwright-report
    node -e "
    const fs = require('fs');
    const output = fs.readFileSync('e2e-output.log', 'utf-8');
    const metrics = {};

    const patterns = {
      onlineSearch: /Family search took: ([\d.]+)ms/,
      memberSelect: /Member selection took: ([\d.]+)ms/,
      confirmCheckIn: /Check-in confirmation took: ([\d.]+)ms/,
      fullFlow: /Full check-in flow took: ([\d.]+)ms/,
      offlineSearch: /Offline family search took: ([\d.]+)ms/,
      offlineCheckIn: /Offline check-in queue took: ([\d.]+)ms/,
    };

    for (const [key, pattern] of Object.entries(patterns)) {
      const match = output.match(pattern);
      if (match) metrics[key] = parseFloat(match[1]);
    }

    fs.writeFileSync(
      'playwright-report/performance-metrics.json',
      JSON.stringify(metrics, null, 2)
    );
    "

- name: Generate Performance Report
  run: npm run perf:validate
```

### 4. Report Generation

The script generates two reports:

#### Markdown Report (`playwright-report/performance-report.md`)

```markdown
# Performance Test Report

Generated: 2025-12-14T12:00:00.000Z
Baseline Version: 1.0.0

## Summary

- Total Metrics: 6
- Passed: 5
- Failed: 1
- Regressions: 0

**Overall Status:** ❌ FAIL

## Performance Metrics

| Metric | Value | Target | Baseline | Status | Regression |
|--------|-------|--------|----------|--------|------------|
| onlineSearch | 145.23ms | 200ms | 145.23ms | ✅ pass | N/A |
| memberSelect | 67.89ms | 100ms | 67.89ms | ✅ pass | N/A |
| confirmCheckIn | 178.45ms | 200ms | 178.45ms | ✅ pass | N/A |
| fullFlow | 487.32ms | 600ms | 487.32ms | ✅ pass | N/A |
| offlineSearch | 32.11ms | 50ms | 32.11ms | ✅ pass | N/A |
| offlineCheckIn | 65.00ms | 50ms | N/A | ❌ fail | N/A |
```

#### JSON Report (`playwright-report/performance-report.json`)

```json
{
  "timestamp": "2025-12-14T12:00:00.000Z",
  "metrics": [
    {
      "key": "onlineSearch",
      "value": 145.23,
      "target": 200,
      "baseline": 145.23,
      "status": "pass"
    },
    {
      "key": "offlineCheckIn",
      "value": 65.0,
      "target": 50,
      "status": "fail"
    }
  ],
  "summary": {
    "totalMetrics": 6,
    "passed": 5,
    "failed": 1,
    "regressions": 0
  },
  "baselineVersion": "1.0.0"
}
```

## Exit Codes

- **0**: All metrics passed
- **1**: One or more metrics failed or regressed

## Metric Definitions

| Metric | Description | Target | Console Pattern |
|--------|-------------|--------|-----------------|
| `onlineSearch` | Family search with network | <200ms | `Family search took: Xms` |
| `memberSelect` | Member selection UI response | <100ms | `Member selection took: Xms` |
| `confirmCheckIn` | Check-in confirmation | <200ms | `Check-in confirmation took: Xms` |
| `fullFlow` | Complete check-in flow | <600ms | `Full check-in flow took: Xms` |
| `offlineSearch` | Cached family search | <50ms | `Offline family search took: Xms` |
| `offlineCheckIn` | Offline queue operation | <50ms | `Offline check-in queue took: Xms` |

## Regression Detection

When `--fail-on-regression` is used:
- Compares current metrics against baseline values
- Fails if any metric is >20% slower than baseline
- Ignores regressions if no baseline exists

Example:
```
Baseline: 100ms
Current: 125ms
Regression: 25% -> FAIL
```

## CI Integration Example

```yaml
name: E2E Tests

jobs:
  e2e:
    steps:
      - name: Run E2E Tests
        run: npm run e2e 2>&1 | tee e2e-output.log

      - name: Extract Performance Metrics
        run: |
          # Use custom reporter or parse output
          # Creates playwright-report/performance-metrics.json

      - name: Generate Performance Report
        run: npm run perf:report

      - name: Upload Report
        uses: actions/upload-artifact@v3
        with:
          name: performance-report
          path: playwright-report/performance-report.*

      - name: Validate Performance (PR only)
        if: github.event_name == 'pull_request'
        run: npm run perf:validate
```

## Troubleshooting

### "No performance metrics found"

This means `playwright-report/performance-metrics.json` doesn't exist or is empty.

**Fix:**
1. Ensure performance tests run (check `e2e/tests/admin/checkin/checkin-performance.spec.ts`)
2. Set up custom reporter (Option A above) OR
3. Parse console output in CI (Option B above)

### Metrics don't match console output

The console output from Playwright tests is NOT automatically captured by the JSON reporter.

**Fix:**
Use the custom reporter approach to capture stdout during test execution.

### Baseline values not persisting

Baseline values in `performance-baseline.json` should be manually updated after successful runs.

**Do NOT auto-update baseline** - this prevents detecting real regressions.

## Development Workflow

1. Run E2E tests: `npm run e2e`
2. Extract metrics (manual or CI)
3. Generate report: `npm run perf:report`
4. Review `playwright-report/performance-report.md`
5. If all passed and better than baseline, update baseline manually

## Future Improvements

- [ ] Custom Playwright reporter for automatic metrics extraction
- [ ] Historical trend tracking
- [ ] Visual performance graphs
- [ ] Percentile analysis (p50, p95, p99)
- [ ] CI comment on PRs with performance comparison
