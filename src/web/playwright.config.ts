import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright E2E Test Configuration
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  globalSetup: './e2e/global-setup.ts',
  testDir: './e2e/tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,

  // Screenshot comparison tolerances — covers antialiasing / font-rendering noise
  // without hiding real regressions. Feature specs under e2e/tests/ use toHaveScreenshot.
  expect: {
    toHaveScreenshot: {
      maxDiffPixelRatio: 0.02,
    },
  },

  reporter: [
    ['html', { outputFolder: 'playwright-report' }],
    ['json', { outputFile: 'e2e-results.json' }],
    ['./e2e/reporters/performance-metrics-reporter.ts'],
    ['list'],
  ],

  use: {
    baseURL: process.env.E2E_BASE_URL || 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },

  projects: [
    // Desktop Chrome - primary for admin workflows
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    // Kiosk mode - tablet for check-in workflows
    {
      name: 'kiosk',
      use: {
        ...devices['iPad Pro'],
        viewport: { width: 1024, height: 768 },
        deviceScaleFactor: 2,
        hasTouch: true,
      },
    },
  ],

  // Start dev server before tests if not already running.
  // PWA_DEV=1 is forwarded so feat-500's service-worker assertions
  // can register a real SW against the dev server. Other specs are
  // unaffected (the hook only runs when the env var is set).
  webServer: {
    command: 'npm run dev',
    url: 'http://localhost:5173',
    reuseExistingServer: !process.env.CI,
    timeout: 120000,
    env: {
      PWA_DEV: process.env.PWA_DEV ?? '1',
    },
  },
});
