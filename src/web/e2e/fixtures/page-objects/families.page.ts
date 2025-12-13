/**
 * Families Page Object Model
 *
 * NOTE: This page object uses aspirational selectors. When the families UI is
 * enhanced, update locators to use data-testid attributes instead of:
 * - CSS classes (.bg-white, .text-red-600)
 * - Positional selectors (.first(), .nth(1))
 * - Parent traversal (..)
 *
 * See Issue #158 for tracking.
 */
import { type Page, type Locator, expect } from '@playwright/test';

/**
 * Page Object for Families pages
 * Covers list view, detail view, form, and member management
 */
export class FamiliesPage {
  readonly page: Page;

  // List view elements
  readonly searchInput: Locator;
  readonly createFamilyButton: Locator;
  readonly campusFilter: Locator;
  readonly familyCards: Locator;
  readonly pageSizeSelect: Locator;
  readonly previousPageButton: Locator;
  readonly nextPageButton: Locator;

  // Detail page elements
  readonly familyName: Locator;
  readonly campusName: Locator;
  readonly editButton: Locator;
  readonly deleteButton: Locator;
  readonly addressSection: Locator;
  readonly membersSection: Locator;
  readonly addMemberButton: Locator;
  readonly memberCards: Locator;

  // Form elements
  readonly nameInput: Locator;
  readonly campusSelect: Locator;
  readonly street1Input: Locator;
  readonly street2Input: Locator;
  readonly cityInput: Locator;
  readonly stateInput: Locator;
  readonly postalCodeInput: Locator;
  readonly submitButton: Locator;
  readonly cancelButton: Locator;
  readonly validationError: Locator;

  // Member management elements
  readonly addMemberModal: Locator;
  readonly memberSearchInput: Locator;
  readonly removeMemberButton: Locator;
  readonly roleSelect: Locator;

  // Common elements
  readonly loadingSpinner: Locator;
  readonly errorMessage: Locator;
  readonly successMessage: Locator;
  readonly emptyState: Locator;
  readonly confirmDialog: Locator;
  readonly confirmButton: Locator;

  constructor(page: Page) {
    this.page = page;

    // List view
    this.searchInput = page.getByPlaceholder(/search.*family/i);
    this.createFamilyButton = page.getByRole('link', { name: /create family/i });
    this.campusFilter = page.getByLabel(/campus/i);
    this.familyCards = page.locator('a[href*="/admin/families/"]');
    this.pageSizeSelect = page.locator('select').filter({ hasText: /25|50|100/ });
    this.previousPageButton = page.getByRole('button', { name: /previous/i });
    this.nextPageButton = page.getByRole('button', { name: /next/i });

    // Detail page
    this.familyName = page.locator('h1');
    this.campusName = page.locator('h1 + p');
    this.editButton = page.getByRole('link', { name: /edit family/i });
    this.deleteButton = page.getByRole('button', { name: /delete/i });
    this.addressSection = page.getByText(/^address$/i).locator('..');
    this.membersSection = page.getByText(/family members/i).locator('..');
    this.addMemberButton = page.getByRole('button', { name: /add member/i });
    this.memberCards = page.locator('[data-testid="family-member-card"]');

    // Form
    this.nameInput = page.getByLabel(/family name/i);
    this.campusSelect = page.getByLabel(/campus/i);
    this.street1Input = page.getByLabel(/street address$/i);
    this.street2Input = page.getByLabel(/street address 2/i);
    this.cityInput = page.getByLabel(/city/i);
    this.stateInput = page.getByLabel(/state/i);
    this.postalCodeInput = page.getByLabel(/postal code/i);
    this.submitButton = page.getByRole('button', { name: /(create family|save changes)/i });
    this.cancelButton = page.getByRole('button', { name: /cancel/i });
    this.validationError = page.locator('.text-red-600');

    // Member management
    this.addMemberModal = page.locator('[role="dialog"]');
    this.memberSearchInput = page.getByPlaceholder(/search.*person/i);
    this.removeMemberButton = page.getByRole('button', { name: /remove/i });
    this.roleSelect = page.getByLabel(/role/i);

    // Common
    this.loadingSpinner = page.locator('.animate-spin');
    this.errorMessage = page.locator('.text-red-600').first();
    this.successMessage = page.getByTestId('success-message');
    this.emptyState = page.getByText(/no families/i);
    this.confirmDialog = page.locator('[role="dialog"]');
    this.confirmButton = page.getByRole('button', { name: /confirm|yes/i });
  }

  async gotoList() {
    await this.page.goto('/admin/families');
    await this.waitForLoad();
  }

  async gotoFamilyDetail(idKey: string) {
    await this.page.goto(`/admin/families/${idKey}`);
    await this.waitForLoad();
  }

  async gotoCreateFamily() {
    await this.page.goto('/admin/families/new');
  }

  async gotoEditFamily(idKey: string) {
    await this.page.goto(`/admin/families/${idKey}/edit`);
  }

  async waitForLoad() {
    try {
      await expect(this.loadingSpinner).not.toBeVisible({ timeout: 10000 });
    } catch {
      // Spinner may not appear for fast loads
    }
  }

  async searchFamilies(query: string) {
    await this.searchInput.fill(query);
    // Wait for search to take effect (debouncing)
    await this.page.waitForLoadState('networkidle');
  }

  async filterByCampus(campusName: string) {
    await this.campusFilter.selectOption({ label: campusName });
    await this.page.waitForLoadState('networkidle');
  }

  async clickFamily(familyName: string) {
    await this.page.getByText(familyName).first().click();
  }

  async createFamily(data: {
    name: string;
    campus?: string;
    address?: {
      street1: string;
      street2?: string;
      city: string;
      state: string;
      postalCode: string;
    };
  }) {
    await this.nameInput.fill(data.name);

    if (data.campus) {
      await this.campusSelect.selectOption({ label: data.campus });
    }

    if (data.address) {
      await this.street1Input.fill(data.address.street1);
      if (data.address.street2) {
        await this.street2Input.fill(data.address.street2);
      }
      await this.cityInput.fill(data.address.city);
      await this.stateInput.fill(data.address.state);
      await this.postalCodeInput.fill(data.address.postalCode);
    }

    await this.submitButton.click();
  }

  async updateFamily(data: {
    name?: string;
    campus?: string;
  }) {
    if (data.name) {
      await this.nameInput.fill(data.name);
    }

    if (data.campus) {
      await this.campusSelect.selectOption({ label: data.campus });
    }

    await this.submitButton.click();
  }

  async deleteFamily() {
    await this.deleteButton.click();
    await expect(this.confirmDialog).toBeVisible();
    await this.confirmButton.click();
  }

  async cancelDelete() {
    await this.deleteButton.click();
    await expect(this.confirmDialog).toBeVisible();
    await this.page.keyboard.press('Escape');
  }

  async addMember(personName: string, role: string) {
    await this.addMemberButton.click();
    await expect(this.addMemberModal).toBeVisible();

    // Search for person
    await this.memberSearchInput.fill(personName);
    await this.page.waitForLoadState('networkidle');

    // Select person from search results
    await this.page.getByText(personName, { exact: true }).click();

    // Select role
    await this.roleSelect.selectOption({ label: role });

    // Confirm
    await this.addMemberModal.getByRole('button', { name: /add/i }).click();
  }

  async removeMember(personName: string) {
    const memberCard = this.page.locator('[data-testid="family-member-card"]', { hasText: personName });
    await memberCard.getByRole('button', { name: /remove/i }).click();
  }

  async expectFamilyVisible(familyName: string) {
    await expect(this.page.getByText(familyName).first()).toBeVisible();
  }

  async expectFamilyNotVisible(familyName: string) {
    await expect(this.page.getByText(familyName).first()).not.toBeVisible();
  }

  async expectOnDetailPage(familyName: string) {
    await expect(this.familyName).toContainText(familyName);
  }

  async expectValidationError(field: string, message?: string) {
    const error = this.page.locator(`#${field}`).locator('..').locator('.text-red-600');
    await expect(error).toBeVisible();
    if (message) {
      await expect(error).toContainText(message);
    }
  }

  async expectMemberCount(count: number) {
    const membersText = await this.membersSection.getByText(/family members/i).textContent();
    expect(membersText).toContain(count.toString());
  }

  async expectAddressVisible(address: string) {
    await expect(this.addressSection.getByText(address)).toBeVisible();
  }
}
