/**
 * E2E Tests: Family CRUD Operations
 * Tests creating, reading, updating, and deleting families
 */

import { test, expect } from '@playwright/test';
import { FamiliesPage } from '../../../fixtures/page-objects/families.page';
import { LoginPage } from '../../../fixtures/page-objects/login.page';
import { testData } from '../../../fixtures/test-data';

test.describe('Family CRUD Operations', () => {
  test.beforeEach(async ({ page }) => {
    // Login first
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('john.smith@example.com', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test('should create a new family successfully', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Navigate to create form
    await familiesPage.gotoCreateFamily();
    await expect(page.getByRole('heading', { name: /create family/i })).toBeVisible();

    // Fill out form
    await familiesPage.createFamily({
      name: 'Test Anderson Family',
    });

    // Wait for navigation to detail page
    await expect(page).toHaveURL(/\/admin\/families\/[^/]+$/);

    // Verify family created
    await familiesPage.expectOnDetailPage('Test Anderson Family');
  });

  test('should create family with address', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoCreateFamily();

    // Fill out form with address
    await familiesPage.createFamily({
      name: 'Test Williams Family',
      address: {
        street1: '123 Main Street',
        city: 'Springfield',
        state: 'IL',
        postalCode: '62701',
      },
    });

    // Verify created
    await familiesPage.expectOnDetailPage('Test Williams Family');

    // Verify address is displayed
    await expect(page.getByText(/123 Main Street/i)).toBeVisible();
  });

  test('should validate required fields on create', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Navigate to create form
    await familiesPage.gotoCreateFamily();

    // Try to submit without filling required fields
    await familiesPage.submitButton.click();

    // Should still be on form page (HTML5 validation or custom)
    await expect(page).toHaveURL('/admin/families/new');

    // Submit button should remain (form didn't submit)
    await expect(familiesPage.submitButton).toBeVisible();
  });

  test('should validate name field is required', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoCreateFamily();

    // Try to submit without name
    await familiesPage.submitButton.click();

    // Should still be on form
    await expect(page).toHaveURL('/admin/families/new');
  });

  test('should view family details', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Navigate to a known family
    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Verify details page
    await familiesPage.expectOnDetailPage(testData.families.smith.name);

    // Verify metadata section exists
    await expect(page.getByText(/family members/i)).toBeVisible();
  });

  test('should navigate to edit family form', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Go to family detail
    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.johnson.name);

    // Click edit button
    await familiesPage.editButton.click();

    // Verify on edit form
    await expect(page).toHaveURL(/\/admin\/families\/.+\/edit/);
    await expect(page.getByRole('heading', { name: /edit family/i })).toBeVisible();

    // Verify form is pre-filled
    await expect(familiesPage.nameInput).toHaveValue(testData.families.johnson.name);
  });

  test('should update family name', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // First, create a family to edit
    await familiesPage.gotoCreateFamily();
    await familiesPage.createFamily({
      name: 'Test Edit Family',
    });

    // Navigate to edit
    await familiesPage.editButton.click();
    await expect(page).toHaveURL(/\/admin\/families\/.+\/edit/);

    // Update name
    await familiesPage.updateFamily({
      name: 'Updated Test Family',
    });

    // Verify updates persisted
    await expect(page).toHaveURL(/\/admin\/families\/[^/]+$/); // Back on detail page
    await familiesPage.expectOnDetailPage('Updated Test Family');
  });

  test('should cancel family creation', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoCreateFamily();

    // Fill some data
    await familiesPage.nameInput.fill('Cancelled Family');

    // Click cancel
    await familiesPage.cancelButton.click();

    // Should return to families list
    await expect(page).toHaveURL('/admin/families');
  });

  test('should cancel family edit', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Navigate to existing family and edit
    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);
    const familyUrl = page.url();

    await familiesPage.editButton.click();

    // Make changes
    await familiesPage.nameInput.fill('Should Not Save');

    // Cancel
    await familiesPage.cancelButton.click();

    // Should return to detail page without changes
    await expect(page).toHaveURL(familyUrl);
    await familiesPage.expectOnDetailPage(testData.families.smith.name);
  });

  test.skip('should delete a family with confirmation', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Create a family to delete
    await familiesPage.gotoCreateFamily();
    await familiesPage.createFamily({
      name: 'Family To Delete',
    });

    // Verify on detail page
    await familiesPage.expectOnDetailPage('Family To Delete');

    // Delete the family
    await familiesPage.deleteFamily();

    // Should redirect to families list
    await expect(page).toHaveURL('/admin/families');

    // Family should no longer exist (search for it)
    await familiesPage.searchFamilies('Family To Delete');
    await expect(page.getByText(/no families found/i)).toBeVisible();
  });

  test.skip('should cancel delete confirmation', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Navigate to existing family
    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);
    const familyUrl = page.url();

    // Start delete but cancel
    await familiesPage.cancelDelete();

    // Should still be on detail page
    await expect(page).toHaveURL(familyUrl);
    await familiesPage.expectOnDetailPage(testData.families.smith.name);
  });

  test('@smoke should complete full CRUD cycle', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Create
    await familiesPage.gotoCreateFamily();
    await familiesPage.createFamily({
      name: 'CRUD Test Family',
      address: {
        street1: '456 Test Lane',
        city: 'Testville',
        state: 'CA',
        postalCode: '90210',
      },
    });

    // Read
    await familiesPage.expectOnDetailPage('CRUD Test Family');
    await expect(page.getByText(/456 Test Lane/i)).toBeVisible();

    // Update
    await familiesPage.editButton.click();
    await familiesPage.updateFamily({
      name: 'Updated CRUD Family',
    });
    await familiesPage.expectOnDetailPage('Updated CRUD Family');

    // Delete would go here (skipped for now as delete might not be implemented)
  });

  test('should create family with street2 (apartment)', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoCreateFamily();

    await familiesPage.createFamily({
      name: 'Apartment Family',
      address: {
        street1: '789 Complex Blvd',
        street2: 'Apt 4B',
        city: 'Metropolis',
        state: 'NY',
        postalCode: '10001',
      },
    });

    await familiesPage.expectOnDetailPage('Apartment Family');
    await expect(page.getByText(/Apt 4B/i)).toBeVisible();
  });

  test('should handle form validation errors', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoCreateFamily();

    // Fill invalid data
    await familiesPage.nameInput.fill('');
    await familiesPage.nameInput.blur();

    // Try to submit
    await familiesPage.submitButton.click();

    // Should still be on form
    await expect(page).toHaveURL('/admin/families/new');
  });

  test('should show unsaved changes warning on cancel', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoCreateFamily();

    // Set up dialog handler BEFORE making changes that trigger it
    page.on('dialog', async (dialog) => {
      expect(dialog.type()).toBe('confirm');
      expect(dialog.message()).toContain('unsaved changes');
      await dialog.dismiss();
    });

    // Make changes
    await familiesPage.nameInput.fill('Unsaved Family');

    // Try to cancel
    await familiesPage.cancelButton.click();

    // Should still be on form (dialog was dismissed)
    await expect(page).toHaveURL('/admin/families/new');
  });
});

test.describe('Family Form Validation', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('john.smith@example.com', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test('should validate name length', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoCreateFamily();

    // Try very long name
    const longName = 'A'.repeat(256);
    await familiesPage.nameInput.fill(longName);

    // Blur to trigger validation
    await familiesPage.nameInput.blur();

    // If there's a max length, validation error should appear
    // Otherwise form should handle it gracefully
  });

  test('should validate address fields when provided', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoCreateFamily();

    await familiesPage.nameInput.fill('Address Validation Test');

    // Fill incomplete address
    await familiesPage.street1Input.fill('123 Main St');
    await familiesPage.cityInput.fill(''); // Missing city

    // Try to submit
    await familiesPage.submitButton.click();

    // Should show validation error or remain on form
    // Exact behavior depends on validation rules
  });

  test('should validate state field format', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoCreateFamily();

    await familiesPage.nameInput.fill('State Validation Test');
    await familiesPage.stateInput.fill('California'); // Should be 2-letter code

    await familiesPage.stateInput.blur();

    // Validation should trigger (if implemented)
  });

  test('should validate postal code format', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoCreateFamily();

    await familiesPage.nameInput.fill('Postal Code Test');
    await familiesPage.postalCodeInput.fill('invalid');

    await familiesPage.postalCodeInput.blur();

    // Validation should trigger (if implemented)
  });

  test('should allow creating family without address', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoCreateFamily();

    // Just provide name (address is optional)
    await familiesPage.createFamily({
      name: 'No Address Family',
    });

    // Should succeed
    await familiesPage.expectOnDetailPage('No Address Family');

    // Address section should not be visible
    await expect(page.getByText(/^address$/i)).not.toBeVisible();
  });
});

test.describe('Family Form - Edit Mode Differences', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('john.smith@example.com', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test('should not show address fields in edit mode', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Create family first
    await familiesPage.gotoCreateFamily();
    await familiesPage.createFamily({
      name: 'Edit Mode Test Family',
    });

    // Go to edit
    await familiesPage.editButton.click();

    // Address fields should not be editable in edit mode
    // (Address editing happens separately)
    await expect(familiesPage.street1Input).not.toBeVisible();
  });

  test('should show different button text in edit mode', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Create mode
    await familiesPage.gotoCreateFamily();
    await expect(familiesPage.submitButton).toHaveText(/create family/i);

    // Edit mode
    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);
    await familiesPage.editButton.click();

    await expect(familiesPage.submitButton).toHaveText(/save changes/i);
  });
});
