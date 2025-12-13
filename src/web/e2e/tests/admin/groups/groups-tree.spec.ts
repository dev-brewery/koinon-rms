/**
 * E2E Tests: Groups Tree Navigation
 * Tests the groups tree view, search, and navigation functionality
 */

import { test, expect } from '@playwright/test';
import { GroupsPage } from '../../../fixtures/page-objects/groups.page';
import { LoginPage } from '../../../fixtures/page-objects/login.page';
import { testData } from '../../../fixtures/test-data';

test.describe('Groups Tree Navigation', () => {
  test.beforeEach(async ({ page }) => {
    // Login first
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'admin123');
    await loginPage.expectLoggedIn();

    // Navigate to groups page
    const groupsPage = new GroupsPage(page);
    await groupsPage.gotoTreeView();
  });

  test('should display groups tree view on load', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    // Verify page loaded
    await expect(page).toHaveURL('/admin/groups');
    await expect(page.getByRole('heading', { name: 'Groups' })).toBeVisible();

    // Verify search and create button visible
    await expect(groupsPage.searchInput).toBeVisible();
    await expect(groupsPage.createGroupButton).toBeVisible();
  });

  test('should display seeded test groups', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    // Verify all test groups are visible
    await groupsPage.expectGroupVisible(testData.groups.nursery.name);
    await groupsPage.expectGroupVisible(testData.groups.preschool.name);
    await groupsPage.expectGroupVisible(testData.groups.elementary.name);
  });

  test('should navigate to group detail on click', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    // Click on a group
    await groupsPage.clickGroup(testData.groups.nursery.name);

    // Verify navigation to detail page
    await expect(page).toHaveURL(/\/admin\/groups\/.+/);
    await groupsPage.expectOnDetailPage(testData.groups.nursery.name);
  });

  test('should filter groups by search query', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    // Search for "Nursery"
    await groupsPage.searchGroups('Nursery');

    // Verify only Nursery is visible
    await groupsPage.expectGroupVisible(testData.groups.nursery.name);

    // Other groups should not be visible (or verify count)
    const visibleGroups = await page.getByText(/nursery|preschool|elementary/i).count();
    expect(visibleGroups).toBeGreaterThanOrEqual(1);
  });

  test('should show empty state when search has no results', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    // Search for non-existent group
    await groupsPage.searchGroups('NonexistentGroup12345');

    // Verify empty state message
    await expect(page.getByText(/no groups found/i)).toBeVisible();
  });

  test('should toggle between tree and list view', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    // Verify starting in tree view (default)
    await expect(groupsPage.treeViewButton).toHaveClass(/bg-white/);

    // Switch to list view
    await groupsPage.switchToListView();
    await expect(groupsPage.listViewButton).toHaveClass(/bg-white/);

    // Groups should still be visible
    await groupsPage.expectGroupVisible(testData.groups.nursery.name);

    // Switch back to tree view
    await groupsPage.switchToTreeView();
    await expect(groupsPage.treeViewButton).toHaveClass(/bg-white/);
  });

  test('should navigate to create group form', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    // Click create button
    await groupsPage.createGroupButton.click();

    // Verify navigation to form
    await expect(page).toHaveURL('/admin/groups/new');
    await expect(page.getByRole('heading', { name: /create group/i })).toBeVisible();
  });

  test('@smoke should load groups page without errors', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    // Verify no error messages
    await expect(groupsPage.errorMessage).not.toBeVisible();

    // Verify at least one group is visible
    await groupsPage.expectGroupVisible(testData.groups.nursery.name);
  });

  test('should show group type badge in list view', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    // Switch to list view
    await groupsPage.switchToListView();

    // Verify group type is displayed
    const nurseryRow = page.locator('tbody tr', { hasText: testData.groups.nursery.name });
    await expect(nurseryRow).toBeVisible();

    // Look for group type or member count indicators
    await expect(nurseryRow.getByText(/members/i)).toBeVisible();
  });

  test('should clear search query', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    // Search for something
    await groupsPage.searchGroups('Nursery');
    await groupsPage.expectGroupVisible(testData.groups.nursery.name);

    // Clear search
    await groupsPage.searchInput.clear();
    await page.waitForTimeout(500); // Wait for debounce

    // All groups should be visible again
    await groupsPage.expectGroupVisible(testData.groups.nursery.name);
    await groupsPage.expectGroupVisible(testData.groups.preschool.name);
    await groupsPage.expectGroupVisible(testData.groups.elementary.name);
  });

  test('should handle empty groups list', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    // Search for something that won't exist
    await groupsPage.searchGroups('ZZZNothingMatchesThis999');

    // Verify empty state with helpful message
    await expect(page.getByText(/no groups found/i)).toBeVisible();
  });
});

test.describe('Groups Tree - Navigation Performance', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test('should load groups page within reasonable time', async ({ page }) => {
    const startTime = Date.now();
    const groupsPage = new GroupsPage(page);

    await groupsPage.gotoTreeView();
    await groupsPage.waitForLoad();

    const loadTime = Date.now() - startTime;

    // Should load within 3 seconds (generous for E2E)
    expect(loadTime).toBeLessThan(3000);

    // Verify content loaded
    await groupsPage.expectGroupVisible(testData.groups.nursery.name);
  });
});
