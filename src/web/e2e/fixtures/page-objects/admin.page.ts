import { type Page, type Locator, expect } from '@playwright/test';

/**
 * Page Object for Admin Dashboard
 * Covers navigation and common admin operations
 */
export class AdminPage {
  readonly page: Page;
  readonly nav: {
    dashboard: Locator;
    people: Locator;
    families: Locator;
    groups: Locator;
    checkin: Locator;
    settings: Locator;
  };
  readonly searchInput: Locator;
  readonly addButton: Locator;
  readonly tableRows: Locator;
  readonly loadingSpinner: Locator;

  constructor(page: Page) {
    this.page = page;
    this.nav = {
      dashboard: page.getByRole('link', { name: 'Dashboard' }),
      people: page.getByRole('link', { name: 'People' }),
      families: page.getByRole('link', { name: 'Families' }),
      groups: page.getByRole('link', { name: 'Groups' }),
      checkin: page.getByRole('link', { name: 'Check-in' }),
      settings: page.getByRole('link', { name: 'Settings' }),
    };
    this.searchInput = page.getByPlaceholder(/search/i);
    this.addButton = page.getByRole('button', { name: /add|create|new/i });
    this.tableRows = page.locator('tbody tr');
    this.loadingSpinner = page.getByTestId('loading-spinner');
  }

  async goto() {
    await this.page.goto('/dashboard');
  }

  async navigateTo(section: keyof typeof this.nav) {
    await this.nav[section].click();
  }

  async search(query: string) {
    await this.searchInput.fill(query);
    await this.searchInput.press('Enter');
  }

  async waitForLoad() {
    await expect(this.loadingSpinner).not.toBeVisible({ timeout: 10000 });
  }

  async expectRowCount(count: number) {
    await expect(this.tableRows).toHaveCount(count);
  }

  async expectRowContaining(text: string) {
    await expect(this.page.locator('tbody tr', { hasText: text })).toBeVisible();
  }
}
