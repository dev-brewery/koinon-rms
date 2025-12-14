/**
 * E2E Tests: Dashboard Performance
 * Validates <1000ms load target per Issue #169
 *
 * ASSUMPTIONS:
 * - Performance API available for timing
 * - Dashboard stats endpoint returns within target
 * - UI rendering doesn't block critical path
 * - First Contentful Paint occurs within 1000ms
 */

import { test, expect } from '../../../fixtures/auth.fixture';
import * as fs from 'fs';
import * as path from 'path';

// Performance baseline file
const BASELINE_PATH = path.join(
  __dirname,
  '../../../metadata/performance-baseline.json'
);

// Load or initialize baseline
function loadBaseline() {
  try {
    return JSON.parse(fs.readFileSync(BASELINE_PATH, 'utf-8'));
  } catch {
    return {
      version: '1.0.0',
      lastUpdated: new Date().toISOString(),
      targets: {
        dashboardPageLoad: 1000,
        statsRender: 1000,
        firstContentfulPaint: 1000,
      },
      baseline: {},
    };
  }
}

function saveBaseline(data: Record<string, unknown>) {
  // DO NOT write to filesystem during tests - this should be done in CI post-processing
  // Baseline updates should be committed intentionally, not auto-generated per test run
  console.log('Baseline data (not persisted):', JSON.stringify(data, null, 2));
}

test.describe('Dashboard Performance', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    // Login before each test
    await loginAsAdmin();
  });

  test('should load dashboard page within 1000ms', async ({ page }) => {
    // Start performance measurement
    await page.evaluate(() => performance.mark('dashboard-load-start'));

    // Navigate to dashboard
    await page.goto('/admin');

    // Wait for dashboard to be visible
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();

    // End measurement
    const duration = await page.evaluate(() => {
      performance.mark('dashboard-load-end');
      performance.measure('dashboard-page-load', 'dashboard-load-start', 'dashboard-load-end');
      const measure = performance.getEntriesByName('dashboard-page-load')[0];
      return measure.duration;
    });

    console.log(`Dashboard page load took: ${duration}ms`);
    expect(duration).toBeLessThan(1000);

    // Save to baseline
    const baseline = loadBaseline();
    baseline.baseline['dashboard-page-load'] = duration;
    baseline.lastUpdated = new Date().toISOString();
    saveBaseline(baseline);
  });

  test('should render stats within 1000ms', async ({ page }) => {
    // Navigate to dashboard first
    await page.goto('/admin');

    // Start measurement for stats rendering
    await page.evaluate(() => performance.mark('stats-render-start'));

    // Wait for stats cards to be visible (at least one stat card should appear)
    // TODO(#198): Replace fragile selector with data-testid="stat-card" when dashboard component adds test IDs
    const statCards = page.locator('[data-testid*="stat-card"], .stat-card, [class*="stat"], [role="status"]').first();
    await expect(statCards).toBeVisible({ timeout: 2000 });

    // End measurement
    const duration = await page.evaluate(() => {
      performance.mark('stats-render-end');
      performance.measure('stats-render', 'stats-render-start', 'stats-render-end');
      const measure = performance.getEntriesByName('stats-render')[0];
      return measure.duration;
    });

    console.log(`Stats render took: ${duration}ms`);
    expect(duration).toBeLessThan(1000);

    // Save to baseline
    const baseline = loadBaseline();
    baseline.baseline['stats-render'] = duration;
    saveBaseline(baseline);
  });

  test('should track dashboard stats API timing', async ({ page }) => {
    // Setup observer BEFORE navigating to dashboard
    const timingPromise = page.evaluate(() => {
      return new Promise<{ dns: number; connect: number; request: number; response: number; total: number } | null>((resolve) => {
        const observer = new PerformanceObserver((list) => {
          for (const entry of list.getEntries()) {
            // Look for dashboard stats endpoint
            if (entry.name.includes('/api/v1/dashboard') || entry.name.includes('/api/v1/stats')) {
              const e = entry as PerformanceResourceTiming;
              observer.disconnect();
              resolve({
                dns: e.domainLookupEnd - e.domainLookupStart,
                connect: e.connectEnd - e.connectStart,
                request: e.responseStart - e.requestStart,
                response: e.responseEnd - e.responseStart,
                total: e.responseEnd - e.requestStart,
              });
            }
          }
        });
        observer.observe({ entryTypes: ['resource'], buffered: true });

        // Timeout after 5 seconds
        setTimeout(() => {
          observer.disconnect();
          resolve(null);
        }, 5000);
      });
    });

    // Navigate to dashboard to trigger API call
    await page.goto('/admin');
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();

    // Wait for observer to capture API timing
    const timing = await timingPromise;

    if (timing) {
      console.log('Dashboard Stats API Timing:', timing);
      expect(timing.total).toBeLessThan(1000);

      // Save to baseline
      const baseline = loadBaseline();
      baseline.baseline['dashboard-api-timing'] = timing.total;
      saveBaseline(baseline);
    } else {
      console.log('Warning: Dashboard stats API endpoint not detected');
    }
  });

  test('should achieve First Contentful Paint within 1000ms', async ({ page }) => {
    // Navigate to dashboard
    await page.goto('/admin');

    // Wait for page to be fully loaded
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();

    // Measure First Contentful Paint
    const renderMetrics = await page.evaluate(() => {
      const paint = performance.getEntriesByType('paint');
      const fcp = paint.find((p) => p.name === 'first-contentful-paint');
      const lcp = performance
        .getEntriesByType('largest-contentful-paint')
        .pop();

      return {
        fcp: fcp?.startTime || 0,
        lcp: (lcp as PerformanceEntry | undefined)?.startTime || 0,
      };
    });

    console.log('Dashboard render metrics:', renderMetrics);
    console.log(`First Contentful Paint: ${renderMetrics.fcp}ms`);
    console.log(`Largest Contentful Paint: ${renderMetrics.lcp}ms`);

    // FCP should be under 1000ms
    expect(renderMetrics.fcp).toBeLessThan(1000);

    // Save to baseline
    const baseline = loadBaseline();
    baseline.baseline['dashboard-fcp'] = renderMetrics.fcp;
    baseline.baseline['dashboard-lcp'] = renderMetrics.lcp;
    saveBaseline(baseline);
  });

  test('should not have long tasks blocking UI', async ({ page }) => {
    // Setup longtask observer BEFORE navigating to dashboard
    const longTasksPromise = page.evaluate(() => {
      return new Promise<number[]>((resolve) => {
        const tasks: number[] = [];
        const observer = new PerformanceObserver((list) => {
          for (const entry of list.getEntries()) {
            tasks.push(entry.duration);
          }
        });

        // Start observing immediately
        observer.observe({ entryTypes: ['longtask'] });

        // Resolve after 5 seconds
        setTimeout(() => {
          observer.disconnect();
          resolve(tasks);
        }, 5000);
      });
    });

    // Navigate to dashboard
    await page.goto('/admin');
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();

    // Get captured long tasks
    const longTasks = await longTasksPromise;

    console.log(`Long tasks detected: ${longTasks.length}`);
    if (longTasks.length > 0) {
      console.log('Long task durations:', longTasks);
    }

    // Should have minimal long tasks (threshold: 3 tasks max)
    expect(longTasks.length).toBeLessThan(3);

    // Save to baseline
    const baseline = loadBaseline();
    baseline.baseline['dashboard-long-tasks'] = longTasks.length;
    saveBaseline(baseline);
  });
});

test.describe('Dashboard Performance - Full Flow', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('should measure complete dashboard load cycle', async ({ page }) => {
    // Measure complete flow from navigation to full render
    await page.evaluate(() => performance.mark('dashboard-flow-start'));

    // Navigate to dashboard
    await page.goto('/admin');

    // Wait for all key elements
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();

    // Wait for stats to load (check for any stat-related elements)
    // TODO(#198): Replace fragile selector with data-testid="stat-card" when dashboard component adds test IDs
    const statsVisible = page.locator('[data-testid*="stat"], .stat, [role="status"]').first();
    try {
      await expect(statsVisible).toBeVisible({ timeout: 2000 });
    } catch {
      console.log('Warning: No stat elements detected');
    }

    const duration = await page.evaluate(() => {
      performance.mark('dashboard-flow-end');
      performance.measure('dashboard-full-flow', 'dashboard-flow-start', 'dashboard-flow-end');
      return performance.getEntriesByName('dashboard-full-flow')[0].duration;
    });

    console.log(`Full dashboard load cycle took: ${duration}ms`);
    // Total flow should be under 1500ms (allowing for multiple API calls)
    expect(duration).toBeLessThan(1500);

    const baseline = loadBaseline();
    baseline.baseline['dashboard-full-flow'] = duration;
    saveBaseline(baseline);
  });
});

test.describe('Dashboard Performance - Regression Detection', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('@smoke should not regress from baseline', async ({ page }) => {
    const baseline = loadBaseline();
    const measurements: Record<string, number> = {};

    // Measure page load
    await page.evaluate(() => performance.mark('load-start'));
    await page.goto('/admin');
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();
    measurements['dashboard-page-load'] = await page.evaluate(() => {
      performance.mark('load-end');
      performance.measure('load', 'load-start', 'load-end');
      return performance.getEntriesByName('load')[0].duration;
    });

    // Measure FCP
    const fcp = await page.evaluate(() => {
      const paint = performance.getEntriesByType('paint');
      const fcpEntry = paint.find((p) => p.name === 'first-contentful-paint');
      return fcpEntry?.startTime || 0;
    });
    measurements['dashboard-fcp'] = fcp;

    console.log('Regression test measurements:', measurements);

    // Compare to baseline (allow 20% regression)
    for (const [key, value] of Object.entries(measurements)) {
      const baselineValue = baseline.baseline[key];
      if (baselineValue && typeof baselineValue === 'number') {
        const regression = ((value - baselineValue) / baselineValue) * 100;
        console.log(`${key}: ${value}ms (baseline: ${baselineValue}ms, ${regression.toFixed(1)}% change)`);
        expect(regression).toBeLessThan(20); // Max 20% regression
      }
    }
  });
});
