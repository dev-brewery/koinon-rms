# Web Scripts

Utility scripts for the Koinon RMS web application.

## Performance Reporting

### Quick Start

```bash
# Test the performance report generator
./scripts/test-performance-report.sh

# Generate report from real test run
npm run perf:report

# Validate performance and fail on regression
npm run perf:validate
```

### Scripts

| Script | Description |
|--------|-------------|
| `performance-report.ts` | Generates performance reports from E2E test metrics |
| `test-performance-report.sh` | Test script with sample data |

### Documentation

- **[PERFORMANCE-REPORTING.md](./PERFORMANCE-REPORTING.md)** - Complete integration guide

### Integration

To enable automatic metrics collection, add the custom reporter to `playwright.config.ts`:

```typescript
import { defineConfig } from '@playwright/test';

export default defineConfig({
  reporter: [
    ['html'],
    ['json', { outputFile: 'e2e-results.json' }],
    ['./e2e/reporters/performance-metrics-reporter.ts'],
  ],
  // ... rest of config
});
```

Then run:

```bash
npm run e2e                    # Runs tests, captures metrics
npm run perf:report            # Generates report
npm run perf:validate          # Validates against targets/baseline
```

### Output

Performance reports are generated in `playwright-report/`:

- `performance-report.md` - Human-readable markdown report
- `performance-report.json` - Machine-readable JSON report
- `performance-metrics.json` - Raw metrics from test run

### CI Integration

Example GitHub Actions workflow:

```yaml
- name: Run E2E Tests
  run: npm run e2e

- name: Generate Performance Report
  if: always()
  run: npm run perf:report

- name: Upload Performance Report
  if: always()
  uses: actions/upload-artifact@v3
  with:
    name: performance-report
    path: playwright-report/performance-report.*

- name: Validate Performance
  if: github.event_name == 'pull_request'
  run: npm run perf:validate
```

### Metrics

Performance tests log metrics using console.log:

```typescript
console.log(`Family search took: ${duration}ms`);
```

The custom reporter captures these and writes to `performance-metrics.json`.

See [PERFORMANCE-REPORTING.md](./PERFORMANCE-REPORTING.md) for complete details.
