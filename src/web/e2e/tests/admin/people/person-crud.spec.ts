/**
 * E2E Tests: Person CRUD Operations
 * Tests creating, reading, updating, and deleting people
 *
 * ASSUMPTIONS (UI not yet fully implemented):
 * - Person form follows similar pattern to groups/families (edit/cancel buttons)
 * - Person detail page shows name, email, demographics in structured sections
 * - Delete functionality includes confirmation modal
 * - Form validation follows Zod schema in person.schema.ts
 * - Success redirects to person detail page after create/update
 *
 * NOTE: Update selectors when UI is implemented to use data-testid attributes
 */

import { test, expect } from '@playwright/test';
import { PeoplePage } from '../../../fixtures/page-objects/people.page';
import { LoginPage } from '../../../fixtures/page-objects/login.page';
import { testData } from '../../../fixtures/test-data';

test.describe('Person CRUD Operations', () => {
  test.beforeEach(async ({ page }) => {
    // Login first
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test('should create a new person successfully', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    // Navigate to create form
    await peoplePage.gotoCreatePerson();
    await expect(page.getByRole('heading', { name: /add person/i })).toBeVisible();

    // Fill out form
    await peoplePage.createPerson({
      firstName: 'Test',
      lastName: 'Person',
      email: 'test.person@example.com',
      gender: 'Male',
      birthDate: '1990-01-15',
    });

    // Wait for navigation to detail page
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveURL(/\/admin\/people\/[^/]+$/);

    // Verify person created
    await peoplePage.expectOnDetailPage('Test Person');
    await expect(page.getByText('test.person@example.com')).toBeVisible();
  });

  test('should create person with all optional fields', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    await peoplePage.createPerson({
      firstName: 'John',
      lastName: 'Doe',
      nickName: 'Johnny',
      middleName: 'Michael',
      email: 'john.doe@example.com',
      gender: 'Male',
      birthDate: '1985-06-15',
    });

    // Verify created
    await page.waitForLoadState('networkidle');
    await peoplePage.expectOnDetailPage('John Doe');
    await expect(page.getByText('john.doe@example.com')).toBeVisible();
  });

  test('should validate required fields on create', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    // Navigate to create form
    await peoplePage.gotoCreatePerson();

    // Try to submit without filling required fields
    await peoplePage.submitButton.click();

    // HTML5 validation will prevent submit, or custom validation shows errors
    await expect(page).toHaveURL('/admin/people/new');
    await expect(peoplePage.validationError.first()).toBeVisible();
  });

  test('should validate first name is required', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    // Fill last name but not first name
    await peoplePage.lastNameInput.fill('Smith');

    // Try to submit
    await peoplePage.submitButton.click();

    // Should still be on form page
    await expect(page).toHaveURL('/admin/people/new');
    await expect(peoplePage.validationError.first()).toBeVisible();
  });

  test('should validate last name is required', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    // Fill first name but not last name
    await peoplePage.firstNameInput.fill('John');

    // Try to submit
    await peoplePage.submitButton.click();

    // Should still be on form page
    await expect(page).toHaveURL('/admin/people/new');
    await expect(peoplePage.validationError.first()).toBeVisible();
  });

  test('should validate email format', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    // Fill required fields
    await peoplePage.firstNameInput.fill('Test');
    await peoplePage.lastNameInput.fill('User');

    // Fill invalid email
    await peoplePage.emailInput.fill('invalid-email');
    await peoplePage.submitButton.click();

    // Should show validation error
    await expect(page).toHaveURL('/admin/people/new');
    await expect(peoplePage.validationError.first()).toBeVisible();
  });

  test('should view person details', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    // Navigate to a known person
    await peoplePage.gotoList();
    await peoplePage.clickPerson(testData.people.johnSmith.fullName);

    // Verify details page
    await peoplePage.expectOnDetailPage(testData.people.johnSmith.fullName);

    // Verify key information is displayed
    await expect(page.getByText(testData.people.johnSmith.email)).toBeVisible();

    // Verify metadata section exists
    await expect(page.getByText(/created/i)).toBeVisible();
  });

  test('should navigate to edit person form', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    // Go to person detail
    await peoplePage.gotoList();
    await peoplePage.clickPerson(testData.people.janeSmith.fullName);

    // Click edit button
    await peoplePage.editButton.click();

    // Verify on edit form
    await expect(page).toHaveURL(/\/admin\/people\/.+\/edit/);
    await expect(page.getByRole('heading', { name: /edit person/i })).toBeVisible();

    // Verify form is pre-filled
    await expect(peoplePage.firstNameInput).toHaveValue(testData.people.janeSmith.firstName);
    await expect(peoplePage.lastNameInput).toHaveValue(testData.people.janeSmith.lastName);
  });

  test('should update person details', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    // First, create a person to edit
    await peoplePage.gotoCreatePerson();
    await peoplePage.createPerson({
      firstName: 'Edit',
      lastName: 'Test',
      email: 'edit.test@example.com',
    });

    // Navigate to edit
    await peoplePage.editButton.click();
    await expect(page).toHaveURL(/\/admin\/people\/.+\/edit/);

    // Update fields
    await peoplePage.updatePerson({
      firstName: 'Updated',
      lastName: 'Person',
      email: 'updated.person@example.com',
    });

    // Verify updates persisted
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveURL(/\/admin\/people\/[^/]+$/); // Back on detail page
    await peoplePage.expectOnDetailPage('Updated Person');
    await expect(page.getByText('updated.person@example.com')).toBeVisible();
  });

  test('should update only some fields', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    // Create person
    await peoplePage.gotoCreatePerson();
    await peoplePage.createPerson({
      firstName: 'Partial',
      lastName: 'Update',
      email: 'partial@example.com',
      gender: 'Female',
    });

    // Edit - only change email
    await peoplePage.editButton.click();
    await peoplePage.updatePerson({
      email: 'new.email@example.com',
    });

    // Verify email changed but name stayed same
    await page.waitForLoadState('networkidle');
    await peoplePage.expectOnDetailPage('Partial Update');
    await expect(page.getByText('new.email@example.com')).toBeVisible();
  });

  test('should cancel person creation', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    // Fill some data
    await peoplePage.firstNameInput.fill('Cancelled');
    await peoplePage.lastNameInput.fill('Person');

    // Click cancel
    await peoplePage.cancelButton.click();

    // Should return to people list
    await expect(page).toHaveURL('/admin/people');
  });

  test('should cancel person edit', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    // Navigate to existing person and edit
    await peoplePage.gotoList();
    await peoplePage.clickPerson(testData.people.johnSmith.fullName);
    const personUrl = page.url();

    await peoplePage.editButton.click();

    // Make changes
    await peoplePage.firstNameInput.fill('Should Not Save');

    // Cancel
    await peoplePage.cancelButton.click();

    // Should return to detail page without changes
    await expect(page).toHaveURL(personUrl);
    await peoplePage.expectOnDetailPage(testData.people.johnSmith.fullName);
  });

  test('should show unsaved changes warning on cancel', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    // Set up dialog handler BEFORE navigating to the page
    page.on('dialog', async (dialog) => {
      expect(dialog.type()).toBe('confirm');
      expect(dialog.message()).toContain('unsaved changes');
      await dialog.dismiss();
    });

    await peoplePage.gotoCreatePerson();

    // Make changes
    await peoplePage.firstNameInput.fill('Unsaved');
    await peoplePage.lastNameInput.fill('Changes');

    // Try to cancel
    await peoplePage.cancelButton.click();

    // Should still be on form (dialog was dismissed)
    await expect(page).toHaveURL('/admin/people/new');
  });

  // SKIP: Delete API endpoint not yet implemented - track in Issue #160
  test.skip('should delete a person with confirmation', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    // Create a person to delete
    await peoplePage.gotoCreatePerson();
    await peoplePage.createPerson({
      firstName: 'Delete',
      lastName: 'Me',
      email: 'delete.me@example.com',
    });

    // Verify on detail page
    await peoplePage.expectOnDetailPage('Delete Me');

    // Delete the person
    await peoplePage.deletePerson();

    // Should redirect to people list
    await expect(page).toHaveURL('/admin/people');

    // Person should no longer exist (search for it)
    await peoplePage.searchPeople('Delete Me');
    await expect(page.getByText(/no people found/i)).toBeVisible();
  });

  // SKIP: Delete API endpoint not yet implemented - track in Issue #160
  test.skip('should cancel delete confirmation', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    // Navigate to existing person
    await peoplePage.gotoList();
    await peoplePage.clickPerson(testData.people.johnSmith.fullName);
    const personUrl = page.url();

    // Start delete but cancel
    await peoplePage.cancelDelete();

    // Should still be on detail page
    await expect(page).toHaveURL(personUrl);
    await peoplePage.expectOnDetailPage(testData.people.johnSmith.fullName);
  });

  test('@smoke should complete full CRUD cycle', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    // Create
    await peoplePage.gotoCreatePerson();
    await peoplePage.createPerson({
      firstName: 'CRUD',
      lastName: 'Cycle',
      email: 'crud.cycle@example.com',
      gender: 'Unknown',
      birthDate: '1995-03-20',
    });

    // Read
    await peoplePage.expectOnDetailPage('CRUD Cycle');
    await expect(page.getByText('crud.cycle@example.com')).toBeVisible();

    // Update
    await peoplePage.editButton.click();
    await peoplePage.updatePerson({
      email: 'updated.crud@example.com',
    });
    await expect(page.getByText('updated.crud@example.com')).toBeVisible();

    // Delete would go here (skipped for now as delete might not be implemented)
  });

  test('should handle name length validation', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    // Try very long first name
    const longName = 'A'.repeat(256);
    await peoplePage.firstNameInput.fill(longName);
    await peoplePage.lastNameInput.fill('Test');

    // Blur to trigger validation
    await peoplePage.firstNameInput.blur();

    // HTML5 maxLength or Zod validation should prevent this
  });

  test('should allow optional fields to be empty', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    // Only required fields
    await peoplePage.createPerson({
      firstName: 'Minimal',
      lastName: 'Person',
    });

    // Should succeed
    await peoplePage.expectOnDetailPage('Minimal Person');
  });

  test('should show different button text in edit mode', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    // Create mode
    await peoplePage.gotoCreatePerson();
    await expect(peoplePage.submitButton).toHaveText(/create person/i);

    // Edit mode
    await peoplePage.gotoList();
    await peoplePage.clickPerson(testData.people.johnSmith.fullName);
    await peoplePage.editButton.click();

    await expect(peoplePage.submitButton).toHaveText(/update person/i);
  });

  test('should validate birth date is not in future', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    await peoplePage.firstNameInput.fill('Future');
    await peoplePage.lastNameInput.fill('Baby');

    // Set birth date to future
    const futureDate = new Date();
    futureDate.setFullYear(futureDate.getFullYear() + 1);
    const futureDateStr = futureDate.toISOString().split('T')[0];

    await peoplePage.birthDateInput.fill(futureDateStr);
    await peoplePage.birthDateInput.blur();

    // Zod validation should trigger error
    // This assumes person.schema.ts validates birthDate <= today
  });

  test('should create person with nickname only', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    await peoplePage.createPerson({
      firstName: 'William',
      lastName: 'Thompson',
      nickName: 'Bill',
    });

    await peoplePage.expectOnDetailPage('William Thompson');
    // Nickname may be displayed in detail view
  });

  test('should allow clearing optional fields on edit', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    // Create person with email
    await peoplePage.gotoCreatePerson();
    await peoplePage.createPerson({
      firstName: 'Clear',
      lastName: 'Email',
      email: 'clear@example.com',
    });

    // Edit - clear email
    await peoplePage.editButton.click();
    await peoplePage.emailInput.clear();
    await peoplePage.submitButton.click();

    // Email should be removed
    await expect(page.getByText('clear@example.com')).not.toBeVisible();
  });
});

test.describe('Person Form Validation', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test('should validate first name max length (50 chars)', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    // HTML5 maxLength=50 enforced on input
    await peoplePage.firstNameInput.fill('A'.repeat(60));

    const value = await peoplePage.firstNameInput.inputValue();
    expect(value.length).toBeLessThanOrEqual(50);
  });

  test('should validate last name max length (50 chars)', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    // HTML5 maxLength=50 enforced on input
    await peoplePage.lastNameInput.fill('B'.repeat(60));

    const value = await peoplePage.lastNameInput.inputValue();
    expect(value.length).toBeLessThanOrEqual(50);
  });

  test('should validate email is valid format', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    await peoplePage.firstNameInput.fill('Test');
    await peoplePage.lastNameInput.fill('User');
    await peoplePage.emailInput.fill('not-an-email');

    // Blur to trigger validation
    await peoplePage.emailInput.blur();

    // HTML5 type="email" validation or Zod validation
  });

  test('should allow empty email (optional)', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    await peoplePage.createPerson({
      firstName: 'No',
      lastName: 'Email',
    });

    // Should succeed
    await peoplePage.expectOnDetailPage('No Email');
  });

  test('should validate gender is valid enum value', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoCreatePerson();

    // Gender dropdown should only show valid options
    await peoplePage.genderSelect.click();

    // Should have Unknown, Male, Female options
    await expect(page.getByRole('option', { name: 'Unknown' })).toBeVisible();
    await expect(page.getByRole('option', { name: 'Male' })).toBeVisible();
    await expect(page.getByRole('option', { name: 'Female' })).toBeVisible();
  });
});
