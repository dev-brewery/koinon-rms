/**
 * Roster Page Object Model
 *
 * NOTE: This page object uses data-testid attributes for reliable element selection.
 * Uses semantic selectors and role-based queries where possible.
 */
import { type Page, type Locator, expect } from '@playwright/test';

/**
 * Page Object for Room Roster page
 * Covers navigation, location selection, and roster display
 */
export class RosterPage {
  readonly page: Page;

  // Page elements
  readonly pageHeading: Locator;
  readonly locationPicker: Locator;
  readonly autoRefreshCheckbox: Locator;
  readonly refreshButton: Locator;
  readonly rosterList: Locator;
  readonly emptyState: Locator;
  readonly loadingSpinner: Locator;
  readonly errorMessage: Locator;

  constructor(page: Page) {
    this.page = page;

    // Page elements - using data-testid for reliability
    this.pageHeading = page.getByRole('heading', { name: /room roster/i });
    this.locationPicker = page.getByTestId('location-picker');
    this.autoRefreshCheckbox = page.getByTestId('auto-refresh-checkbox');
    this.refreshButton = page.getByRole('button', { name: /refresh/i });
    this.rosterList = page.getByTestId('roster-list');
    this.emptyState = page.getByText(/select a room to view the roster/i);
    this.loadingSpinner = page.getByTestId('roster-loading');
    this.errorMessage = page.getByTestId('roster-error');
  }

  async gotoRoster() {
    await this.page.goto('/admin/roster');
    await this.waitForLoad();
  }

  async waitForLoad() {
    try {
      // Wait for loading spinner to disappear (10s timeout for slow CI environments)
      await expect(this.loadingSpinner).not.toBeVisible({ timeout: 10000 });
    } catch {
      // Loading spinner may not appear for fast loads or cached data
    }
  }

  async expectOnRosterPage() {
    await expect(this.pageHeading).toBeVisible();
    await expect(this.page).toHaveURL('/admin/roster');
  }

  async expectEmptyState() {
    await expect(this.emptyState).toBeVisible();
  }

  async expectLocationPickerVisible() {
    await expect(this.locationPicker).toBeVisible();
  }
}
