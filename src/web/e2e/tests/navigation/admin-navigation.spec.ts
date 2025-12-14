/**
 * E2E Tests: Admin Navigation
 * Tests all admin navigation links and route accessibility
 */

import { test, expect } from '@playwright/test';
import { LoginPage } from '../../fixtures/page-objects/login.page';

test.describe('Admin Navigation', () => {
  test.beforeEach(async ({ page }) => {
    // Login before each test
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('john.smith@example.com', 'admin123');
    await loginPage.expectLoggedIn();

    // Navigate to admin area
    await page.goto('/admin');
    await expect(page).toHaveURL('/admin');
  });

  test('should display all main navigation sections', async ({ page }) => {
    // Check for navigation sections
    await expect(page.getByText('Overview')).toBeVisible();
    await expect(page.getByText('Management')).toBeVisible();
    await expect(page.getByText('Operations')).toBeVisible();
    await expect(page.getByText('System')).toBeVisible();
  });

  test('should navigate to Dashboard', async ({ page }) => {
    await page.getByRole('link', { name: 'Dashboard' }).click();
    await expect(page).toHaveURL('/admin');
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
  });

  test('should navigate to Analytics', async ({ page }) => {
    await page.getByRole('link', { name: 'Analytics' }).click();
    await expect(page).toHaveURL('/admin/analytics');
    await expect(page.getByRole('heading', { name: 'Analytics' })).toBeVisible();
  });

  test('should navigate to People', async ({ page }) => {
    await page.getByRole('link', { name: 'People' }).click();
    await expect(page).toHaveURL('/admin/people');
    await expect(page.getByRole('heading', { name: 'People' })).toBeVisible();
  });

  test('should navigate to Families', async ({ page }) => {
    await page.getByRole('link', { name: 'Families' }).click();
    await expect(page).toHaveURL('/admin/families');
    await expect(page.getByRole('heading', { name: 'Families' })).toBeVisible();
  });

  test('should navigate to Groups', async ({ page }) => {
    await page.getByRole('link', { name: 'Groups' }).click();
    await expect(page).toHaveURL('/admin/groups');
    await expect(page.getByRole('heading', { name: 'Groups' })).toBeVisible();
  });

  test('should navigate to Schedules', async ({ page }) => {
    await page.getByRole('link', { name: 'Schedules' }).click();
    await expect(page).toHaveURL('/admin/schedules');
    await expect(page.getByRole('heading', { name: 'Schedules' })).toBeVisible();
  });

  test('should navigate to Communications', async ({ page }) => {
    await page.getByRole('link', { name: 'Communications' }).click();
    await expect(page).toHaveURL('/admin/communications');
    await expect(page.getByRole('heading', { name: 'Communications' })).toBeVisible();
  });

  test('should navigate to Room Roster', async ({ page }) => {
    await page.getByRole('link', { name: 'Room Roster' }).click();
    await expect(page).toHaveURL('/admin/roster');
    await expect(page.getByRole('heading', { name: 'Room Roster' })).toBeVisible();
  });

  test('should navigate to Settings', async ({ page }) => {
    await page.getByRole('link', { name: 'Settings' }).click();
    await expect(page).toHaveURL('/admin/settings');
    await expect(page.getByRole('heading', { name: 'Settings' })).toBeVisible();
  });

  test('should navigate to Check-in Mode from sidebar', async ({ page }) => {
    await page.getByRole('link', { name: 'Check-In Mode' }).click();
    await expect(page).toHaveURL('/checkin');
    // Check-in page should load
    await expect(page.locator('body')).toBeVisible();
  });

  test('should highlight active navigation item', async ({ page }) => {
    // Navigate to People
    await page.getByRole('link', { name: 'People' }).click();

    // Check that People link has active styling
    const peopleLink = page.getByRole('link', { name: 'People' });
    await expect(peopleLink).toHaveClass(/bg-primary-50/);
    await expect(peopleLink).toHaveClass(/text-primary-700/);

    // Other links should not have active styling
    const groupsLink = page.getByRole('link', { name: 'Groups' });
    await expect(groupsLink).not.toHaveClass(/bg-primary-50/);
  });

  test('should maintain navigation state after page reload', async ({ page }) => {
    // Navigate to Analytics
    await page.getByRole('link', { name: 'Analytics' }).click();
    await expect(page).toHaveURL('/admin/analytics');

    // Reload page
    await page.reload();

    // Should still be on Analytics and link should be highlighted
    await expect(page).toHaveURL('/admin/analytics');
    const analyticsLink = page.getByRole('link', { name: 'Analytics' });
    await expect(analyticsLink).toHaveClass(/bg-primary-50/);
  });

  test('@smoke should navigate through all main pages', async ({ page }) => {
    const pages = [
      { name: 'Dashboard', url: '/admin', heading: 'Dashboard' },
      { name: 'Analytics', url: '/admin/analytics', heading: 'Analytics' },
      { name: 'People', url: '/admin/people', heading: 'People' },
      { name: 'Families', url: '/admin/families', heading: 'Families' },
      { name: 'Groups', url: '/admin/groups', heading: 'Groups' },
      { name: 'Schedules', url: '/admin/schedules', heading: 'Schedules' },
      { name: 'Communications', url: '/admin/communications', heading: 'Communications' },
      { name: 'Room Roster', url: '/admin/roster', heading: 'Room Roster' },
      { name: 'Settings', url: '/admin/settings', heading: 'Settings' },
    ];

    for (const pageInfo of pages) {
      await page.getByRole('link', { name: pageInfo.name }).click();
      await expect(page).toHaveURL(pageInfo.url);
      await expect(page.getByRole('heading', { name: pageInfo.heading })).toBeVisible();
    }
  });
});

test.describe('Admin Navigation - Mobile', () => {
  test.use({ viewport: { width: 375, height: 667 } });

  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('john.smith@example.com', 'admin123');
    await loginPage.expectLoggedIn();
    await page.goto('/admin');
  });

  test('should toggle sidebar on mobile', async ({ page }) => {
    // Sidebar should be hidden initially on mobile
    const sidebar = page.locator('aside');
    await expect(sidebar).toHaveClass(/translate-x-0|lg:translate-x-0/);

    // Click menu button to open
    const menuButton = page.getByRole('button', { name: /menu/i });
    await menuButton.click();

    // Sidebar should be visible
    await expect(sidebar).toHaveClass(/translate-x-0/);

    // Click overlay to close
    const overlay = page.locator('.bg-black.bg-opacity-50');
    await overlay.click();

    // Sidebar should be hidden again
    await expect(sidebar).toHaveClass(/-translate-x-full/);
  });

  test('should close sidebar after navigation on mobile', async ({ page }) => {
    // Open sidebar
    const menuButton = page.getByRole('button', { name: /menu/i });
    await menuButton.click();

    // Click a navigation link
    await page.getByRole('link', { name: 'People' }).click();

    // Sidebar should close automatically
    const sidebar = page.locator('aside');
    await expect(sidebar).toHaveClass(/-translate-x-full/);

    // Should navigate to the correct page
    await expect(page).toHaveURL('/admin/people');
  });
});
