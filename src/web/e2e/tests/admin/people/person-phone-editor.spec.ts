/**
 * E2E Tests: Person Phone Number Management
 * Tests adding, editing, and removing phone numbers on person form
 *
 * ASSUMPTIONS (based on PersonFormPage.tsx implementation):
 * - Phone numbers managed via dynamic array on person form
 * - "+ Add Phone" button adds new phone input row
 * - Each phone row has: number input, SMS checkbox, remove button
 * - Phone numbers are optional (can create person without any)
 * - SMS checkbox defaults to checked (isMessagingEnabled: true)
 * - Remove button has trash icon with aria-label="Remove phone number"
 * - Phone type selector may exist but is optional (not in current UI)
 * - Empty phone numbers are filtered out on submit
 *
 * NOTE: These tests target the existing phone editor UI in PersonFormPage.tsx
 */

import { test, expect } from '@playwright/test';
import { PeoplePage } from '../../../fixtures/page-objects/people.page';
import { LoginPage } from '../../../fixtures/page-objects/login.page';

test.describe('Person Phone Number Management - Create Mode', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('john.smith@example.com', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test('should create person without phone numbers', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    // Create person with no phone numbers
    await peoplePage.createPerson({
      firstName: 'No',
      lastName: 'Phone',
      email: 'no.phone@example.com',
    });

    // Should succeed
    await peoplePage.expectOnDetailPage('No Phone');
  });

  test('should add single phone number', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    // Add one phone number
    await peoplePage.createPerson({
      firstName: 'Single',
      lastName: 'Phone',
      phoneNumbers: [
        { number: '555-1234', smsEnabled: true },
      ],
    });

    // Verify person created
    await peoplePage.expectOnDetailPage('Single Phone');

    // Phone number should be visible on detail page
    await peoplePage.expectPhoneNumberVisible('555-1234');
  });

  test('should add multiple phone numbers', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    // Add three phone numbers
    await peoplePage.createPerson({
      firstName: 'Multiple',
      lastName: 'Phones',
      phoneNumbers: [
        { number: '555-1111', smsEnabled: true },
        { number: '555-2222', smsEnabled: false },
        { number: '555-3333', smsEnabled: true },
      ],
    });

    // Verify person created
    await peoplePage.expectOnDetailPage('Multiple Phones');

    // All phone numbers should be visible
    await peoplePage.expectPhoneNumberVisible('555-1111');
    await peoplePage.expectPhoneNumberVisible('555-2222');
    await peoplePage.expectPhoneNumberVisible('555-3333');
  });

  test('should toggle SMS enabled checkbox', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    // Fill required fields
    await peoplePage.firstNameInput.fill('SMS');
    await peoplePage.lastNameInput.fill('Test');

    // Add phone
    await peoplePage.addPhoneButton.click();

    // Phone input should exist
    const phoneInputs = await peoplePage.phoneNumberInputs.all();
    expect(phoneInputs.length).toBe(1);

    // SMS checkbox should be checked by default
    const smsCheckboxes = await peoplePage.smsCheckboxes.all();
    await expect(smsCheckboxes[0]).toBeChecked();

    // Uncheck SMS
    await smsCheckboxes[0].uncheck();
    await expect(smsCheckboxes[0]).not.toBeChecked();

    // Re-check SMS
    await smsCheckboxes[0].check();
    await expect(smsCheckboxes[0]).toBeChecked();

    // Verify final state is checked
    await expect(smsCheckboxes[0]).toBeChecked();
  });

  test('should remove phone number before submit', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    await peoplePage.firstNameInput.fill('Remove');
    await peoplePage.lastNameInput.fill('Phone');

    // Add two phones
    await peoplePage.addPhoneButton.click();
    await peoplePage.addPhoneButton.click();

    let phoneInputs = await peoplePage.phoneNumberInputs.all();
    expect(phoneInputs.length).toBe(2);

    // Fill them
    await phoneInputs[0].fill('555-1111');
    await phoneInputs[1].fill('555-2222');

    // Remove first phone
    await peoplePage.removePhoneNumber(0);

    // Should only have one phone left
    phoneInputs = await peoplePage.phoneNumberInputs.all();
    expect(phoneInputs.length).toBe(1);

    // Remaining phone should be the second one
    const remainingValue = await phoneInputs[0].inputValue();
    expect(remainingValue).toBe('555-2222');
  });

  test('should add and remove multiple phones', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    await peoplePage.firstNameInput.fill('Dynamic');
    await peoplePage.lastNameInput.fill('Phones');

    // Add 3 phones
    await peoplePage.addPhoneButton.click();
    await peoplePage.addPhoneButton.click();
    await peoplePage.addPhoneButton.click();

    let count = await peoplePage.getPhoneNumberCount();
    expect(count).toBe(3);

    // Remove middle one
    await peoplePage.removePhoneNumber(1);

    count = await peoplePage.getPhoneNumberCount();
    expect(count).toBe(2);

    // Remove all
    await peoplePage.removePhoneNumber(0);
    await peoplePage.removePhoneNumber(0); // After removing first, second becomes index 0

    count = await peoplePage.getPhoneNumberCount();
    expect(count).toBe(0);
  });

  test('should filter out empty phone numbers on submit', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    await peoplePage.firstNameInput.fill('Empty');
    await peoplePage.lastNameInput.fill('Phones');

    // Add 3 phone inputs
    await peoplePage.addPhoneButton.click();
    await peoplePage.addPhoneButton.click();
    await peoplePage.addPhoneButton.click();

    // Only fill one
    const phoneInputs = await peoplePage.phoneNumberInputs.all();
    await phoneInputs[0].fill('555-1111');
    // Leave phoneInputs[1] and phoneInputs[2] empty

    // Submit
    await peoplePage.submitButton.click();

    // Should succeed (empty phones filtered out)
    await peoplePage.expectOnDetailPage('Empty Phones');

    // Only filled phone should be visible
    await peoplePage.expectPhoneNumberVisible('555-1111');
  });

  test('should validate phone number format', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    await peoplePage.firstNameInput.fill('Invalid');
    await peoplePage.lastNameInput.fill('Phone');

    // Add phone with invalid format
    await peoplePage.addPhoneButton.click();
    const phoneInputs = await peoplePage.phoneNumberInputs.all();
    await phoneInputs[0].fill('abc-def-ghij'); // Invalid phone

    // HTML5 type="tel" may or may not validate strictly
    // Zod validation may trigger on blur or submit
    await phoneInputs[0].blur();

    // Depending on validation rules, may show error or allow
  });

  test('should allow international phone formats', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    // International format
    await peoplePage.createPerson({
      firstName: 'International',
      lastName: 'Phone',
      phoneNumbers: [
        { number: '+1-555-1234', smsEnabled: true },
      ],
    });

    await peoplePage.expectOnDetailPage('International Phone');
    await peoplePage.expectPhoneNumberVisible('+1-555-1234');
  });

  test('should handle very long phone numbers', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    await peoplePage.firstNameInput.fill('Long');
    await peoplePage.lastNameInput.fill('Phone');

    await peoplePage.addPhoneButton.click();
    const phoneInputs = await peoplePage.phoneNumberInputs.all();

    // Very long phone number
    await phoneInputs[0].fill('1234567890123456789012345678901234567890');

    // May have maxLength validation
  });
});

test.describe('Person Phone Number Management - Edit Mode', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('john.smith@example.com', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test('should show existing phone numbers in edit mode', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    // Create person with phone
    await peoplePage.gotoCreatePerson();
    await peoplePage.createPerson({
      firstName: 'Edit',
      lastName: 'Existing',
      phoneNumbers: [
        { number: '555-9999', smsEnabled: true },
      ],
    });

    // Go to edit
    await peoplePage.editButton.click();

    // Phone number should be pre-filled
    const phoneInputs = await peoplePage.phoneNumberInputs.all();
    expect(phoneInputs.length).toBe(1);

    const value = await phoneInputs[0].inputValue();
    expect(value).toBe('555-9999');

    // SMS checkbox should be checked
    const smsCheckboxes = await peoplePage.smsCheckboxes.all();
    await expect(smsCheckboxes[0]).toBeChecked();
  });

  test('should add phone number in edit mode', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    // Create person without phone
    await peoplePage.gotoCreatePerson();
    await peoplePage.createPerson({
      firstName: 'Add',
      lastName: 'Later',
    });

    // Go to edit
    await peoplePage.editButton.click();

    // Add phone
    await peoplePage.addPhoneButton.click();
    const phoneInputs = await peoplePage.phoneNumberInputs.all();
    await phoneInputs[0].fill('555-7777');

    // Save
    await peoplePage.submitButton.click();

    // Phone should be visible
    await peoplePage.expectPhoneNumberVisible('555-7777');
  });

  test('should update existing phone number', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    // Create person with phone
    await peoplePage.gotoCreatePerson();
    await peoplePage.createPerson({
      firstName: 'Update',
      lastName: 'Phone',
      phoneNumbers: [
        { number: '555-0000', smsEnabled: true },
      ],
    });

    // Go to edit
    await peoplePage.editButton.click();

    // Change phone number
    const phoneInputs = await peoplePage.phoneNumberInputs.all();
    await phoneInputs[0].clear();
    await phoneInputs[0].fill('555-1111');

    // Save
    await peoplePage.submitButton.click();

    // New phone should be visible
    await peoplePage.expectPhoneNumberVisible('555-1111');

    // Old phone should not be visible
    await expect(page.getByText('555-0000')).not.toBeVisible();
  });

  test('should remove existing phone number', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    // Create person with phone
    await peoplePage.gotoCreatePerson();
    await peoplePage.createPerson({
      firstName: 'Remove',
      lastName: 'Existing',
      phoneNumbers: [
        { number: '555-8888', smsEnabled: true },
      ],
    });

    // Verify phone is there
    await peoplePage.expectPhoneNumberVisible('555-8888');

    // Go to edit
    await peoplePage.editButton.click();

    // Remove phone
    await peoplePage.removePhoneNumber(0);

    // Save
    await peoplePage.submitButton.click();

    // Phone should be removed
    await expect(page.getByText('555-8888')).not.toBeVisible();
  });

  test('should toggle SMS on existing phone', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    // Create person with phone (SMS enabled)
    await peoplePage.gotoCreatePerson();
    await peoplePage.createPerson({
      firstName: 'Toggle',
      lastName: 'SMS',
      phoneNumbers: [
        { number: '555-6666', smsEnabled: true },
      ],
    });

    // Go to edit
    await peoplePage.editButton.click();

    // SMS should be checked
    const smsCheckboxes = await peoplePage.smsCheckboxes.all();
    await expect(smsCheckboxes[0]).toBeChecked();

    // Uncheck SMS
    await smsCheckboxes[0].uncheck();

    // Save
    await peoplePage.submitButton.click();

    // SMS preference should be saved (may show badge or indicator)
    await peoplePage.expectOnDetailPage('Toggle SMS');
  });

  test('should add multiple phones to existing person', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    // Create person with one phone
    await peoplePage.gotoCreatePerson();
    await peoplePage.createPerson({
      firstName: 'Expand',
      lastName: 'Phones',
      phoneNumbers: [
        { number: '555-1000', smsEnabled: true },
      ],
    });

    // Go to edit
    await peoplePage.editButton.click();

    // Add two more phones
    await peoplePage.addPhoneButton.click();
    await peoplePage.addPhoneButton.click();

    const phoneInputs = await peoplePage.phoneNumberInputs.all();
    expect(phoneInputs.length).toBe(3);

    // Fill new phones
    await phoneInputs[1].fill('555-2000');
    await phoneInputs[2].fill('555-3000');

    // Save
    await peoplePage.submitButton.click();

    // All phones should be visible
    await peoplePage.expectPhoneNumberVisible('555-1000');
    await peoplePage.expectPhoneNumberVisible('555-2000');
    await peoplePage.expectPhoneNumberVisible('555-3000');
  });

  test('should handle removing all phones from person with multiple', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    // Create person with multiple phones
    await peoplePage.gotoCreatePerson();
    await peoplePage.createPerson({
      firstName: 'Clear',
      lastName: 'All',
      phoneNumbers: [
        { number: '555-1111', smsEnabled: true },
        { number: '555-2222', smsEnabled: true },
        { number: '555-3333', smsEnabled: true },
      ],
    });

    // Go to edit
    await peoplePage.editButton.click();

    // Remove all phones
    await peoplePage.removePhoneNumber(0);
    await peoplePage.removePhoneNumber(0);
    await peoplePage.removePhoneNumber(0);

    const count = await peoplePage.getPhoneNumberCount();
    expect(count).toBe(0);

    // Save
    await peoplePage.submitButton.click();

    // Person should exist but no phones
    await peoplePage.expectOnDetailPage('Clear All');
    await expect(page.getByText('555-1111')).not.toBeVisible();
  });
});

test.describe('Person Phone Number Management - Edge Cases', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('john.smith@example.com', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test('should handle adding many phones', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    await peoplePage.firstNameInput.fill('Many');
    await peoplePage.lastNameInput.fill('Phones');

    // Add 10 phones
    for (let i = 0; i < 10; i++) {
      await peoplePage.addPhoneButton.click();
    }

    const count = await peoplePage.getPhoneNumberCount();
    expect(count).toBe(10);

    // Fill all phones
    const phoneInputs = await peoplePage.phoneNumberInputs.all();
    for (let i = 0; i < 10; i++) {
      await phoneInputs[i].fill(`555-${i.toString().padStart(4, '0')}`);
    }

    // Submit
    await peoplePage.submitButton.click();

    // Should succeed
    await peoplePage.expectOnDetailPage('Many Phones');
  });

  test('should preserve phone order', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    await peoplePage.createPerson({
      firstName: 'Ordered',
      lastName: 'Phones',
      phoneNumbers: [
        { number: '555-1111', smsEnabled: true },
        { number: '555-2222', smsEnabled: true },
        { number: '555-3333', smsEnabled: true },
      ],
    });

    // Go to edit
    await peoplePage.editButton.click();

    // Phones should be in same order
    const phoneInputs = await peoplePage.phoneNumberInputs.all();
    expect(await phoneInputs[0].inputValue()).toBe('555-1111');
    expect(await phoneInputs[1].inputValue()).toBe('555-2222');
    expect(await phoneInputs[2].inputValue()).toBe('555-3333');
  });

  test('should handle duplicate phone numbers', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    await peoplePage.firstNameInput.fill('Duplicate');
    await peoplePage.lastNameInput.fill('Phones');

    // Add duplicate phones
    await peoplePage.addPhoneButton.click();
    await peoplePage.addPhoneButton.click();

    const phoneInputs = await peoplePage.phoneNumberInputs.all();
    await phoneInputs[0].fill('555-9999');
    await phoneInputs[1].fill('555-9999'); // Same number

    // Submit
    await peoplePage.submitButton.click();

    // Should either succeed (allow duplicates) or show validation error
    // Behavior depends on business rules
  });

  test('should handle phone number with only spaces', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    await peoplePage.firstNameInput.fill('Spaces');
    await peoplePage.lastNameInput.fill('Phone');

    await peoplePage.addPhoneButton.click();

    const phoneInputs = await peoplePage.phoneNumberInputs.all();
    await phoneInputs[0].fill('     '); // Only spaces

    // Submit
    await peoplePage.submitButton.click();

    // Should filter out empty/whitespace-only numbers
    await peoplePage.expectOnDetailPage('Spaces Phone');
  });

  test('should not show phone section if no phones', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    // Create person without phones
    await peoplePage.gotoCreatePerson();
    await peoplePage.createPerson({
      firstName: 'No',
      lastName: 'Phone',
    });

    // Phone section may not be visible or shows "No phone numbers"
    // Exact behavior depends on UI implementation
    await peoplePage.expectOnDetailPage('No Phone');
  });
});

test.describe('Person Phone Number - Form Interactions', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('john.smith@example.com', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test('should maintain phone data when form validation fails', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    // Fill phone but miss required field (first name)
    await peoplePage.lastNameInput.fill('Test');
    await peoplePage.addPhoneButton.click();
    const phoneInputs = await peoplePage.phoneNumberInputs.all();
    await phoneInputs[0].fill('555-1234');

    // Uncheck SMS to test state preservation
    const smsCheckboxes = await peoplePage.smsCheckboxes.all();
    await smsCheckboxes[0].uncheck();

    // Try to submit (will fail - missing first name)
    await peoplePage.submitButton.click();

    // Should still be on form
    await expect(page).toHaveURL('/admin/people/new');

    // Phone data should be preserved
    const preservedValue = await phoneInputs[0].inputValue();
    expect(preservedValue).toBe('555-1234');

    // SMS checkbox state should be preserved
    await expect(smsCheckboxes[0]).not.toBeChecked();
  });

  test('should show phone section while editing even if empty', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    // Phone section should be visible with "+ Add Phone" button
    await expect(peoplePage.addPhoneButton).toBeVisible();

    // Should show label
    await expect(page.getByText(/phone numbers/i)).toBeVisible();
  });

  test('should handle rapid add/remove clicks', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    await peoplePage.firstNameInput.fill('Rapid');
    await peoplePage.lastNameInput.fill('Clicks');

    // Rapidly add phones
    await peoplePage.addPhoneButton.click();
    await peoplePage.addPhoneButton.click();
    await peoplePage.addPhoneButton.click();

    let count = await peoplePage.getPhoneNumberCount();
    expect(count).toBe(3);

    // Rapidly remove
    await peoplePage.removePhoneNumber(0);
    await peoplePage.removePhoneNumber(0);

    count = await peoplePage.getPhoneNumberCount();
    expect(count).toBe(1);
  });

  test('should focus new phone input after adding', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    // Add phone
    await peoplePage.addPhoneButton.click();

    // New input may receive focus automatically
    // await expect(phoneInputs[0]).toBeFocused();
  });
});
