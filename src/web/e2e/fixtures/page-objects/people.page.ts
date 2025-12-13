/**
 * People Page Object Model
 *
 * NOTE: This page object uses aspirational selectors. When the people UI is
 * implemented/enhanced, update locators to use data-testid attributes instead of:
 * - CSS classes (.bg-white, .text-red-600)
 * - Positional selectors (.first(), .nth(1))
 * - Parent traversal (..)
 *
 * See Issue #160 for tracking.
 */
import { type Page, type Locator, expect } from '@playwright/test';

/**
 * Page Object for People pages
 * Covers list view, detail view, form, and phone number management
 */
export class PeoplePage {
  readonly page: Page;

  // List view elements
  readonly searchInput: Locator;
  readonly createPersonButton: Locator;
  readonly filterByGender: Locator;
  readonly filterByCampus: Locator;
  readonly personCards: Locator;
  readonly personListItems: Locator;
  readonly pageSizeSelect: Locator;
  readonly previousPageButton: Locator;
  readonly nextPageButton: Locator;

  // Detail page elements
  readonly personName: Locator;
  readonly personEmail: Locator;
  readonly editButton: Locator;
  readonly deleteButton: Locator;
  readonly phoneNumbersSection: Locator;
  readonly familySection: Locator;
  readonly demographicsSection: Locator;

  // Form elements
  readonly firstNameInput: Locator;
  readonly lastNameInput: Locator;
  readonly nickNameInput: Locator;
  readonly middleNameInput: Locator;
  readonly emailInput: Locator;
  readonly genderSelect: Locator;
  readonly birthDateInput: Locator;
  readonly campusSelect: Locator;
  readonly submitButton: Locator;
  readonly cancelButton: Locator;
  readonly validationError: Locator;

  // Phone number elements
  readonly addPhoneButton: Locator;
  readonly phoneNumberInputs: Locator;
  readonly removePhoneButtons: Locator;
  readonly phoneTypeSelects: Locator;
  readonly smsCheckboxes: Locator;

  // Common elements
  readonly loadingSpinner: Locator;
  readonly errorMessage: Locator;
  readonly successMessage: Locator;
  readonly emptyState: Locator;
  readonly deleteConfirmModal: Locator;
  readonly deleteConfirmButton: Locator;
  readonly deleteCancelButton: Locator;

  constructor(page: Page) {
    this.page = page;

    // List view
    this.searchInput = page.getByPlaceholder(/search.*people/i);
    this.createPersonButton = page.getByRole('link', { name: /add person/i });
    this.filterByGender = page.getByLabel(/gender/i);
    this.filterByCampus = page.getByLabel(/campus/i);
    this.personCards = page.locator('a[href*="/admin/people/"]');
    this.personListItems = page.locator('tbody tr');
    this.pageSizeSelect = page.locator('select').filter({ hasText: /25|50|100/ });
    this.previousPageButton = page.getByRole('button', { name: /previous/i });
    this.nextPageButton = page.getByRole('button', { name: /next/i });

    // Detail page
    this.personName = page.locator('h1');
    this.personEmail = page.getByText(/@/).first();
    this.editButton = page.getByRole('link', { name: /edit/i });
    this.deleteButton = page.getByRole('button', { name: /delete/i });
    this.phoneNumbersSection = page.getByText(/phone numbers/i).locator('..');
    this.familySection = page.getByText(/family/i).locator('..');
    this.demographicsSection = page.getByText(/demographics/i).locator('..');

    // Form
    this.firstNameInput = page.getByLabel(/^first name/i);
    this.lastNameInput = page.getByLabel(/^last name/i);
    this.nickNameInput = page.getByLabel(/nick name/i);
    this.middleNameInput = page.getByLabel(/middle name/i);
    this.emailInput = page.getByLabel(/email/i);
    this.genderSelect = page.getByLabel(/gender/i);
    this.birthDateInput = page.getByLabel(/birth date/i);
    this.campusSelect = page.getByLabel(/campus/i);
    this.submitButton = page.getByRole('button', { name: /(create person|update person)/i });
    this.cancelButton = page.getByRole('button', { name: /cancel/i });
    this.validationError = page.locator('.text-red-600');

    // Phone numbers
    this.addPhoneButton = page.getByRole('button', { name: /add phone/i });
    this.phoneNumberInputs = page.locator('input[type="tel"]');
    this.removePhoneButtons = page.getByRole('button', { name: /remove phone number/i });
    this.phoneTypeSelects = page.getByLabel(/phone type/i);
    this.smsCheckboxes = page.locator('input[type="checkbox"]').filter({ has: page.locator('..', { hasText: /sms/i }) });

    // Common
    this.loadingSpinner = page.locator('.animate-spin');
    this.errorMessage = page.locator('.text-red-600').first();
    this.successMessage = page.getByTestId('success-message');
    this.emptyState = page.getByText(/no people/i);
    this.deleteConfirmModal = page.locator('.fixed.inset-0');
    this.deleteConfirmButton = page.getByRole('button', { name: /^delete$/i });
    this.deleteCancelButton = this.deleteConfirmModal.getByRole('button', { name: /cancel/i });
  }

  async gotoList() {
    await this.page.goto('/admin/people');
    await this.waitForLoad();
  }

  async gotoPersonDetail(idKey: string) {
    await this.page.goto(`/admin/people/${idKey}`);
    await this.waitForLoad();
  }

  async gotoCreatePerson() {
    await this.page.goto('/admin/people/new');
  }

  async gotoEditPerson(idKey: string) {
    await this.page.goto(`/admin/people/${idKey}/edit`);
  }

  async waitForLoad() {
    try {
      await expect(this.loadingSpinner).not.toBeVisible({ timeout: 10000 });
    } catch {
      // Spinner may not appear for fast loads
    }
  }

  async searchPeople(query: string) {
    await this.searchInput.fill(query);
    // Wait for search to take effect (debouncing)
    await this.page.waitForLoadState('networkidle');
  }

  async filterByGenderValue(gender: string) {
    await this.filterByGender.selectOption({ label: gender });
    await this.page.waitForLoadState('networkidle');
  }

  async filterByCampusValue(campusName: string) {
    await this.filterByCampus.selectOption({ label: campusName });
    await this.page.waitForLoadState('networkidle');
  }

  async clickPerson(personName: string) {
    await this.page.getByText(personName).first().click();
  }

  async createPerson(data: {
    firstName: string;
    lastName: string;
    nickName?: string;
    middleName?: string;
    email?: string;
    gender?: string;
    birthDate?: string;
    campus?: string;
    phoneNumbers?: Array<{
      number: string;
      type?: string;
      smsEnabled?: boolean;
    }>;
  }) {
    await this.firstNameInput.fill(data.firstName);
    await this.lastNameInput.fill(data.lastName);

    if (data.nickName) {
      await this.nickNameInput.fill(data.nickName);
    }

    if (data.middleName) {
      await this.middleNameInput.fill(data.middleName);
    }

    if (data.email) {
      await this.emailInput.fill(data.email);
    }

    if (data.gender) {
      await this.genderSelect.selectOption({ label: data.gender });
    }

    if (data.birthDate) {
      await this.birthDateInput.fill(data.birthDate);
    }

    if (data.campus) {
      await this.campusSelect.selectOption({ label: data.campus });
    }

    if (data.phoneNumbers && data.phoneNumbers.length > 0) {
      for (const phone of data.phoneNumbers) {
        await this.addPhoneButton.click();
        const phoneInputs = await this.phoneNumberInputs.all();
        const lastInput = phoneInputs[phoneInputs.length - 1];
        await lastInput.fill(phone.number);

        if (phone.smsEnabled === false) {
          const smsCheckboxes = await this.smsCheckboxes.all();
          const lastCheckbox = smsCheckboxes[smsCheckboxes.length - 1];
          await lastCheckbox.uncheck();
        }
      }
    }

    await this.submitButton.click();
  }

  async updatePerson(data: {
    firstName?: string;
    lastName?: string;
    nickName?: string;
    middleName?: string;
    email?: string;
    gender?: string;
    birthDate?: string;
  }) {
    if (data.firstName) {
      await this.firstNameInput.fill(data.firstName);
    }

    if (data.lastName) {
      await this.lastNameInput.fill(data.lastName);
    }

    if (data.nickName !== undefined) {
      await this.nickNameInput.fill(data.nickName);
    }

    if (data.middleName !== undefined) {
      await this.middleNameInput.fill(data.middleName);
    }

    if (data.email !== undefined) {
      await this.emailInput.fill(data.email);
    }

    if (data.gender) {
      await this.genderSelect.selectOption({ label: data.gender });
    }

    if (data.birthDate !== undefined) {
      await this.birthDateInput.fill(data.birthDate);
    }

    await this.submitButton.click();
  }

  async addPhoneNumber(number: string, smsEnabled = true) {
    await this.addPhoneButton.click();
    const phoneInputs = await this.phoneNumberInputs.all();
    const lastInput = phoneInputs[phoneInputs.length - 1];
    await lastInput.fill(number);

    if (!smsEnabled) {
      const smsCheckboxes = await this.smsCheckboxes.all();
      const lastCheckbox = smsCheckboxes[smsCheckboxes.length - 1];
      await lastCheckbox.uncheck();
    }
  }

  async removePhoneNumber(index: number) {
    const removeButtons = await this.removePhoneButtons.all();
    await removeButtons[index].click();
  }

  async getPhoneNumberCount(): Promise<number> {
    return await this.phoneNumberInputs.count();
  }

  async deletePerson() {
    await this.deleteButton.click();
    await expect(this.deleteConfirmModal).toBeVisible();
    await this.deleteConfirmButton.click();
  }

  async cancelDelete() {
    await this.deleteButton.click();
    await expect(this.deleteConfirmModal).toBeVisible();
    await this.deleteCancelButton.click();
  }

  async expectPersonVisible(personName: string) {
    await expect(this.page.getByText(personName).first()).toBeVisible();
  }

  async expectPersonNotVisible(personName: string) {
    await expect(this.page.getByText(personName).first()).not.toBeVisible();
  }

  async expectOnDetailPage(personName: string) {
    await expect(this.personName).toContainText(personName);
  }

  async expectValidationError(field: string, message?: string) {
    const error = this.page.locator(`#${field}`).locator('..').locator('.text-red-600');
    await expect(error).toBeVisible();
    if (message) {
      await expect(error).toContainText(message);
    }
  }

  async expectPhoneNumberVisible(phoneNumber: string) {
    await expect(this.page.getByText(phoneNumber)).toBeVisible();
  }

  async expectEmailVisible(email: string) {
    await expect(this.page.getByText(email)).toBeVisible();
  }
}
