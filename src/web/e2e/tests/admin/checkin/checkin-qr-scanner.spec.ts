/**
 * E2E Tests: Check-in QR Code Scanner
 * Tests QR code scanning workflow
 *
 * ASSUMPTIONS:
 * - QR code contains family ID or phone number
 * - Camera permission dialog handled
 * - QR scanner library integrated (e.g., jsQR, zxing)
 * - Fallback to manual entry available
 * - Scanner works in kiosk mode (touch UI)
 */

import { test, expect } from '@playwright/test';
import { CheckinPage } from '../../../fixtures/page-objects/checkin.page';

test.describe('Check-in QR Code Scanner', () => {
  test.beforeEach(async ({ page }) => {
    const checkin = new CheckinPage(page);
    await checkin.goto();
  });

  test('should display QR scanner button', async ({ page }) => {
    await expect(page.getByTestId('qr-scanner-button')).toBeVisible();
    await expect(
      page.getByRole('button', { name: /scan qr|qr code/i })
    ).toBeVisible();
  });

  test('should open camera on QR button click', async ({ page }) => {
    // Grant camera permission
    await page.context().grantPermissions(['camera']);

    await page.getByTestId('qr-scanner-button').click();

    // Scanner modal should open
    await expect(page.getByTestId('qr-scanner-modal')).toBeVisible();
    await expect(page.getByRole('heading', { name: /scan qr code/i })).toBeVisible();

    // Video element should be present
    await expect(page.locator('video[data-testid="qr-scanner-video"]')).toBeVisible();
  });

  test('should handle camera permission denial', async ({ page }) => {
    // Deny camera permission
    await page.context().grantPermissions([]);

    await page.getByTestId('qr-scanner-button').click();

    // Should show permission error
    await expect(
      page.getByText(/camera permission|allow camera/i)
    ).toBeVisible();

    // Should show fallback option
    await expect(
      page.getByRole('button', { name: /enter manually|use phone/i })
    ).toBeVisible();
  });

  test('should scan QR code and load family', async ({ page, context }) => {
    // Grant camera permission
    await context.grantPermissions(['camera']);

    // Mock QR code scan result
    await page.exposeFunction('mockQRScan', () => {
      return JSON.stringify({
        type: 'family',
        id: 'ABC123', // IdKey
        phone: '5551234567',
      });
    });

    // Open scanner
    await page.getByTestId('qr-scanner-button').click();
    await expect(page.getByTestId('qr-scanner-modal')).toBeVisible();

    // Simulate QR detection using 'qr-detected' custom event
    // NOTE: This event name must match app implementation in QR scanner component
    // TODO(#157): Verify custom event name matches actual implementation
    await page.evaluate(async () => {
      const qrData = await (window as any).mockQRScan();
      const event = new CustomEvent('qr-detected', { detail: qrData });
      document.dispatchEvent(event);
    });

    // Should close scanner and load family
    await expect(page.getByTestId('qr-scanner-modal')).not.toBeVisible();

    // Family members should be displayed
    const checkin = new CheckinPage(page);
    await expect(checkin.familyMemberCards.first()).toBeVisible();
  });

  test('should handle invalid QR code format', async ({ page, context }) => {
    await context.grantPermissions(['camera']);

    await page.getByTestId('qr-scanner-button').click();

    // Simulate invalid QR data
    await page.evaluate(() => {
      const event = new CustomEvent('qr-detected', {
        detail: 'invalid-data-format',
      });
      document.dispatchEvent(event);
    });

    // Should show error message
    await expect(
      page.getByText(/invalid qr code|not recognized/i)
    ).toBeVisible();

    // Scanner should remain open for retry
    await expect(page.getByTestId('qr-scanner-modal')).toBeVisible();
  });

  test('should handle QR code for non-existent family', async ({ page, context }) => {
    await context.grantPermissions(['camera']);

    await page.getByTestId('qr-scanner-button').click();

    // Simulate QR for non-existent family
    await page.evaluate(() => {
      const event = new CustomEvent('qr-detected', {
        detail: JSON.stringify({
          type: 'family',
          id: 'NOTFOUND',
          phone: '0000000000',
        }),
      });
      document.dispatchEvent(event);
    });

    // Should show family not found error
    await expect(
      page.getByText(/family not found|no family/i)
    ).toBeVisible();

    // Should offer to try again
    await expect(
      page.getByRole('button', { name: /try again|scan again/i })
    ).toBeVisible();
  });

  test('should close scanner on cancel', async ({ page, context }) => {
    await context.grantPermissions(['camera']);

    await page.getByTestId('qr-scanner-button').click();
    await expect(page.getByTestId('qr-scanner-modal')).toBeVisible();

    // Click cancel button
    await page.getByRole('button', { name: /cancel|close/i }).click();

    // Modal should close
    await expect(page.getByTestId('qr-scanner-modal')).not.toBeVisible();

    // Should return to phone input
    const checkin = new CheckinPage(page);
    await expect(checkin.phoneInput).toBeVisible();
  });

  test('should show scanning indicator while detecting', async ({ page, context }) => {
    await context.grantPermissions(['camera']);

    await page.getByTestId('qr-scanner-button').click();

    // Scanner should show active state
    await expect(
      page.getByText(/scanning|point camera/i)
    ).toBeVisible();

    // Should show viewfinder overlay
    await expect(page.getByTestId('qr-scanner-overlay')).toBeVisible();
  });

  test('should handle phone number QR code format', async ({ page, context }) => {
    await context.grantPermissions(['camera']);

    await page.getByTestId('qr-scanner-button').click();

    // Simulate QR with just phone number
    await page.evaluate(() => {
      const event = new CustomEvent('qr-detected', {
        detail: '5551234567', // Plain phone number
      });
      document.dispatchEvent(event);
    });

    // Should load family by phone
    const checkin = new CheckinPage(page);
    await expect(checkin.familyMemberCards.first()).toBeVisible();
  });

  test('should switch to manual entry from scanner', async ({ page, context }) => {
    await context.grantPermissions(['camera']);

    await page.getByTestId('qr-scanner-button').click();
    await expect(page.getByTestId('qr-scanner-modal')).toBeVisible();

    // Click manual entry button
    await page.getByRole('button', { name: /manual|enter phone/i }).click();

    // Should close scanner and focus phone input
    await expect(page.getByTestId('qr-scanner-modal')).not.toBeVisible();
    const checkin = new CheckinPage(page);
    await expect(checkin.phoneInput).toBeFocused();
  });

  test('should work in landscape orientation', async ({ page, context }) => {
    // Set landscape viewport
    await page.setViewportSize({ width: 1024, height: 600 });
    await context.grantPermissions(['camera']);

    await page.getByTestId('qr-scanner-button').click();

    // Scanner should adapt to landscape
    const modal = page.getByTestId('qr-scanner-modal');
    await expect(modal).toBeVisible();

    const box = await modal.boundingBox();
    expect(box?.width).toBeGreaterThan(box?.height || 0);
  });

  test('@smoke should complete full QR scan workflow', async ({ page, context }) => {
    await context.grantPermissions(['camera']);
    const checkin = new CheckinPage(page);

    // Step 1: Click QR scanner button
    await page.getByTestId('qr-scanner-button').click();
    await expect(page.getByTestId('qr-scanner-modal')).toBeVisible();

    // Step 2: Mock successful QR scan
    await page.evaluate(() => {
      const event = new CustomEvent('qr-detected', {
        detail: JSON.stringify({
          type: 'family',
          phone: '5551234567',
        }),
      });
      document.dispatchEvent(event);
    });

    // Step 3: Verify family loaded
    await expect(checkin.familyMemberCards.first()).toBeVisible();

    // Step 4: Select member and check in
    await checkin.selectMember(0);
    await checkin.confirmCheckin();

    // Step 5: Verify success
    await expect(checkin.successMessage).toBeVisible();
  });

  test('should show torch toggle in low light', async ({ page, context }) => {
    await context.grantPermissions(['camera']);

    await page.getByTestId('qr-scanner-button').click();

    // Torch/flashlight button should be available
    await expect(
      page.getByTestId('torch-toggle').or(page.getByRole('button', { name: /flash|light/i }))
    ).toBeVisible();
  });

  test('should timeout after 60 seconds of no scan', async ({ page, context }) => {
    await context.grantPermissions(['camera']);

    await page.getByTestId('qr-scanner-button').click();

    // NOTE: This test assumes QR scanner component has configurable timeout
    // Production timeout is 60s, but test environment should mock to 5s
    // TODO(#157): Verify timeout is configurable via test environment variable
    // Expected: QR_SCANNER_TIMEOUT=5000 in test config
    await page.waitForTimeout(5000);

    // Should show timeout message triggered by app's timeout handler
    await expect(
      page.getByText(/timeout|taking too long/i)
    ).toBeVisible({ timeout: 10000 });

    // Should offer retry
    await expect(
      page.getByRole('button', { name: /try again|retry/i })
    ).toBeVisible();
  });

  test('should support multiple camera selection', async ({ page, context }) => {
    await context.grantPermissions(['camera']);

    await page.getByTestId('qr-scanner-button').click();

    // Check if multiple cameras available
    const cameraCount = await page.evaluate(async () => {
      const devices = await navigator.mediaDevices.enumerateDevices();
      return devices.filter((d) => d.kind === 'videoinput').length;
    });

    if (cameraCount > 1) {
      // Camera switcher should be visible
      await expect(
        page.getByTestId('camera-switcher').or(page.getByRole('button', { name: /switch camera/i }))
      ).toBeVisible();
    }
  });

  test('should handle rapid QR scans (debounce)', async ({ page, context }) => {
    await context.grantPermissions(['camera']);

    await page.getByTestId('qr-scanner-button').click();

    // Simulate multiple rapid scans
    await page.evaluate(() => {
      for (let i = 0; i < 5; i++) {
        const event = new CustomEvent('qr-detected', {
          detail: JSON.stringify({ type: 'family', phone: '5551234567' }),
        });
        document.dispatchEvent(event);
      }
    });

    // Should only process once (debounced)
    const checkin = new CheckinPage(page);
    await expect(checkin.familyMemberCards.first()).toBeVisible();

    // Should not have multiple API calls (check network tab)
    const requestCount = await page.evaluate(() => {
      return performance
        .getEntriesByType('resource')
        .filter((e) => e.name.includes('/api/v1/families/search')).length;
    });
    expect(requestCount).toBeLessThanOrEqual(1);
  });
});

test.describe('Check-in QR Code Accessibility', () => {
  test('should have accessible scanner controls', async ({ page, context }) => {
    await context.grantPermissions(['camera']);

    await page.getByTestId('qr-scanner-button').click();

    // Modal should have proper ARIA attributes
    const modal = page.getByTestId('qr-scanner-modal');
    await expect(modal).toHaveAttribute('role', 'dialog');
    await expect(modal).toHaveAttribute('aria-label');

    // Close button should be accessible
    const closeButton = page.getByRole('button', { name: /cancel|close/i });
    await expect(closeButton).toBeVisible();
  });

  test('should announce scan results to screen readers', async ({ page, context }) => {
    await context.grantPermissions(['camera']);

    await page.getByTestId('qr-scanner-button').click();

    // Simulate successful scan
    await page.evaluate(() => {
      const event = new CustomEvent('qr-detected', {
        detail: JSON.stringify({ type: 'family', phone: '5551234567' }),
      });
      document.dispatchEvent(event);
    });

    // Should have aria-live region announcing result
    await expect(
      page.locator('[role="status"]').or(page.locator('[aria-live="polite"]'))
    ).toContainText(/family found|loaded/i);
  });
});
