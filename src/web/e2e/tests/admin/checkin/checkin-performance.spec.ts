/**
 * E2E Tests: Check-in Performance
 * Validates <200ms online, <50ms offline targets
 *
 * ASSUMPTIONS:
 * - Performance API available for timing
 * - Family search returns within target
 * - UI rendering doesn't block critical path
 * - Offline cache lookup is faster than network
 */

import { test, expect } from '@playwright/test';
import { CheckinPage } from '../../../fixtures/page-objects/checkin.page';
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
        onlineSearch: 200,
        offlineSearch: 50,
        memberSelect: 100,
        confirmCheckIn: 200,
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

test.describe('Check-in Performance - Online Mode', () => {
  test.beforeEach(async ({ page }) => {
    const checkin = new CheckinPage(page);
    await checkin.goto();
  });

  test('should search family within 200ms target', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Start performance measurement
    await page.evaluate(() => performance.mark('search-start'));

    // Perform search
    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();

    // Wait for results
    await expect(checkin.familyMemberCards.first()).toBeVisible();

    // End measurement
    const duration = await page.evaluate(() => {
      performance.mark('search-end');
      performance.measure('family-search', 'search-start', 'search-end');
      const measure = performance.getEntriesByName('family-search')[0];
      return measure.duration;
    });

    console.log(`Family search took: ${duration}ms`);
    expect(duration).toBeLessThan(200);

    // Save to baseline
    const baseline = loadBaseline();
    baseline.baseline['online-search'] = duration;
    baseline.lastUpdated = new Date().toISOString();
    saveBaseline(baseline);
  });

  test('should measure member selection performance', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Setup
    await checkin.searchByPhone('5551234567');
    await expect(checkin.familyMemberCards.first()).toBeVisible();

    // Measure selection
    await page.evaluate(() => performance.mark('select-start'));
    await checkin.selectMember(0);
    const duration = await page.evaluate(() => {
      performance.mark('select-end');
      performance.measure('member-select', 'select-start', 'select-end');
      return performance.getEntriesByName('member-select')[0].duration;
    });

    console.log(`Member selection took: ${duration}ms`);
    expect(duration).toBeLessThan(100);

    const baseline = loadBaseline();
    baseline.baseline['member-select'] = duration;
    saveBaseline(baseline);
  });

  test('should complete check-in within 200ms', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Setup
    await checkin.searchByPhone('5551234567');
    await checkin.selectMember(0);

    // Measure check-in
    await page.evaluate(() => performance.mark('checkin-start'));
    await checkin.confirmCheckin();
    await expect(checkin.successMessage).toBeVisible();

    const duration = await page.evaluate(() => {
      performance.mark('checkin-end');
      performance.measure('check-in', 'checkin-start', 'checkin-end');
      return performance.getEntriesByName('check-in')[0].duration;
    });

    console.log(`Check-in confirmation took: ${duration}ms`);
    expect(duration).toBeLessThan(200);

    const baseline = loadBaseline();
    baseline.baseline['confirm-checkin'] = duration;
    saveBaseline(baseline);
  });

  test('should measure full flow performance', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Measure end-to-end
    await page.evaluate(() => performance.mark('flow-start'));

    // Complete full flow
    await checkin.searchByPhone('5551234567');
    await checkin.selectMember(0);
    await checkin.confirmCheckin();
    await expect(checkin.successMessage).toBeVisible();

    const duration = await page.evaluate(() => {
      performance.mark('flow-end');
      performance.measure('full-flow', 'flow-start', 'flow-end');
      return performance.getEntriesByName('full-flow')[0].duration;
    });

    console.log(`Full check-in flow took: ${duration}ms`);
    // Total should be less than sum of parts (target: ~500ms)
    expect(duration).toBeLessThan(600);

    const baseline = loadBaseline();
    baseline.baseline['full-flow'] = duration;
    saveBaseline(baseline);
  });

  test('should track API response time', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Setup observer BEFORE triggering the request
    const timingPromise = page.evaluate(() => {
      return new Promise<{ dns: number; tcp: number; request: number; response: number; total: number }>((resolve) => {
        const observer = new PerformanceObserver((list) => {
          for (const entry of list.getEntries()) {
            if (entry.name.includes('/api/v1/families/search')) {
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

    // Trigger the request
    await checkin.searchByPhone('5551234567');

    // Wait for observer to capture
    const timing = await timingPromise;

    if (timing) {
      console.log('API Timing:', timing);
      expect(timing.total).toBeLessThan(200);
    }
  });
});

test.describe('Check-in Performance - Offline Mode', () => {
  test('should search cached family within 50ms', async ({ page, context }) => {
    const checkin = new CheckinPage(page);

    // Cache data first
    await checkin.searchByPhone('5551234567');
    await expect(checkin.familyMemberCards.first()).toBeVisible();

    // Go offline
    await context.setOffline(true);

    // Reset page
    await checkin.goto();

    // Measure offline search
    await page.evaluate(() => performance.mark('offline-search-start'));
    await checkin.searchByPhone('5551234567');
    await expect(checkin.familyMemberCards.first()).toBeVisible();

    const duration = await page.evaluate(() => {
      performance.mark('offline-search-end');
      performance.measure(
        'offline-family-search',
        'offline-search-start',
        'offline-search-end'
      );
      return performance.getEntriesByName('offline-family-search')[0].duration;
    });

    console.log(`Offline family search took: ${duration}ms`);
    expect(duration).toBeLessThan(50);

    const baseline = loadBaseline();
    baseline.baseline['offline-search'] = duration;
    saveBaseline(baseline);
  });

  test('should queue check-in within 50ms', async ({ page, context }) => {
    const checkin = new CheckinPage(page);

    // Cache and setup
    await checkin.searchByPhone('5551234567');
    await checkin.selectMember(0);

    // Go offline
    await context.setOffline(true);

    // Measure offline check-in (queue operation)
    await page.evaluate(() => performance.mark('offline-checkin-start'));
    await checkin.confirmCheckin();
    await expect(page.getByText(/queued/i)).toBeVisible();

    const duration = await page.evaluate(() => {
      performance.mark('offline-checkin-end');
      performance.measure(
        'offline-checkin',
        'offline-checkin-start',
        'offline-checkin-end'
      );
      return performance.getEntriesByName('offline-checkin')[0].duration;
    });

    console.log(`Offline check-in queue took: ${duration}ms`);
    expect(duration).toBeLessThan(50);

    const baseline = loadBaseline();
    baseline.baseline['offline-checkin'] = duration;
    saveBaseline(baseline);
  });
});

test.describe('Check-in Performance - Rendering', () => {
  test('should render family list efficiently', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Search for family with multiple members
    await checkin.searchByPhone('5551234567');

    // Measure rendering time
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

    console.log('Render metrics:', renderMetrics);
    // FCP should be under 1s
    expect(renderMetrics.fcp).toBeLessThan(1000);
  });

  test('should not block UI during search', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Setup longtask observer BEFORE search operation
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

    // Now perform the search operation
    await checkin.searchByPhone('5551234567');
    await expect(checkin.familyMemberCards.first()).toBeVisible();

    // Get captured long tasks
    const longTasks = await longTasksPromise;

    // Should have minimal long tasks
    expect(longTasks.length).toBeLessThan(3);
  });
});

test.describe('Check-in Performance - Regression Detection', () => {
  test('@smoke should not regress from baseline', async ({ page }) => {
    const checkin = new CheckinPage(page);
    const baseline = loadBaseline();

    // Run all measurements
    const measurements: Record<string, number> = {};

    // Online search
    await page.evaluate(() => performance.mark('search-start'));
    await checkin.searchByPhone('5551234567');
    await expect(checkin.familyMemberCards.first()).toBeVisible();
    measurements['online-search'] = await page.evaluate(() => {
      performance.mark('search-end');
      performance.measure('search', 'search-start', 'search-end');
      return performance.getEntriesByName('search')[0].duration;
    });

    // Member select
    await page.evaluate(() => performance.mark('select-start'));
    await checkin.selectMember(0);
    measurements['member-select'] = await page.evaluate(() => {
      performance.mark('select-end');
      performance.measure('select', 'select-start', 'select-end');
      return performance.getEntriesByName('select')[0].duration;
    });

    // Check-in
    await page.evaluate(() => performance.mark('checkin-start'));
    await checkin.confirmCheckin();
    await expect(checkin.successMessage).toBeVisible();
    measurements['confirm-checkin'] = await page.evaluate(() => {
      performance.mark('checkin-end');
      performance.measure('checkin', 'checkin-start', 'checkin-end');
      return performance.getEntriesByName('checkin')[0].duration;
    });

    // Compare to baseline (allow 20% regression)
    for (const [key, value] of Object.entries(measurements)) {
      const baselineValue = baseline.baseline[key];
      if (baselineValue) {
        const regression = ((value - baselineValue) / baselineValue) * 100;
        console.log(`${key}: ${value}ms (baseline: ${baselineValue}ms, ${regression.toFixed(1)}% change)`);
        expect(regression).toBeLessThan(20); // Max 20% regression
      }
    }
  });
});
