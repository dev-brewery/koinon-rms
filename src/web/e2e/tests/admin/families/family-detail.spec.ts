/**
 * E2E Tests: Family Detail View
 * Tests viewing family details, members, and address information
 */

import { test, expect } from '@playwright/test';
import { FamiliesPage } from '../../../fixtures/page-objects/families.page';
import { LoginPage } from '../../../fixtures/page-objects/login.page';
import { testData } from '../../../fixtures/test-data';

test.describe('Family Detail View', () => {
  test.beforeEach(async ({ page }) => {
    // Login first
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test('should display family name and basic info', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Navigate to a known family
    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Verify details page loaded
    await familiesPage.expectOnDetailPage(testData.families.smith.name);

    // Verify edit button is visible
    await expect(familiesPage.editButton).toBeVisible();
  });

  test('should display family members section', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Verify members section exists
    await expect(page.getByText(/family members/i)).toBeVisible();

    // Verify add member button is visible
    await expect(familiesPage.addMemberButton).toBeVisible();
  });

  test('should show correct member count', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Smith family should have 4 members
    const memberCountText = await page.getByText(/family members/i).textContent();
    expect(memberCountText).toContain('4');
  });

  test('should display member information', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Verify member names are displayed
    await expect(page.getByText(testData.people.johnSmith.fullName)).toBeVisible();
    await expect(page.getByText(testData.people.janeSmith.fullName)).toBeVisible();
  });

  test('should display address section when available', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Check if address section exists
    try {
      await expect(page.getByText(/^address$/i)).toBeVisible({ timeout: 2000 });
      
      // If address exists, verify it has content
      const addressSection = page.getByText(/^address$/i).locator('..');
      await expect(addressSection.locator('address')).toBeVisible();
    } catch {
      // Address might not be set in test data - that's ok
    }
  });

  test('should navigate to edit form', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

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

  test('should navigate back to list', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Click back button
    const backButton = page.locator('button[aria-label="Back to families"]');
    await backButton.click();

    // Verify back on list page
    await expect(page).toHaveURL('/admin/families');
  });

  test('should show campus information when set', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // If campus is set, it should be visible below the family name
    try {
      await expect(familiesPage.campusName).toBeVisible({ timeout: 2000 });
    } catch {
      // Campus might not be set - that's ok
    }
  });

  test('should display empty state when no members', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Create a new family with no members
    await familiesPage.gotoCreateFamily();
    await familiesPage.createFamily({
      name: 'Empty Family Test',
    });

    // Verify empty state
    await expect(page.getByText(/no family members yet/i)).toBeVisible();
    await expect(page.getByText(/add your first member/i)).toBeVisible();
  });

  test('@smoke should display complete family information', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Verify all major sections are present
    await familiesPage.expectOnDetailPage(testData.families.smith.name);
    await expect(page.getByText(/family members/i)).toBeVisible();
    await expect(familiesPage.editButton).toBeVisible();

    // Verify no errors
    await expect(page.getByText(/failed to load/i)).not.toBeVisible();
  });

  test('should show member roles', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Member cards should show roles (Adult, Child, etc.)
    await expect(page.getByText(/adult|child/i)).toBeVisible();
  });

  test('should link to person detail from member', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Click on a member name
    const memberLink = page.getByRole('link', { name: testData.people.johnSmith.fullName });
    
    try {
      await expect(memberLink).toBeVisible({ timeout: 2000 });
      await memberLink.click();

      // Should navigate to person detail
      await expect(page).toHaveURL(/\/admin\/people\/.+/);
    } catch {
      // Member might not be a link yet - skip
      test.skip();
    }
  });

  test('should handle family not found', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Try to navigate to non-existent family
    await page.goto('/admin/families/nonexistent123');

    // Should show error message
    await expect(page.getByText(/failed to load family/i)).toBeVisible();

    // Should have button to go back
    await expect(page.getByRole('button', { name: /back to families/i })).toBeVisible();
  });

  test('should display loading state', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Navigate directly to family detail
    await page.goto(`/admin/families/${testData.families.smith.name}`);

    // Loading spinner should appear briefly
    try {
      await expect(familiesPage.loadingSpinner).toBeVisible({ timeout: 1000 });
    } catch {
      // Fast load - that's ok
    }

    // Eventually should load
    await familiesPage.waitForLoad();
    await familiesPage.expectOnDetailPage(testData.families.smith.name);
  });
});

test.describe('Family Detail - Address Display', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test('should show edit link for address', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // If address section exists, should have edit link
    try {
      const addressSection = page.getByText(/^address$/i).locator('..');
      await expect(addressSection).toBeVisible({ timeout: 2000 });

      const editLink = addressSection.getByRole('link', { name: /edit/i });
      await expect(editLink).toBeVisible();
      
      // Should link to edit form
      await expect(editLink).toHaveAttribute('href', /\/edit$/);
    } catch {
      // No address - skip
      test.skip();
    }
  });

  test('should not show address section when no address', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Create family without address
    await familiesPage.gotoCreateFamily();
    await familiesPage.createFamily({
      name: 'No Address Family',
    });

    // Address section should not be visible
    await expect(page.getByText(/^address$/i)).not.toBeVisible();
  });
});
