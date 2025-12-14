import { test, expect } from '@playwright/test';
import { LoginPage } from '../../fixtures/page-objects/login.page';

test.describe('Login Flow', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
  });

  test('should display login form on load', async ({ page }) => {
    const loginPage = new LoginPage(page);

    await expect(loginPage.emailInput).toBeVisible();
    await expect(loginPage.passwordInput).toBeVisible();
    await expect(loginPage.signInButton).toBeVisible();
  });

  test('should show error for invalid credentials', async ({ page }) => {
    const loginPage = new LoginPage(page);

    await loginPage.login('invalid', 'invalid');
    await loginPage.expectErrorMessage('Invalid email or password');
  });

  test('should redirect to dashboard on successful login', async ({ page }) => {
    const loginPage = new LoginPage(page);

    await loginPage.login('john.smith@example.com', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test('should persist session after page reload', async ({ page }) => {
    const loginPage = new LoginPage(page);

    await loginPage.login('john.smith@example.com', 'admin123');
    await loginPage.expectLoggedIn();

    // Reload and verify still logged in
    await page.reload();
    await expect(page).toHaveURL('/dashboard');
  });

  test('should redirect unauthenticated users to login', async ({ page }) => {
    // Try to access protected route without login
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/login/);
  });

  test('@smoke should complete login flow', async ({ page }) => {
    const loginPage = new LoginPage(page);

    await loginPage.login('john.smith@example.com', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test('should submit form on Enter key press', async ({ page }) => {
    const loginPage = new LoginPage(page);

    await loginPage.emailInput.fill('john.smith@example.com');
    await loginPage.passwordInput.fill('admin123');
    await loginPage.passwordInput.press('Enter');

    await loginPage.expectLoggedIn();
  });
});
