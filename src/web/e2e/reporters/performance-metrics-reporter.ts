/**
 * Custom Playwright Reporter: Performance Metrics
 *
 * Captures console.log output from performance tests and extracts
 * timing metrics to a JSON file for post-processing.
 *
 * Usage in playwright.config.ts:
 *
 * reporter: [
 *   ['html'],
 *   ['json', { outputFile: 'e2e-results.json' }],
 *   ['./e2e/reporters/performance-metrics-reporter.ts'],
 * ]
 */

import type {
  Reporter,
  FullConfig,
  Suite,
  TestCase,
  TestResult,
  FullResult,
} from '@playwright/test/reporter';
import * as fs from 'fs';
import * as path from 'path';

interface PerformanceMetrics {
  [key: string]: number;
}

// Console log patterns to extract performance metrics
const METRIC_PATTERNS: Record<string, RegExp> = {
  onlineSearch: /Family search took: ([\d.]+)ms/,
  memberSelect: /Member selection took: ([\d.]+)ms/,
  confirmCheckIn: /Check-in confirmation took: ([\d.]+)ms/,
  fullFlow: /Full check-in flow took: ([\d.]+)ms/,
  offlineSearch: /Offline family search took: ([\d.]+)ms/,
  offlineCheckIn: /Offline check-in queue took: ([\d.]+)ms/,
};

class PerformanceMetricsReporter implements Reporter {
  private metrics: PerformanceMetrics = {};
  private outputPath: string;

  constructor(options: { outputFile?: string } = {}) {
    this.outputPath = options.outputFile || path.join('playwright-report', 'performance-metrics.json');
  }

  onBegin(_config: FullConfig, _suite: Suite): void {
    console.log(`\nPerformance Metrics Reporter initialized`);
    console.log(`Output: ${this.outputPath}\n`);
  }

  onTestEnd(test: TestCase, result: TestResult): void {
    // Skip tests that aren't performance-related
    if (!test.location.file.includes('performance')) {
      return;
    }

    // Parse stdout for performance metrics
    for (const chunk of result.stdout) {
      const text = typeof chunk === 'string' ? chunk : chunk.toString();

      for (const [key, pattern] of Object.entries(METRIC_PATTERNS)) {
        const match = text.match(pattern);
        if (match) {
          const value = parseFloat(match[1]);

          // Keep the latest value for each metric
          // (or average if multiple tests log the same metric)
          if (this.metrics[key]) {
            this.metrics[key] = (this.metrics[key] + value) / 2;
          } else {
            this.metrics[key] = value;
          }

          console.log(`  [Performance] ${key}: ${value.toFixed(2)}ms`);
        }
      }
    }
  }

  onEnd(_result: FullResult): void {
    const metricCount = Object.keys(this.metrics).length;

    if (metricCount === 0) {
      console.log('\n⚠️  No performance metrics captured');
      console.log('   Make sure performance tests are running and logging metrics\n');
      return;
    }

    try {
      // Ensure output directory exists
      const outputDir = path.dirname(this.outputPath);
      if (!fs.existsSync(outputDir)) {
        fs.mkdirSync(outputDir, { recursive: true });
      }

      // Write metrics to JSON file
      fs.writeFileSync(this.outputPath, JSON.stringify(this.metrics, null, 2), 'utf-8');

      console.log(`\n✅ Performance metrics saved: ${this.outputPath}`);
      console.log(`   Captured ${metricCount} metrics:`);

      for (const [key, value] of Object.entries(this.metrics)) {
        console.log(`   - ${key}: ${value.toFixed(2)}ms`);
      }

      console.log('');
    } catch (error) {
      console.error(`\n❌ Failed to write performance metrics to ${this.outputPath}:`);
      console.error(error);
      // Don't throw - let tests complete, but log the failure
    }
  }
}

export default PerformanceMetricsReporter;
