#!/usr/bin/env node
/**
 * Performance Report Generator
 *
 * Parses Playwright E2E test results and generates performance reports
 * comparing against baseline targets and detecting regressions.
 *
 * Usage:
 *   npm run perf:report                     # Generate report only
 *   npm run perf:validate                   # Fail on >20% regression
 *   tsx scripts/performance-report.ts       # Direct execution
 */

import * as fs from 'fs';
import * as path from 'path';

interface PerformanceBaseline {
  version: string;
  lastUpdated: string;
  targets: Record<string, number>;
  baseline: Record<string, number>;
  notes?: string[];
}

interface TestResult {
  suites: TestSuite[];
  stats: {
    startTime: string;
    duration: number;
    expected: number;
    skipped: number;
    unexpected: number;
    flaky: number;
  };
}

interface TestSuite {
  title: string;
  file: string;
  suites?: TestSuite[];
}

interface PerformanceMetric {
  key: string;
  value: number;
  target: number;
  baseline?: number;
  status: 'pass' | 'fail' | 'regression';
  regressionPercent?: number;
}

interface PerformanceReport {
  timestamp: string;
  metrics: PerformanceMetric[];
  summary: {
    totalMetrics: number;
    passed: number;
    failed: number;
    regressions: number;
  };
  baselineVersion: string;
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

/**
 * Load performance baseline from metadata file
 */
function loadBaseline(): PerformanceBaseline {
  const baselinePath = path.join(process.cwd(), 'e2e/metadata/performance-baseline.json');

  try {
    const content = fs.readFileSync(baselinePath, 'utf-8');
    return JSON.parse(content);
  } catch (error) {
    console.error(`Failed to load baseline from ${baselinePath}:`, error);
    process.exit(1);
  }
}

/**
 * Load Playwright test results
 */
function loadTestResults(): TestResult {
  const resultsPath = path.join(process.cwd(), 'e2e-results.json');

  try {
    const content = fs.readFileSync(resultsPath, 'utf-8');
    return JSON.parse(content);
  } catch (error) {
    console.error(`Failed to load test results from ${resultsPath}:`, error);
    process.exit(1);
  }
}

/**
 * Extract performance metrics from Playwright console output
 * Note: Playwright JSON reporter doesn't capture console.log by default,
 * so we need to parse stdout or use a custom reporter.
 * For now, this is a placeholder that would need CI integration.
 */
function extractMetricsFromConsole(): Record<string, number> {
  const metrics: Record<string, number> = {};

  // In CI, we'd parse the test output logs
  // For now, return empty - metrics should be added by CI script
  // that captures stdout and parses it before running this script

  return metrics;
}

/**
 * Parse performance metrics from test results
 * This is a simplified version - in production, we'd need to capture
 * console output from the test run via a custom Playwright reporter
 */
function parseMetrics(): Record<string, number> {
  // For now, we'll read from a temporary metrics file if it exists
  // This would be written by a custom Playwright reporter
  const metricsPath = path.join(process.cwd(), 'playwright-report/performance-metrics.json');

  if (fs.existsSync(metricsPath)) {
    try {
      const content = fs.readFileSync(metricsPath, 'utf-8');
      return JSON.parse(content);
    } catch (error) {
      console.error(`Failed to parse metrics from ${metricsPath}:`, error);
      return {};
    }
  }

  // Fallback to parsing console output if available
  return extractMetricsFromConsole();
}

/**
 * Compare metrics against baseline and targets
 */
function analyzeMetrics(
  metrics: Record<string, number>,
  baseline: PerformanceBaseline,
  failOnRegression: boolean
): PerformanceReport {
  const report: PerformanceReport = {
    timestamp: new Date().toISOString(),
    metrics: [],
    summary: {
      totalMetrics: 0,
      passed: 0,
      failed: 0,
      regressions: 0,
    },
    baselineVersion: baseline.version,
  };

  // Analyze each metric
  for (const [key, value] of Object.entries(metrics)) {
    const target = baseline.targets[key];
    const baselineValue = baseline.baseline[key];

    if (!target) {
      console.warn(`No target defined for metric: ${key}`);
      continue;
    }

    let status: 'pass' | 'fail' | 'regression' = 'pass';
    let regressionPercent: number | undefined;

    // Check if exceeds target
    if (value > target) {
      status = 'fail';
    }

    // Check for regression if baseline exists
    if (baselineValue && failOnRegression) {
      regressionPercent = ((value - baselineValue) / baselineValue) * 100;
      if (regressionPercent > 20) {
        status = 'regression';
        report.summary.regressions++;
      }
    }

    const metric: PerformanceMetric = {
      key,
      value,
      target,
      baseline: baselineValue,
      status,
      regressionPercent,
    };

    report.metrics.push(metric);
    report.summary.totalMetrics++;

    if (status === 'fail' || status === 'regression') {
      report.summary.failed++;
    } else {
      report.summary.passed++;
    }
  }

  return report;
}

/**
 * Generate markdown report
 */
function generateMarkdownReport(report: PerformanceReport): string {
  const lines: string[] = [];

  lines.push('# Performance Test Report');
  lines.push('');
  lines.push(`Generated: ${report.timestamp}`);
  lines.push(`Baseline Version: ${report.baselineVersion}`);
  lines.push('');

  // Summary
  lines.push('## Summary');
  lines.push('');
  lines.push(`- Total Metrics: ${report.summary.totalMetrics}`);
  lines.push(`- Passed: ${report.summary.passed}`);
  lines.push(`- Failed: ${report.summary.failed}`);
  lines.push(`- Regressions: ${report.summary.regressions}`);
  lines.push('');

  // Overall status
  const overallStatus = report.summary.failed === 0 ? 'PASS' : 'FAIL';
  const statusEmoji = overallStatus === 'PASS' ? '✅' : '❌';
  lines.push(`**Overall Status:** ${statusEmoji} ${overallStatus}`);
  lines.push('');

  // Detailed metrics table
  lines.push('## Performance Metrics');
  lines.push('');
  lines.push('| Metric | Value | Target | Baseline | Status | Regression |');
  lines.push('|--------|-------|--------|----------|--------|------------|');

  for (const metric of report.metrics) {
    const statusIcon = metric.status === 'pass' ? '✅' : metric.status === 'regression' ? '⚠️' : '❌';
    const valueStr = `${metric.value.toFixed(2)}ms`;
    const targetStr = `${metric.target}ms`;
    const baselineStr = metric.baseline ? `${metric.baseline.toFixed(2)}ms` : 'N/A';
    const regressionStr = metric.regressionPercent
      ? `${metric.regressionPercent > 0 ? '+' : ''}${metric.regressionPercent.toFixed(1)}%`
      : 'N/A';

    lines.push(`| ${metric.key} | ${valueStr} | ${targetStr} | ${baselineStr} | ${statusIcon} ${metric.status} | ${regressionStr} |`);
  }

  lines.push('');

  // Notes
  lines.push('## Notes');
  lines.push('');
  lines.push('- **Target:** Maximum acceptable performance (non-negotiable MVP requirement)');
  lines.push('- **Baseline:** Previous run measurement for regression detection');
  lines.push('- **Regression:** >20% slower than baseline triggers regression warning');
  lines.push('');

  // Failed metrics details
  if (report.summary.failed > 0) {
    lines.push('## Failed Metrics');
    lines.push('');

    for (const metric of report.metrics.filter(m => m.status === 'fail' || m.status === 'regression')) {
      lines.push(`### ${metric.key}`);
      lines.push('');
      lines.push(`- **Value:** ${metric.value.toFixed(2)}ms`);
      lines.push(`- **Target:** ${metric.target}ms`);

      if (metric.status === 'fail') {
        const exceededBy = metric.value - metric.target;
        lines.push(`- **Exceeded by:** ${exceededBy.toFixed(2)}ms (${((exceededBy / metric.target) * 100).toFixed(1)}%)`);
      }

      if (metric.status === 'regression' && metric.baseline && metric.regressionPercent) {
        lines.push(`- **Baseline:** ${metric.baseline.toFixed(2)}ms`);
        lines.push(`- **Regression:** ${metric.regressionPercent.toFixed(1)}% slower`);
      }

      lines.push('');
    }
  }

  return lines.join('\n');
}

/**
 * Main execution
 */
function main() {
  const args = process.argv.slice(2);
  const failOnRegression = args.includes('--fail-on-regression');

  console.log('Performance Report Generator');
  console.log('============================');
  console.log('');

  // Load baseline
  console.log('Loading performance baseline...');
  const baseline = loadBaseline();
  console.log(`Baseline version: ${baseline.version}`);
  console.log(`Targets: ${Object.keys(baseline.targets).length} metrics`);
  console.log('');

  // Parse metrics from test results
  console.log('Parsing performance metrics...');
  const metrics = parseMetrics();
  const metricCount = Object.keys(metrics).length;
  console.log(`Found ${metricCount} performance metrics`);

  if (metricCount === 0) {
    console.warn('');
    console.warn('⚠️  No performance metrics found!');
    console.warn('');
    console.warn('This usually means:');
    console.warn('1. Performance tests did not run');
    console.warn('2. Console output was not captured');
    console.warn('3. Custom reporter is not configured');
    console.warn('');
    console.warn('To fix this, ensure performance tests run and their console');
    console.warn('output is captured to playwright-report/performance-metrics.json');
    console.warn('');
    process.exit(1);
  }

  console.log('');

  // Analyze metrics
  console.log('Analyzing performance...');
  const report = analyzeMetrics(metrics, baseline, failOnRegression);
  console.log(`Passed: ${report.summary.passed}/${report.summary.totalMetrics}`);
  console.log(`Failed: ${report.summary.failed}/${report.summary.totalMetrics}`);

  if (failOnRegression) {
    console.log(`Regressions: ${report.summary.regressions}/${report.summary.totalMetrics}`);
  }

  console.log('');

  // Generate reports
  console.log('Generating reports...');

  const reportDir = path.join(process.cwd(), 'playwright-report');
  if (!fs.existsSync(reportDir)) {
    fs.mkdirSync(reportDir, { recursive: true });
  }

  // Markdown report
  const markdownPath = path.join(reportDir, 'performance-report.md');
  const markdown = generateMarkdownReport(report);
  fs.writeFileSync(markdownPath, markdown, 'utf-8');
  console.log(`✓ Markdown report: ${markdownPath}`);

  // JSON report
  const jsonPath = path.join(reportDir, 'performance-report.json');
  fs.writeFileSync(jsonPath, JSON.stringify(report, null, 2), 'utf-8');
  console.log(`✓ JSON report: ${jsonPath}`);

  console.log('');

  // Exit with appropriate code
  if (report.summary.failed > 0) {
    console.error('❌ Performance tests FAILED');
    console.error('');

    for (const metric of report.metrics.filter(m => m.status === 'fail')) {
      console.error(`  ${metric.key}: ${metric.value.toFixed(2)}ms (target: ${metric.target}ms)`);
    }

    console.error('');
    process.exit(1);
  }

  if (failOnRegression && report.summary.regressions > 0) {
    console.error('⚠️  Performance REGRESSIONS detected');
    console.error('');

    for (const metric of report.metrics.filter(m => m.status === 'regression')) {
      console.error(`  ${metric.key}: ${metric.regressionPercent?.toFixed(1)}% slower than baseline`);
    }

    console.error('');
    process.exit(1);
  }

  console.log('✅ All performance metrics passed');
  process.exit(0);
}

// Run if executed directly
if (require.main === module) {
  main();
}
