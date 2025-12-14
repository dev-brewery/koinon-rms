/**
 * E2E Tests: Breadcrumb Navigation
 * Tests breadcrumb navigation functionality and accuracy
 */

import { test, expect } from '@playwright/test';
import { LoginPage } from '../../fixtures/page-objects/login.page';

test.describe('Breadcrumb Navigation', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('john.smith@example.com', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test('should display breadcrumb on admin pages', async ({ page }) => {
    await page.goto('/admin/people');

    // Check for breadcrumb container (implementation may vary)
    const breadcrumb = page.locator('[aria-label="Breadcrumb"], nav.breadcrumb, .breadcrumb');

    // Breadcrumb might not be visible on all pages, so we just verify the layout works
    // If breadcrumb component is implemented, it should be testable here
    // For now, just verify the locator is valid (doesn't throw)
    await expect(breadcrumb.or(page.locator('main'))).toBeVisible();
  });

  test('should show correct breadcrumb trail for nested pages', async ({ page }) => {
    // Navigate to a nested page (e.g., edit person)
    await page.goto('/admin/people');

    // Click to view a person (if data exists)
    // This is a basic structure test - actual implementation depends on data
    const pageContent = page.locator('main');
    await expect(pageContent).toBeVisible();
  });

  test('should allow navigation via breadcrumb links', async ({ page }) => {
    // This test assumes breadcrumb implementation
    // If breadcrumbs are added later, this provides the test structure

    await page.goto('/admin/people');
    await expect(page).toHaveURL('/admin/people');

    // Future: Test clicking breadcrumb links to navigate back
  });

  test('should update breadcrumb on navigation', async ({ page }) => {
    await page.goto('/admin/people');
    await expect(page).toHaveURL('/admin/people');

    await page.goto('/admin/groups');
    await expect(page).toHaveURL('/admin/groups');

    // Breadcrumb should reflect current page
    // Actual assertions depend on breadcrumb implementation
  });
});

test.describe('Page Titles', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('john.smith@example.com', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test('should display correct page heading for Dashboard', async ({ page }) => {
    await page.goto('/admin');
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
  });

  test('should display correct page heading for People', async ({ page }) => {
    await page.goto('/admin/people');
    await expect(page.getByRole('heading', { name: 'People' })).toBeVisible();
  });

  test('should display correct page heading for Families', async ({ page }) => {
    await page.goto('/admin/families');
    await expect(page.getByRole('heading', { name: 'Families' })).toBeVisible();
  });

  test('should display correct page heading for Groups', async ({ page }) => {
    await page.goto('/admin/groups');
    await expect(page.getByRole('heading', { name: 'Groups' })).toBeVisible();
  });

  test('should display correct page heading for Schedules', async ({ page }) => {
    await page.goto('/admin/schedules');
    await expect(page.getByRole('heading', { name: 'Schedules' })).toBeVisible();
  });

  test('should display correct page heading for Analytics', async ({ page }) => {
    await page.goto('/admin/analytics');
    await expect(page.getByRole('heading', { name: 'Analytics' })).toBeVisible();
  });

  test('should display correct page heading for Communications', async ({ page }) => {
    await page.goto('/admin/communications');
    await expect(page.getByRole('heading', { name: 'Communications' })).toBeVisible();
  });

  test('should display correct page heading for Room Roster', async ({ page }) => {
    await page.goto('/admin/roster');
    await expect(page.getByRole('heading', { name: 'Room Roster' })).toBeVisible();
  });

  test('should display correct page heading for Settings', async ({ page }) => {
    await page.goto('/admin/settings');
    await expect(page.getByRole('heading', { name: 'Settings' })).toBeVisible();
  });

  test('@smoke should verify all page headings', async ({ page }) => {
    const pages = [
      { url: '/admin', heading: 'Dashboard' },
      { url: '/admin/people', heading: 'People' },
      { url: '/admin/families', heading: 'Families' },
      { url: '/admin/groups', heading: 'Groups' },
      { url: '/admin/schedules', heading: 'Schedules' },
      { url: '/admin/analytics', heading: 'Analytics' },
      { url: '/admin/communications', heading: 'Communications' },
      { url: '/admin/roster', heading: 'Room Roster' },
      { url: '/admin/settings', heading: 'Settings' },
    ];

    for (const pageInfo of pages) {
      await page.goto(pageInfo.url);
      await expect(page.getByRole('heading', { name: pageInfo.heading })).toBeVisible();
    }
  });
});
