import { test as base, expect } from '@playwright/test';

/**
 * Authentication fixture for E2E tests
 * Provides logged-in state caching for faster test execution
 */

export interface AuthFixture {
  loginAsAdmin: () => Promise<void>;
  loginAsKiosk: () => Promise<void>;
}

export const test = base.extend<AuthFixture>({
  loginAsAdmin: async ({ page }, use) => {
    const login = async () => {
      await page.goto('/login');
      await page.getByLabel('Email').fill('john.smith@example.com');
      await page.getByLabel('Password').fill('admin123');
      await page.getByRole('button', { name: 'Sign In' }).click();
      await expect(page).toHaveURL('/dashboard');
    };
    await use(login);
  },

  loginAsKiosk: async ({ page }, use) => {
    const login = async () => {
      await page.goto('/kiosk/setup');
      await page.getByLabel('Device Code').fill('KIOSK-001');
      await page.getByRole('button', { name: 'Activate' }).click();
      await expect(page).toHaveURL('/checkin');
    };
    await use(login);
  },
});

export { expect } from '@playwright/test';
