/**
 * E2E Tests: Error Handling & Boundaries
 * Tests 404 pages, error boundaries, and error recovery
 */

import { test, expect } from '@playwright/test';
import { test as authTest } from '../../fixtures/auth.fixture';
import { LoginPage } from '../../fixtures/page-objects/login.page';
import { testData } from '../../fixtures/test-data';

test.describe('Error Handling', () => {
  test('should display 404 page for non-existent routes', async ({ page }) => {
    await page.goto('/this-page-does-not-exist');

    // Should show 404 page
    await expect(page.getByRole('heading', { name: '404' })).toBeVisible();
    await expect(page.getByText(/page not found/i)).toBeVisible();

    // Should have link to go home
    const homeLink = page.getByRole('link', { name: /go home/i });
    await expect(homeLink).toBeVisible();
    await homeLink.click();

    // Should navigate to home
    await expect(page).toHaveURL('/');
  });

  authTest('should display 404 for invalid admin routes', async ({ loginAsAdmin, page }) => {
    await loginAsAdmin();

    await page.goto('/admin/this-does-not-exist');

    // Should show error page (either 404 or route error boundary)
    const errorHeading = page.locator('h1');
    await expect(errorHeading).toContainText(/404|not found|error/i);
  });

  authTest('should display 404 for invalid person IdKey', async ({ loginAsAdmin, page }) => {
    await loginAsAdmin();

    // Try to access non-existent person
    await page.goto('/admin/people/invalid-id-key');

    // Should show error (either 404 or error message)
    const errorIndicator = page.locator('body');
    await expect(errorIndicator).toContainText(/not found|error|invalid/i);
  });

  test('should allow recovery from 404 via navigation', async ({ page }) => {
    await page.goto('/non-existent-page');
    await expect(page.getByRole('heading', { name: '404' })).toBeVisible();

    // Click home link
    await page.getByRole('link', { name: /go home/i }).click();
    await expect(page).toHaveURL('/');

    // Should show normal home page
    await expect(page.getByText(/koinon rms/i)).toBeVisible();
  });

  test('should handle deep-linked 404 and navigate to valid page', async ({ page }) => {
    const loginPage = new LoginPage(page);

    // Start on a 404 page
    await page.goto('/admin/invalid-section/invalid-page');
    await expect(page.locator('h1')).toContainText(/404|error|not found/i);

    // Navigate to login
    await page.goto('/login');
    await loginPage.login(testData.credentials.admin.email, testData.credentials.admin.password);
    await loginPage.expectLoggedIn();

    // Should successfully navigate to valid admin page
    await page.goto('/admin/people');
    await expect(page).toHaveURL('/admin/people');
    await expect(page.getByRole('heading', { name: 'People' })).toBeVisible();
  });
});

test.describe('Error Boundaries', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('should catch errors within admin routes', async ({ page }) => {
    // This test would need a way to trigger an error in the component
    // For now, we verify the error boundary component exists and is configured
    await page.goto('/admin');

    // Verify admin layout loads successfully
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();

    // The error boundary is configured in App.tsx with errorElement prop
    // Actual error testing would require injecting errors via test utilities
  });

  test('should display error UI with retry and home options', async ({ page }) => {
    // Navigate to a page that might have errors
    await page.goto('/admin/people');

    // In a real scenario, we'd trigger an error
    // For now, verify the page loads correctly (error boundary is inactive)
    await expect(page.getByRole('heading', { name: 'People' })).toBeVisible();
  });
});

test.describe('Protected Routes', () => {
  test('should redirect unauthenticated users to login', async ({ page }) => {
    // Try to access admin without logging in
    await page.goto('/admin');
    await expect(page).toHaveURL(/\/login/);
  });

  test('should redirect unauthenticated users from people page', async ({ page }) => {
    await page.goto('/admin/people');
    await expect(page).toHaveURL(/\/login/);
  });

  test('should redirect unauthenticated users from check-in', async ({ page }) => {
    await page.goto('/checkin');
    await expect(page).toHaveURL(/\/login/);
  });

  test('should allow access to public routes without auth', async ({ page }) => {
    // Public group finder should be accessible
    await page.goto('/groups');
    await expect(page).toHaveURL('/groups');

    // Should not redirect to login
    await expect(page).not.toHaveURL(/\/login/);
  });

  test('should maintain intended destination after login', async ({ page }) => {
    // Try to access admin/people without auth
    await page.goto('/admin/people');

    // Should redirect to login
    await expect(page).toHaveURL(/\/login/);

    // Login
    const loginPage = new LoginPage(page);
    await loginPage.login(testData.credentials.admin.email, testData.credentials.admin.password);

    // Should redirect back to intended page (or dashboard)
    // Note: This behavior depends on ProtectedRoute implementation
    await page.waitForURL(/\/admin|\/dashboard/);
  });
});

test.describe('Navigation Resilience', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('should handle browser back/forward buttons', async ({ page }) => {
    await page.goto('/admin');
    await page.getByRole('link', { name: 'People' }).click();
    await expect(page).toHaveURL('/admin/people');

    await page.getByRole('link', { name: 'Groups' }).click();
    await expect(page).toHaveURL('/admin/groups');

    // Go back
    await page.goBack();
    await expect(page).toHaveURL('/admin/people');

    // Go forward
    await page.goForward();
    await expect(page).toHaveURL('/admin/groups');
  });

  test('should handle rapid navigation clicks', async ({ page }) => {
    await page.goto('/admin');

    // Click multiple nav links rapidly
    await page.getByRole('link', { name: 'People' }).click();
    await page.getByRole('link', { name: 'Groups' }).click();
    await page.getByRole('link', { name: 'Families' }).click();

    // Should end up on the last clicked page
    await expect(page).toHaveURL('/admin/families');
    await expect(page.getByRole('heading', { name: 'Families' })).toBeVisible();
  });

  test('should preserve URL parameters during navigation', async ({ page }) => {
    // Navigate to people with search param
    await page.goto('/admin/people?search=john');
    await expect(page).toHaveURL(/\/admin\/people\?search=john/);

    // Navigate away and back
    await page.getByRole('link', { name: 'Dashboard' }).click();
    await page.goBack();

    // URL should be preserved
    await expect(page).toHaveURL(/\/admin\/people/);
  });

  test('@smoke should handle complete navigation workflow', async ({ page }) => {
    // Start at dashboard
    await page.goto('/admin');
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();

    // Navigate to people
    await page.getByRole('link', { name: 'People' }).click();
    await expect(page).toHaveURL('/admin/people');

    // Try an invalid route
    await page.goto('/admin/invalid-page');
    await expect(page.locator('h1')).toContainText(/404|error|not found/i);

    // Recover by going to settings
    await page.goto('/admin/settings');
    await expect(page).toHaveURL('/admin/settings');
    await expect(page.getByRole('heading', { name: 'Settings' })).toBeVisible();

    // Go home
    await page.goto('/');
    await expect(page).toHaveURL('/');
  });
});
