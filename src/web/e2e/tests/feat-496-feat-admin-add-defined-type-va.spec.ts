/**
 * E2E Tests: Feature #496 — Defined Type/Value Management UI
 * Covers admin management of DefinedType and DefinedValue records.
 */

import { test, expect } from '../fixtures/auth.fixture';

test.describe('Defined Type/Value Management', () => {
  test.beforeEach(async ({ loginAsAdmin, page }) => {
    await loginAsAdmin();
    await page.goto('/admin/settings/defined-types');
  });

  test('admin can view all DefinedTypes on the page', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'Defined Types' })).toBeVisible();
    const typeRows = page.locator('[data-testid="defined-type-row"]');
    await expect(typeRows.first()).toBeVisible({ timeout: 10000 });
  });

  test('defined types page is reachable from Settings', async ({ page }) => {
    await page.goto('/admin/settings');
    await expect(page.getByRole('heading', { name: 'Settings' })).toBeVisible();
    const card = page.getByRole('link', { name: /defined types/i });
    await expect(card).toBeVisible();
    await card.click();
    await expect(page).toHaveURL('/admin/settings/defined-types');
    await expect(page.getByRole('heading', { name: 'Defined Types' })).toBeVisible();
  });

  test('admin can expand a type to see its values', async ({ page }) => {
    const firstTypeRow = page.locator('[data-testid="defined-type-row"]').first();
    await firstTypeRow.click();
    const valuesSection = page.locator('[data-testid="defined-values-section"]').first();
    await expect(valuesSection).toBeVisible({ timeout: 10000 });
  });

  test('system types show protected indicator', async ({ page }) => {
    const systemBadge = page.locator('[data-testid="system-type-badge"]').first();
    await expect(systemBadge).toBeVisible({ timeout: 10000 });
  });

  test('admin can add a new value to a type', async ({ page }) => {
    const firstTypeRow = page.locator('[data-testid="defined-type-row"]').first();
    await firstTypeRow.click();
    const addValueButton = page.getByRole('button', { name: /add value/i }).first();
    await expect(addValueButton).toBeVisible({ timeout: 10000 });
    await addValueButton.click();
    const valueInput = page.getByPlaceholder('Enter value').first();
    await expect(valueInput).toBeVisible({ timeout: 5000 });
    await valueInput.fill('E2E Test Value');
    const saveButton = page.getByRole('button', { name: /add value|save changes/i }).first();
    await saveButton.click();
    await expect(page.getByText('E2E Test Value').first()).toBeVisible({ timeout: 10000 });
  });

  test('admin can edit an existing value', async ({ page }) => {
    const firstTypeRow = page.locator('[data-testid="defined-type-row"]').first();
    await firstTypeRow.click();
    const valuesSection = page.locator('[data-testid="defined-values-section"]').first();
    await expect(valuesSection).toBeVisible({ timeout: 10000 });
    const editButton = page.getByRole('button', { name: /edit/i }).first();
    await expect(editButton).toBeVisible({ timeout: 5000 });
    await editButton.click();
    const valueInput = page.getByPlaceholder('Enter value').first();
    await expect(valueInput).toBeVisible({ timeout: 5000 });
  });

  test('screenshot: defined types management page with expanded type', async ({ page }) => {
    const firstTypeRow = page.locator('[data-testid="defined-type-row"]').first();
    await firstTypeRow.click();
    const valuesSection = page.locator('[data-testid="defined-values-section"]').first();
    await expect(valuesSection).toBeVisible({ timeout: 10000 });
    await expect(page).toHaveScreenshot('defined-types-management-expanded.png');
  });
});
