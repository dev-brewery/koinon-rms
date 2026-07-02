/**
 * @smoke — Golden-path smoke suite
 *
 * Runs against a REAL stack (no API mocking) with seeded data
 * (tools/Koinon.TestDataSeeder). This is the suite the product owner, PM, or
 * anyone else runs to answer "is the app alive and can a user actually use
 * it?" — one command via tools/qa/run-e2e-demo.ps1 / .sh.
 *
 * Targets:
 *   - Docker demo stack:  E2E_BASE_URL=http://localhost:3000 npx playwright test --grep @smoke
 *   - Local dev server:   npx playwright test --grep @smoke   (starts vite on :5173)
 *
 * Rules for this file (keep it trustworthy):
 *   - No API mocking. If it needs a mock, it belongs in a feature spec.
 *   - Only seeded data (fixtures/test-data.ts). Never create data here.
 *   - Print bridge is the one exception: it's a LOCAL desktop dependency,
 *     not part of the stack, so the kiosk check tolerates its absence.
 *   - Every test must pass on a freshly seeded stack, in any order.
 */

import { test, expect } from '../fixtures/auth.fixture';
import { installPrintBridgeMock } from '../fixtures/print-bridge.fixture';

test.describe('@smoke golden path', () => {
  test('home page renders and offers sign-in', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByRole('heading', { name: 'Koinon RMS' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Sign In' })).toBeVisible();
  });

  test('admin can log in and reach the admin area', async ({ page, loginAsAdmin }) => {
    await loginAsAdmin();
    // A fresh stack with no campus auto-launches the setup wizard from /admin —
    // both destinations prove authentication + routing + API work.
    await expect(page).toHaveURL(/\/admin(\/setup-wizard)?$/);
  });

  test('people list shows seeded people and search works', async ({ page, loginAsAdmin }) => {
    await loginAsAdmin();
    await page.goto('/admin/people');
    await expect(page.getByRole('heading', { name: 'People' })).toBeVisible();
    await expect(page.getByText('John Smith').first()).toBeVisible();

    await page.getByPlaceholder(/Search people/).fill('Johnson');
    await expect(page.getByText('Bob Johnson').first()).toBeVisible();
    await expect(page.getByText('John Smith')).not.toBeVisible();
  });

  test('person detail loads from the list', async ({ page, loginAsAdmin }) => {
    await loginAsAdmin();
    await page.goto('/admin/people');
    await page.getByText('John Smith').first().click();
    await expect(page).toHaveURL(/\/admin\/people\/[A-Za-z0-9_-]+$/);
    await expect(page.getByText('John Smith').first()).toBeVisible();
    await expect(page.getByText('john.smith@example.com').first()).toBeVisible();
  });

  test('login rejects bad credentials with a visible error', async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Email').fill('john.smith@example.com');
    await page.getByLabel('Password').fill('wrong-password');
    await page.getByRole('button', { name: 'Sign In' }).click();
    // Stays on login and surfaces an error — never a silent failure or a crash.
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByText(/invalid|incorrect|failed/i).first()).toBeVisible();
  });

  test('check-in kiosk boots and probes the print bridge', async ({ page }) => {
    // Install the bridge mock BEFORE navigation: CheckinPage probes printer
    // availability on mount. This also proves the app points at
    // localhost:9632 — the contract the real bridge serves.
    const bridge = await installPrintBridgeMock(page);
    await page.goto('/checkin');
    await expect(page).toHaveURL(/\/checkin/);
    // Kiosk landed on its search step (phone search is the default mode).
    await expect(page.locator('body')).toContainText(/check.?in/i);
    expect(bridge.mode()).toBe('ok');
  });
});
