import { type Page, type Locator, expect } from '@playwright/test';

/**
 * Page Object for Check-in Kiosk page
 * Performance-critical: <200ms online, <50ms offline target
 */
export class CheckinPage {
  readonly page: Page;
  readonly phoneInput: Locator;
  readonly searchButton: Locator;
  readonly familyMemberList: Locator;
  readonly familyMemberCards: Locator;
  readonly confirmButton: Locator;
  readonly successMessage: Locator;
  readonly idleWarningModal: Locator;
  readonly continueButton: Locator;

  constructor(page: Page) {
    this.page = page;
    this.phoneInput = page.getByTestId('phone-input');
    this.searchButton = page.getByRole('button', { name: /search|find/i });
    this.familyMemberList = page.getByTestId('family-member-list');
    this.familyMemberCards = page.getByTestId('family-member-card');
    this.confirmButton = page.getByRole('button', { name: /check in|confirm/i });
    this.successMessage = page.getByTestId('success-message');
    this.idleWarningModal = page.getByRole('alertdialog');
    this.continueButton = page.getByRole('button', { name: /continue|stay/i });
  }

  async goto() {
    await this.page.goto('/checkin');
  }

  async enterPhone(phone: string) {
    await this.phoneInput.fill(phone);
  }

  async submitPhone() {
    await this.searchButton.click();
  }

  async searchByPhone(phone: string) {
    await this.enterPhone(phone);
    await this.submitPhone();
    await expect(this.familyMemberList).toBeVisible();
  }

  async selectMember(index: number) {
    const members = await this.familyMemberCards.all();
    if (members[index]) {
      await members[index].click();
    }
  }

  async selectMemberByName(name: string) {
    await this.page.getByText(name).click();
  }

  async confirmCheckin() {
    await this.confirmButton.click();
  }

  async expectSuccess() {
    await expect(this.successMessage).toBeVisible();
  }

  async dismissIdleWarning() {
    await expect(this.idleWarningModal).toBeVisible();
    await this.continueButton.click();
    await expect(this.idleWarningModal).not.toBeVisible();
  }
}
