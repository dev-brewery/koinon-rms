/**
 * E2E Tests: Supervisor Mode Workflow
 * Tests supervisor authentication, mode activation, and supervisor-only features
 *
 * ASSUMPTIONS:
 * - Supervisor button visible in check-in kiosk header
 * - PIN authentication via number pad modal
 * - Valid test PIN: 1234
 * - Three supervisor tabs: Reprint Labels, Checkout, Page Parent
 * - Triple-tap header activates supervisor mode (alternative)
 */

import { test, expect } from '@playwright/test';
import { CheckinPage } from '../../fixtures/page-objects/checkin.page';

test.describe('Supervisor Mode', () => {
  test.beforeEach(async ({ page }) => {
    const checkin = new CheckinPage(page);
    await checkin.goto();

    // Setup API mocks for supervisor login
    await page.route('**/api/v1/checkin/supervisor/login', async (route) => {
      const request = route.request();
      const body = JSON.parse(request.postData() || '{}');

      if (body.pin === '1234') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            sessionToken: 'test-supervisor-token-123',
            supervisor: {
              idKey: 'abc123xyz',
              fullName: 'Test Supervisor',
            },
            expiresAt: new Date(Date.now() + 120000).toISOString(), // 2 minutes
          }),
        });
      } else {
        await route.fulfill({
          status: 401,
          contentType: 'application/json',
          body: JSON.stringify({ error: 'Invalid PIN' }),
        });
      }
    });

    await page.route('**/api/v1/checkin/supervisor/logout', async (route) => {
      await route.fulfill({
        status: 204,
        contentType: 'application/json',
      });
    });
  });

  test('should show supervisor button in header', async ({ page }) => {
    // Supervisor button should be visible
    const supervisorButton = page.getByRole('button', { name: /supervisor/i });
    await expect(supervisorButton).toBeVisible();
  });

  test('should open PIN entry on supervisor button click', async ({ page }) => {
    await page.getByRole('button', { name: /supervisor/i }).click();

    // PIN entry modal should appear
    await expect(page.getByText('Supervisor PIN')).toBeVisible();
    await expect(page.getByRole('button', { name: '1' })).toBeVisible();
    await expect(page.getByRole('button', { name: '0' })).toBeVisible();
  });

  test('should close PIN entry on cancel', async ({ page }) => {
    await page.getByRole('button', { name: /supervisor/i }).click();
    await expect(page.getByText('Supervisor PIN')).toBeVisible();

    await page.getByRole('button', { name: 'Cancel' }).click();
    await expect(page.getByText('Supervisor PIN')).not.toBeVisible();
  });

  test('should show error on invalid PIN', async ({ page }) => {
    await page.getByRole('button', { name: /supervisor/i }).click();

    // Enter invalid PIN (0000)
    for (let i = 0; i < 4; i++) {
      await page.getByRole('button', { name: '0' }).click();
    }
    await page.getByRole('button', { name: 'Submit' }).click();

    // Should show error
    await expect(page.getByText(/invalid pin/i)).toBeVisible();
  });

  test('@smoke should authenticate with valid PIN and show supervisor mode', async ({ page }) => {
    await page.getByRole('button', { name: /supervisor/i }).click();

    // Enter test PIN (1234)
    await page.getByRole('button', { name: '1' }).click();
    await page.getByRole('button', { name: '2' }).click();
    await page.getByRole('button', { name: '3' }).click();
    await page.getByRole('button', { name: '4' }).click();
    await page.getByRole('button', { name: 'Submit' }).click();

    // Wait for API response and supervisor mode to show
    await expect(page.getByText('Supervisor Mode')).toBeVisible({ timeout: 5000 });
    await expect(page.getByText(/logged in as|test supervisor/i)).toBeVisible();
  });

  test('should display all supervisor tabs', async ({ page }) => {
    // Authenticate
    await page.getByRole('button', { name: /supervisor/i }).click();
    await page.getByRole('button', { name: '1' }).click();
    await page.getByRole('button', { name: '2' }).click();
    await page.getByRole('button', { name: '3' }).click();
    await page.getByRole('button', { name: '4' }).click();
    await page.getByRole('button', { name: 'Submit' }).click();
    await expect(page.getByText('Supervisor Mode')).toBeVisible();

    // Check tabs are visible
    await expect(page.getByRole('button', { name: 'Reprint Labels' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Checkout' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Page Parent' })).toBeVisible();
  });

  test('should exit supervisor mode on exit button click', async ({ page }) => {
    // Authenticate
    await page.getByRole('button', { name: /supervisor/i }).click();
    await page.getByRole('button', { name: '1' }).click();
    await page.getByRole('button', { name: '2' }).click();
    await page.getByRole('button', { name: '3' }).click();
    await page.getByRole('button', { name: '4' }).click();
    await page.getByRole('button', { name: 'Submit' }).click();
    await expect(page.getByText('Supervisor Mode')).toBeVisible();

    // Exit supervisor mode
    await page.getByRole('button', { name: /exit supervisor/i }).click();

    // Should return to normal kiosk view
    await expect(page.getByText('Supervisor Mode')).not.toBeVisible();
    await expect(page.getByRole('button', { name: /supervisor/i })).toBeVisible();
  });

  test('should switch between tabs', async ({ page }) => {
    // Authenticate
    await page.getByRole('button', { name: /supervisor/i }).click();
    await page.getByRole('button', { name: '1' }).click();
    await page.getByRole('button', { name: '2' }).click();
    await page.getByRole('button', { name: '3' }).click();
    await page.getByRole('button', { name: '4' }).click();
    await page.getByRole('button', { name: 'Submit' }).click();
    await expect(page.getByText('Supervisor Mode')).toBeVisible();

    // Default tab is Reprint Labels
    await expect(page.getByText('Current Check-Ins')).toBeVisible();

    // Switch to Checkout tab
    await page.getByRole('button', { name: 'Checkout' }).click();
    await expect(page.getByText(/security code/i)).toBeVisible();

    // Switch to Page Parent tab
    await page.getByRole('button', { name: 'Page Parent' }).click();
    await expect(page.getByText(/page parent/i)).toBeVisible();
  });

  test('should clear PIN input on backspace', async ({ page }) => {
    await page.getByRole('button', { name: /supervisor/i }).click();

    // Enter 3 digits
    await page.getByRole('button', { name: '1' }).click();
    await page.getByRole('button', { name: '2' }).click();
    await page.getByRole('button', { name: '3' }).click();

    // Backspace
    const backspaceButton = page.getByRole('button', { name: /backspace|delete|clear/i });
    await backspaceButton.click();

    // PIN should be incomplete (3 digits → 2 digits)
    // Submit should be disabled or show error
    await page.getByRole('button', { name: 'Submit' }).click();
    await expect(page.getByText(/4 digits|complete pin/i)).toBeVisible();
  });

  test('should prevent submission until 4 digits entered', async ({ page }) => {
    await page.getByRole('button', { name: /supervisor/i }).click();

    // Try to submit with 0 digits
    const submitButton = page.getByRole('button', { name: 'Submit' });
    await expect(submitButton).toBeDisabled();

    // Enter 2 digits
    await page.getByRole('button', { name: '1' }).click();
    await page.getByRole('button', { name: '2' }).click();

    // Still disabled
    await expect(submitButton).toBeDisabled();

    // Enter remaining 2 digits
    await page.getByRole('button', { name: '3' }).click();
    await page.getByRole('button', { name: '4' }).click();

    // Now enabled
    await expect(submitButton).toBeEnabled();
  });

  test('should mask PIN digits', async ({ page }) => {
    await page.getByRole('button', { name: /supervisor/i }).click();

    // Enter digits
    await page.getByRole('button', { name: '1' }).click();
    await page.getByRole('button', { name: '2' }).click();

    // PIN display should show masked characters (e.g., '••' or '**')
    const pinDisplay = page.getByTestId('pin-display');
    await expect(pinDisplay).toHaveText(/[•*]{2}/);

    // Should NOT show actual digits
    await expect(pinDisplay).not.toHaveText('12');
  });

  test.skip('should show session timeout warning', async ({ page }) => {
    // Skipped: Requires extended timeout config (110s wait)
    // Enable with: ENABLE_TIMEOUT_TESTS=1 and extended playwright timeout
    // Authenticate
    await page.getByRole('button', { name: /supervisor/i }).click();
    await page.getByRole('button', { name: '1' }).click();
    await page.getByRole('button', { name: '2' }).click();
    await page.getByRole('button', { name: '3' }).click();
    await page.getByRole('button', { name: '4' }).click();
    await page.getByRole('button', { name: 'Submit' }).click();
    await expect(page.getByText('Supervisor Mode')).toBeVisible();

    // Fast-forward time to near expiration (mock or wait)
    // NOTE: This test may need session timeout to be configurable in test env
    await page.waitForTimeout(110000); // Wait 1:50 (10s before 2min expiration)

    // Should show warning
    await expect(page.getByText(/session expiring|session will expire/i)).toBeVisible({ timeout: 15000 });
  });

  test.skip('should auto-logout on session expiration', async ({ page }) => {
    // Skipped: Requires extended timeout config (125s wait)
    // Enable with: ENABLE_TIMEOUT_TESTS=1 and extended playwright timeout
    // Authenticate
    await page.getByRole('button', { name: /supervisor/i }).click();
    await page.getByRole('button', { name: '1' }).click();
    await page.getByRole('button', { name: '2' }).click();
    await page.getByRole('button', { name: '3' }).click();
    await page.getByRole('button', { name: '4' }).click();
    await page.getByRole('button', { name: 'Submit' }).click();
    await expect(page.getByText('Supervisor Mode')).toBeVisible();

    // Fast-forward past expiration (2 minutes)
    await page.waitForTimeout(125000); // 2:05

    // Should auto-exit supervisor mode
    await expect(page.getByText('Supervisor Mode')).not.toBeVisible({ timeout: 5000 });
    await expect(page.getByRole('button', { name: /supervisor/i })).toBeVisible();
  });
});

test.describe('Supervisor Mode - Triple Tap', () => {
  test.beforeEach(async ({ page }) => {
    const checkin = new CheckinPage(page);
    await checkin.goto();

    // Setup API mock
    await page.route('**/api/v1/checkin/supervisor/login', async (route) => {
      const request = route.request();
      const body = JSON.parse(request.postData() || '{}');

      if (body.pin === '1234') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            sessionToken: 'test-supervisor-token-123',
            supervisor: {
              idKey: 'abc123xyz',
              fullName: 'Test Supervisor',
            },
            expiresAt: new Date(Date.now() + 120000).toISOString(),
          }),
        });
      } else {
        await route.fulfill({
          status: 401,
          contentType: 'application/json',
          body: JSON.stringify({ error: 'Invalid PIN' }),
        });
      }
    });
  });

  test('should open PIN entry on triple-tap header logo', async ({ page }) => {
    // Triple-tap the header logo area
    const header = page.locator('header').getByText('Check-In').first();
    await header.click();
    await header.click();
    await header.click();

    // PIN entry should appear
    await expect(page.getByText('Supervisor PIN')).toBeVisible();
  });

  test('should not trigger on double-tap', async ({ page }) => {
    // Double-tap should not open PIN entry
    const header = page.locator('header').getByText('Check-In').first();
    await header.click();
    await header.click();

    // Wait a moment
    await page.waitForTimeout(500);

    // PIN entry should NOT appear
    await expect(page.getByText('Supervisor PIN')).not.toBeVisible();
  });

  test('should reset tap count after timeout', async ({ page }) => {
    const header = page.locator('header').getByText('Check-In').first();

    // First two taps
    await header.click();
    await header.click();

    // Wait for tap timeout (typically 500-1000ms)
    await page.waitForTimeout(1500);

    // Third tap (should not trigger after timeout)
    await header.click();

    // PIN entry should NOT appear
    await expect(page.getByText('Supervisor PIN')).not.toBeVisible();
  });
});

test.describe('Supervisor Mode - Kiosk Touch', () => {
  test.use({ viewport: { width: 1024, height: 768 }, hasTouch: true });

  test.beforeEach(async ({ page }) => {
    const checkin = new CheckinPage(page);
    await checkin.goto();

    // Setup API mock
    await page.route('**/api/v1/checkin/supervisor/login', async (route) => {
      const request = route.request();
      const body = JSON.parse(request.postData() || '{}');

      if (body.pin === '1234') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            sessionToken: 'test-supervisor-token-123',
            supervisor: {
              idKey: 'abc123xyz',
              fullName: 'Test Supervisor',
            },
            expiresAt: new Date(Date.now() + 120000).toISOString(),
          }),
        });
      }
    });
  });

  test('should have touch-friendly PIN buttons', async ({ page }) => {
    await page.getByRole('button', { name: /supervisor/i }).click();

    // Check button has minimum touch target size (48px)
    const pinButton = page.getByRole('button', { name: '1' });
    const buttonBox = await pinButton.boundingBox();

    expect(buttonBox?.height).toBeGreaterThanOrEqual(48);
    expect(buttonBox?.width).toBeGreaterThanOrEqual(48);
  });

  test('should have touch-friendly supervisor tabs', async ({ page }) => {
    // Authenticate
    await page.getByRole('button', { name: /supervisor/i }).click();
    await page.getByRole('button', { name: '1' }).click();
    await page.getByRole('button', { name: '2' }).click();
    await page.getByRole('button', { name: '3' }).click();
    await page.getByRole('button', { name: '4' }).click();
    await page.getByRole('button', { name: 'Submit' }).click();
    await expect(page.getByText('Supervisor Mode')).toBeVisible();

    // Check tab button size
    const tabButton = page.getByRole('button', { name: 'Checkout' });
    const tabBox = await tabButton.boundingBox();

    expect(tabBox?.height).toBeGreaterThanOrEqual(48);
  });
});
