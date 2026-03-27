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

  // Extended locators for complete flow testing
  readonly nameSearchInput: Locator;
  readonly searchByPhoneButton: Locator;
  readonly searchByNameButton: Locator;
  readonly familyCards: Locator;
  readonly checkInCompleteHeading: Locator;
  readonly doneButton: Locator;
  readonly securityCodeDisplay: Locator;

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

    // Extended locators
    this.nameSearchInput = page.getByPlaceholder(/name|first|last/i);
    this.searchByPhoneButton = page.getByRole('button', { name: /search by phone/i });
    this.searchByNameButton = page.getByRole('button', { name: /search by name/i });
    // Family cards shown in the select-family step (multiple families matched)
    this.familyCards = page.getByRole('heading', { level: 3 });
    // Confirmation step elements
    this.checkInCompleteHeading = page.getByText('Check-In Complete!');
    this.doneButton = page.getByRole('button', { name: 'Done' });
    // Security codes rendered as large mono text inside attendance cards
    this.securityCodeDisplay = page.locator('.font-mono');
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

  /**
   * Switch to name search mode and submit a name query.
   */
  async switchToNameSearch() {
    await this.searchByNameButton.click();
  }

  /**
   * Select an activity toggle for a given person row.
   * personIndex is 0-based within the FamilyMemberList.
   * activityIndex is 0-based within that person's opportunity list.
   */
  async selectActivity(personIndex: number, activityIndex: number) {
    // Activities are rendered as toggle buttons inside each member card section
    const targetIndex = personIndex * 10 + activityIndex; // rough offset; real DOM is flat
    const allToggles = await this.page.getByRole('checkbox').all();
    const toggle = allToggles[targetIndex];
    if (toggle) {
      await toggle.click();
    }
  }

  /**
   * Returns the text content of all visible security code elements
   * on the confirmation screen.
   */
  async getSecurityCodes(): Promise<string[]> {
    const codes = await this.securityCodeDisplay.all();
    const texts: string[] = [];
    for (const code of codes) {
      const text = await code.textContent();
      if (text?.trim()) {
        texts.push(text.trim());
      }
    }
    return texts;
  }

  /**
   * Wait for the confirmation step to be visible.
   */
  async expectConfirmation() {
    await expect(this.checkInCompleteHeading).toBeVisible({ timeout: 5000 });
  }

  /**
   * Click Done and verify we return to the search step.
   */
  async clickDoneAndReset() {
    await this.doneButton.click();
    await expect(this.page.getByRole('button', { name: /search by phone/i })).toBeVisible();
  }
}
