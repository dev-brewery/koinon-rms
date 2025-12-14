/**
 * E2E Tests: Room Roster Navigation
 * Tests navigation to roster page and basic functionality
 *
 * NOTE: This test validates that the roster feature is accessible and loads correctly.
 * Full roster functionality (location selection, attendance display) is tested separately.
 */

import { test, expect } from '@playwright/test';
import { RosterPage } from '../../../fixtures/page-objects/roster.page';
import { LoginPage } from '../../../fixtures/page-objects/login.page';

test.describe('Room Roster Navigation', () => {
  test.beforeEach(async ({ page }) => {
    // Login first
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('john.smith@example.com', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test('should navigate to roster page from admin sidebar', async ({ page }) => {
    // Navigate to admin dashboard
    await page.goto('/admin');
    await expect(page).toHaveURL('/admin');

    // Click on "Room Roster" in sidebar
    const rosterLink = page.getByRole('link', { name: /room roster/i });
    await expect(rosterLink).toBeVisible();
    await rosterLink.click();

    // Verify navigation to roster page
    await expect(page).toHaveURL('/admin/roster');
    await expect(page.getByRole('heading', { name: /room roster/i })).toBeVisible();
  });

  test('should display roster page elements on load', async ({ page }) => {
    const rosterPage = new RosterPage(page);

    await rosterPage.gotoRoster();

    // Verify page heading
    await rosterPage.expectOnRosterPage();

    // Verify location picker is visible
    await rosterPage.expectLocationPickerVisible();

    // Verify auto-refresh toggle is visible
    await expect(rosterPage.autoRefreshCheckbox).toBeVisible();

    // Verify refresh button is visible
    await expect(rosterPage.refreshButton).toBeVisible();
  });

  test('should show empty state when no location selected', async ({ page }) => {
    const rosterPage = new RosterPage(page);

    await rosterPage.gotoRoster();

    // Wait for page to load
    await rosterPage.expectOnRosterPage();

    // Should show empty state
    await rosterPage.expectEmptyState();
  });

  test('should display page description', async ({ page }) => {
    const rosterPage = new RosterPage(page);

    await rosterPage.gotoRoster();

    // Verify page description is visible
    await expect(
      page.getByText(/real-time view of children currently checked into rooms/i)
    ).toBeVisible();
  });

  test('should have auto-refresh enabled by default', async ({ page }) => {
    const rosterPage = new RosterPage(page);

    await rosterPage.gotoRoster();

    // Verify auto-refresh checkbox is checked by default
    await expect(rosterPage.autoRefreshCheckbox).toBeChecked();
  });

  test('should allow toggling auto-refresh', async ({ page }) => {
    const rosterPage = new RosterPage(page);

    await rosterPage.gotoRoster();

    // Verify starts checked
    await expect(rosterPage.autoRefreshCheckbox).toBeChecked();

    // Uncheck auto-refresh
    await rosterPage.autoRefreshCheckbox.uncheck();
    await expect(rosterPage.autoRefreshCheckbox).not.toBeChecked();

    // Re-check auto-refresh
    await rosterPage.autoRefreshCheckbox.check();
    await expect(rosterPage.autoRefreshCheckbox).toBeChecked();
  });

  test('should display refresh button with icon', async ({ page }) => {
    const rosterPage = new RosterPage(page);

    await rosterPage.gotoRoster();

    // Verify refresh button is visible and contains text
    await expect(rosterPage.refreshButton).toBeVisible();
    await expect(rosterPage.refreshButton).toContainText(/refresh/i);

    // Button should contain an SVG icon (aria-hidden)
    const icon = rosterPage.refreshButton.locator('svg[aria-hidden="true"]');
    await expect(icon).toBeVisible();
  });

  test('@smoke should complete basic roster page navigation', async ({ page }) => {
    const rosterPage = new RosterPage(page);

    // Navigate from admin to roster
    await page.goto('/admin');
    await page.getByRole('link', { name: /room roster/i }).click();

    // Verify landed on roster page
    await rosterPage.expectOnRosterPage();

    // Verify key UI elements present
    await rosterPage.expectLocationPickerVisible();
    await expect(rosterPage.autoRefreshCheckbox).toBeVisible();
    await expect(rosterPage.refreshButton).toBeVisible();
    await rosterPage.expectEmptyState();
  });
});
