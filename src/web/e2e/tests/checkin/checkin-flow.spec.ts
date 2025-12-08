import { test, expect } from '@playwright/test';
import { CheckinPage } from '../../fixtures/page-objects/checkin.page';

test.describe('Check-in Flow', () => {
  test.beforeEach(async ({ page }) => {
    const checkin = new CheckinPage(page);
    await checkin.goto();
  });

  test('should display phone input on load', async ({ page }) => {
    const checkin = new CheckinPage(page);

    await expect(checkin.phoneInput).toBeVisible();
    await expect(checkin.phoneInput).toBeFocused();
  });

  test('should find family by phone number', async ({ page }) => {
    const checkin = new CheckinPage(page);

    await checkin.searchByPhone('5551234567');
    await expect(checkin.familyMemberCards.first()).toBeVisible();
  });

  test('should display multiple family members', async ({ page }) => {
    const checkin = new CheckinPage(page);

    await checkin.searchByPhone('5551234567');

    const memberCount = await checkin.familyMemberCards.count();
    expect(memberCount).toBeGreaterThan(0);
  });

  test('should allow selecting a family member', async ({ page }) => {
    const checkin = new CheckinPage(page);

    await checkin.searchByPhone('5551234567');
    await checkin.selectMember(0);

    // Member should show selected state
    await expect(checkin.familyMemberCards.first()).toHaveAttribute('data-selected', 'true');
  });

  test('@smoke should complete full check-in flow', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Search for family
    await checkin.searchByPhone('5551234567');

    // Select first member
    await checkin.selectMember(0);

    // Confirm check-in
    await checkin.confirmCheckin();

    // Verify success
    await checkin.expectSuccess();
  });

  test('should handle idle timeout warning', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Wait for idle warning (use shorter timeout in test config)
    await page.waitForSelector('[role="alertdialog"]', { timeout: 15000 });

    // Modal should be visible
    await expect(checkin.idleWarningModal).toBeVisible();

    // Dismiss and verify it closes
    await checkin.dismissIdleWarning();
  });

  test('should show error for non-existent phone', async ({ page }) => {
    const checkin = new CheckinPage(page);

    await checkin.enterPhone('0000000000');
    await checkin.submitPhone();

    // Should show no results message
    await expect(page.getByText(/no family found|not found/i)).toBeVisible();
  });

  test('should validate phone number format', async ({ page }) => {
    const checkin = new CheckinPage(page);

    await checkin.enterPhone('123'); // Too short
    await checkin.submitPhone();

    // Should show validation error
    await expect(page.getByText(/valid phone|10 digits/i)).toBeVisible();
  });
});

test.describe('Check-in Kiosk Mode', () => {
  test.use({ viewport: { width: 1024, height: 768 }, hasTouch: true });

  test('should have touch-friendly buttons', async ({ page }) => {
    const checkin = new CheckinPage(page);
    await checkin.goto();

    // Check button has minimum touch target size (48px)
    const buttonBox = await checkin.searchButton.boundingBox();
    expect(buttonBox?.height).toBeGreaterThanOrEqual(48);
    expect(buttonBox?.width).toBeGreaterThanOrEqual(48);
  });
});
