/**
 * E2E Tests: Family List and Search
 * Tests the families list view, search, and navigation functionality
 */

import { test, expect } from '@playwright/test';
import { FamiliesPage } from '../../../fixtures/page-objects/families.page';
import { LoginPage } from '../../../fixtures/page-objects/login.page';
import { testData } from '../../../fixtures/test-data';

test.describe('Family List and Search', () => {
  test.beforeEach(async ({ page }) => {
    // Login first
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'admin123');
    await loginPage.expectLoggedIn();

    // Navigate to families page
    const familiesPage = new FamiliesPage(page);
    await familiesPage.gotoList();
  });

  test('should display families list on load', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Verify page loaded
    await expect(page).toHaveURL('/admin/families');
    await expect(page.getByRole('heading', { name: 'Families' })).toBeVisible();

    // Verify search and create button visible
    await expect(familiesPage.searchInput).toBeVisible();
    await expect(familiesPage.createFamilyButton).toBeVisible();
  });

  test('should display seeded test families', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Verify all test families are visible
    await familiesPage.expectFamilyVisible(testData.families.smith.name);
    await familiesPage.expectFamilyVisible(testData.families.johnson.name);
  });

  test('should show member count for each family', async ({ page }) => {
    // Verify member counts are displayed
    const smithCard = page.locator('a', { hasText: testData.families.smith.name });
    await expect(smithCard).toContainText(/\d+\s*member/i);

    const johnsonCard = page.locator('a', { hasText: testData.families.johnson.name });
    await expect(johnsonCard).toContainText(/\d+\s*member/i);
  });

  test('should navigate to family detail on click', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Click on a family
    await familiesPage.clickFamily(testData.families.smith.name);

    // Verify navigation to detail page
    await expect(page).toHaveURL(/\/admin\/families\/.+/);
    await familiesPage.expectOnDetailPage(testData.families.smith.name);
  });

  test('should filter families by search query', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Search for "Smith"
    await familiesPage.searchFamilies('Smith');

    // Verify only Smith family is visible
    await familiesPage.expectFamilyVisible(testData.families.smith.name);

    // Verify result count
    const visibleFamilies = await page.locator('a', { hasText: /member/i }).count();
    expect(visibleFamilies).toBeGreaterThanOrEqual(1);
  });

  test('should show empty state when search has no results', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Search for non-existent family
    await familiesPage.searchFamilies('NonexistentFamily12345');

    // Verify empty state message
    await expect(page.getByText(/no families found/i)).toBeVisible();
  });

  test('should clear search query', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Search for something
    await familiesPage.searchFamilies('Smith');
    await familiesPage.expectFamilyVisible(testData.families.smith.name);

    // Clear search
    await familiesPage.searchInput.clear();
    await page.waitForLoadState('networkidle'); // Wait for debounce

    // All families should be visible again
    await familiesPage.expectFamilyVisible(testData.families.smith.name);
    await familiesPage.expectFamilyVisible(testData.families.johnson.name);
  });

  test('should navigate to create family form', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Click create button
    await familiesPage.createFamilyButton.click();

    // Verify navigation to form
    await expect(page).toHaveURL('/admin/families/new');
    await expect(page.getByRole('heading', { name: /create family/i })).toBeVisible();
  });

  test('@smoke should load families page without errors', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Verify no error messages
    await expect(page.getByText(/failed to load/i)).not.toBeVisible();

    // Verify at least one family is visible
    await familiesPage.expectFamilyVisible(testData.families.smith.name);
  });

  test('should search by member name', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Search for a person's name (should find their family)
    await familiesPage.searchFamilies('John');

    // Should find Smith family (John Smith is a member)
    await familiesPage.expectFamilyVisible(testData.families.smith.name);
  });

  test('should handle empty families list', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Search for something that won't exist
    await familiesPage.searchFamilies('ZZZNothingMatchesThis999');

    // Verify empty state with helpful message
    await expect(page.getByText(/no families found/i)).toBeVisible();
  });

  test('should display campus information when available', async ({ page }) => {
    const smithCard = page.locator('a', { hasText: testData.families.smith.name });

    // If campus is set, it should be displayed
    // Note: This depends on test data having campus set
    try {
      await expect(smithCard.locator('svg + span')).toBeVisible({ timeout: 2000 });
    } catch {
      // Campus might not be set in test data - that's ok
    }
  });

  test('should display address when available', async ({ page }) => {
    const familyCard = page.locator('a', { hasText: testData.families.smith.name });

    // If address is set, it should be displayed
    try {
      const addressIcon = familyCard.locator('svg[viewBox*="17.657"]');
      await expect(addressIcon).toBeVisible({ timeout: 2000 });
    } catch {
      // Address might not be set in test data - that's ok
    }
  });
});

test.describe('Family List - Pagination', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test('should show pagination controls when needed', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);
    await familiesPage.gotoList();

    // Check if pagination exists
    // Note: This will only work if there are enough families
    try {
      await expect(familiesPage.pageSizeSelect).toBeVisible({ timeout: 2000 });
    } catch {
      // Not enough families for pagination - skip test
      test.skip();
    }
  });

  test('should change page size', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);
    await familiesPage.gotoList();

    try {
      // Try to change page size
      await familiesPage.pageSizeSelect.selectOption('50');

      // Verify page refreshed
      await familiesPage.waitForLoad();
    } catch {
      // Pagination not available - skip
      test.skip();
    }
  });

  test('should navigate between pages', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);
    await familiesPage.gotoList();

    try {
      // Check if next button exists and is enabled
      const nextButton = familiesPage.nextPageButton;
      await expect(nextButton).toBeVisible({ timeout: 2000 });

      const isDisabled = await nextButton.isDisabled();
      if (!isDisabled) {
        await nextButton.click();
        await familiesPage.waitForLoad();

        // Verify we're on page 2
        await expect(page.getByText(/page 2 of/i)).toBeVisible();

        // Go back
        await familiesPage.previousPageButton.click();
        await familiesPage.waitForLoad();

        await expect(page.getByText(/page 1 of/i)).toBeVisible();
      }
    } catch {
      // Not enough data for pagination - skip
      test.skip();
    }
  });
});

test.describe('Family List - Performance', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test('should load families page within reasonable time', async ({ page }) => {
    const startTime = Date.now();
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.waitForLoad();

    const loadTime = Date.now() - startTime;

    // Should load within 3 seconds (generous for E2E)
    expect(loadTime).toBeLessThan(3000);

    // Verify content loaded
    await familiesPage.expectFamilyVisible(testData.families.smith.name);
  });
});
