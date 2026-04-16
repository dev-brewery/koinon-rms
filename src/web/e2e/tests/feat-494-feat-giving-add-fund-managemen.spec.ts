/**
 * E2E Tests: Feature #494 — Fund Management Admin UI
 * Covers admin management of giving funds (create, edit, deactivate, status display).
 */

import { test, expect } from '../fixtures/auth.fixture';

test.describe('Fund Management Admin', () => {
  test.beforeEach(async ({ loginAsAdmin, page }) => {
    await loginAsAdmin();
    await page.goto('/admin/settings/funds');
  });

  test('funds page loads and shows heading', async ({ page }) => {
    await expect(page.getByRole('heading', { name: /funds/i })).toBeVisible({ timeout: 10000 });
    await expect(page.locator('[data-testid="funds-page"]')).toBeVisible();
  });

  test('funds page is reachable from Settings', async ({ page }) => {
    await page.goto('/admin/settings');
    await expect(page.getByRole('heading', { name: /settings/i })).toBeVisible();
    const fundsCard = page.getByRole('link', { name: /funds/i });
    await expect(fundsCard).toBeVisible();
    await fundsCard.click();
    await expect(page).toHaveURL('/admin/settings/funds');
    await expect(page.getByRole('heading', { name: /funds/i })).toBeVisible();
  });

  test('fund list shows active/inactive status badges', async ({ page }) => {
    // Wait for the fund list to load
    const fundList = page.locator('[data-testid="fund-list"]');
    await expect(fundList).toBeVisible({ timeout: 10000 });

    // At least one fund should exist (seeded data or from prior test runs)
    // Check that status badges render
    const statusBadges = page.locator('[data-testid="fund-status-badge"]');
    await expect(statusBadges.first()).toBeVisible({ timeout: 10000 });
  });

  test('admin can create a new fund', async ({ page }) => {
    // Click create button
    const createBtn = page.locator('[data-testid="create-fund-btn"]');
    await expect(createBtn).toBeVisible({ timeout: 10000 });
    await createBtn.click();

    // Modal should open
    const form = page.locator('[data-testid="fund-form"]');
    await expect(form).toBeVisible({ timeout: 5000 });

    // Fill in fund name
    const nameInput = page.locator('[data-testid="fund-name-input"]');
    await expect(nameInput).toBeVisible();
    const uniqueName = `E2E Test Fund ${Date.now()}`;
    await nameInput.fill(uniqueName);

    // Submit
    const submitBtn = page.locator('[data-testid="fund-submit-btn"]');
    await submitBtn.click();

    // Fund should appear in the list
    await expect(page.getByText(uniqueName)).toBeVisible({ timeout: 10000 });
  });

  test('admin can edit a fund name and description', async ({ page }) => {
    // Wait for fund list
    const fundList = page.locator('[data-testid="fund-list"]');
    await expect(fundList).toBeVisible({ timeout: 10000 });

    // First create a fund to edit (ensures a known fund exists)
    const createBtn = page.locator('[data-testid="create-fund-btn"]');
    await createBtn.click();

    const form = page.locator('[data-testid="fund-form"]');
    await expect(form).toBeVisible({ timeout: 5000 });

    const uniqueName = `E2E Edit Target ${Date.now()}`;
    await page.locator('[data-testid="fund-name-input"]').fill(uniqueName);
    await page.locator('[data-testid="fund-submit-btn"]').click();

    // Wait for the new fund to appear
    await expect(page.getByText(uniqueName)).toBeVisible({ timeout: 10000 });

    // Find the fund item and click its edit button
    const fundItem = page.locator('[data-testid="fund-item"]').filter({ hasText: uniqueName });
    const editBtn = fundItem.locator('[data-testid="fund-edit-btn"]');
    await expect(editBtn).toBeVisible({ timeout: 5000 });
    await editBtn.click();

    // Edit the name
    const editForm = page.locator('[data-testid="fund-form"]');
    await expect(editForm).toBeVisible({ timeout: 5000 });

    const nameInput = page.locator('[data-testid="fund-name-input"]');
    await nameInput.clear();
    const updatedName = `${uniqueName} (updated)`;
    await nameInput.fill(updatedName);

    await page.locator('[data-testid="fund-submit-btn"]').click();

    // Updated name should appear
    await expect(page.getByText(updatedName)).toBeVisible({ timeout: 10000 });
  });

  test('admin can deactivate an active fund', async ({ page }) => {
    // First create a fund to deactivate
    const createBtn = page.locator('[data-testid="create-fund-btn"]');
    await expect(createBtn).toBeVisible({ timeout: 10000 });
    await createBtn.click();

    const form = page.locator('[data-testid="fund-form"]');
    await expect(form).toBeVisible({ timeout: 5000 });

    const uniqueName = `E2E Deactivate Target ${Date.now()}`;
    await page.locator('[data-testid="fund-name-input"]').fill(uniqueName);
    await page.locator('[data-testid="fund-submit-btn"]').click();

    // Wait for the fund to appear
    await expect(page.getByText(uniqueName)).toBeVisible({ timeout: 10000 });

    // Find and click deactivate button
    const fundItem = page.locator('[data-testid="fund-item"]').filter({ hasText: uniqueName });
    const deactivateBtn = fundItem.locator('[data-testid="fund-deactivate-btn"]');
    await expect(deactivateBtn).toBeVisible({ timeout: 5000 });
    await deactivateBtn.click();

    // Fund should now show as inactive
    await expect(
      fundItem.locator('[data-testid="fund-status-badge"]').filter({ hasText: /inactive/i })
    ).toBeVisible({ timeout: 10000 });
  });

  test('screenshot: funds management page', async ({ page }) => {
    await expect(page.locator('[data-testid="funds-page"]')).toBeVisible({ timeout: 10000 });
    // Wait for data to load
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveScreenshot('fund-management-admin.png');
  });
});
