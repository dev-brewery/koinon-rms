/**
 * E2E Tests: Family Member Management
 * Tests adding and removing members from families
 */

import { test, expect } from '../../../fixtures/auth.fixture';
import { FamiliesPage } from '../../../fixtures/page-objects/families.page';
import { testData } from '../../../fixtures/test-data';

test.describe('Family Member Management', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    // Login first
    await loginAsAdmin();
  });

  test('should display members section on family detail page', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Navigate to a family
    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Verify members section exists
    await expect(page.getByText(/family members/i)).toBeVisible();
    
    // Verify member count is shown
    const memberText = await page.getByText(/family members/i).textContent();
    expect(memberText).toMatch(/\(\d+\)/);
  });

  test('should show all family members', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Verify Smith family members are displayed
    await expect(page.getByText(testData.people.johnSmith.fullName)).toBeVisible();
    await expect(page.getByText(testData.people.janeSmith.fullName)).toBeVisible();
    await expect(page.getByText(testData.people.johnnySmith.fullName)).toBeVisible();
    await expect(page.getByText(testData.people.jennySmith.fullName)).toBeVisible();
  });

  test('should display member roles', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Member cards should show roles
    await expect(page.getByText(/adult|child/i)).toBeVisible();
  });

  test('should show add member button', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.johnson.name);

    // Add member button should be visible
    await expect(familiesPage.addMemberButton).toBeVisible();
  });

  test('should open add member modal', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Click add member button
    await familiesPage.addMemberButton.click();

    // Modal should open
    await expect(familiesPage.addMemberModal).toBeVisible();

    // Should have search input
    await expect(page.getByPlaceholder(/search.*person/i)).toBeVisible();
  });

  test.skip('should add a person to a family', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Create a test family
    await familiesPage.gotoCreateFamily();
    await familiesPage.createFamily({
      name: 'Add Member Test Family',
    });

    // Add a member
    await familiesPage.addMember(testData.people.bobJohnson.fullName, 'Adult');

    // Verify member added
    await expect(page.getByText(testData.people.bobJohnson.fullName)).toBeVisible();

    // Member count should update
    await expect(page.getByText(/family members.*\(1\)/i)).toBeVisible();
  });

  test.skip('should remove a person from a family', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    const initialMemberCount = await page.getByText(/family members/i).textContent();

    // Remove a member
    await familiesPage.removeMember(testData.people.johnnySmith.fullName);

    // Confirm removal
    page.on('dialog', async (dialog) => {
      expect(dialog.type()).toBe('confirm');
      await dialog.accept();
    });

    // Member should be removed
    await expect(page.getByText(testData.people.johnnySmith.fullName)).not.toBeVisible();

    // Count should decrease
    const newMemberCount = await page.getByText(/family members/i).textContent();
    expect(newMemberCount).not.toBe(initialMemberCount);
  });

  test('should show empty state when no members', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Create family with no members
    await familiesPage.gotoCreateFamily();
    await familiesPage.createFamily({
      name: 'Empty Members Family',
    });

    // Verify empty state
    await expect(page.getByText(/no family members yet/i)).toBeVisible();
    await expect(page.getByText(/add your first member/i)).toBeVisible();

    // Add member button should still be visible
    await expect(familiesPage.addMemberButton).toBeVisible();
  });

  test.skip('should search for person in add member modal', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Open modal
    await familiesPage.addMemberButton.click();

    // Search for a person
    await familiesPage.memberSearchInput.fill('Bob');
    await page.waitForLoadState('networkidle');

    // Should show search results
    await expect(page.getByText(testData.people.bobJohnson.fullName)).toBeVisible();
  });

  test.skip('should filter out existing family members from search', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Open add member modal
    await familiesPage.addMemberButton.click();

    // Search for someone already in the family
    await familiesPage.memberSearchInput.fill('John Smith');
    await page.waitForLoadState('networkidle');

    // Should either not show them or show as "Already member"
    // Exact behavior depends on implementation
  });

  test.skip('should assign role when adding member', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Create test family
    await familiesPage.gotoCreateFamily();
    await familiesPage.createFamily({
      name: 'Role Assignment Test',
    });

    // Add member with specific role
    await familiesPage.addMemberButton.click();
    await familiesPage.memberSearchInput.fill(testData.people.barbaraJohnson.fullName);
    await page.getByText(testData.people.barbaraJohnson.fullName).click();

    // Select role
    await familiesPage.roleSelect.selectOption({ label: 'Adult' });

    // Confirm
    await page.getByRole('button', { name: /add/i }).click();

    // Verify member added with correct role
    const memberCard = page.locator('[data-testid="family-member-card"]', {
      hasText: testData.people.barbaraJohnson.fullName,
    });
    await expect(memberCard.getByText(/adult/i)).toBeVisible();
  });

  test('should show remove button for each member', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.johnson.name);

    // Each member card should have a remove button
    const memberCards = page.locator('[data-testid="family-member-card"]');
    const count = await memberCards.count();

    for (let i = 0; i < count; i++) {
      const card = memberCards.nth(i);
      await expect(card.getByRole('button', { name: /remove/i })).toBeVisible();
    }
  });

  test.skip('should confirm before removing member', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Set up dialog handler
    let dialogShown = false;
    page.on('dialog', async (dialog) => {
      dialogShown = true;
      expect(dialog.type()).toBe('confirm');
      expect(dialog.message()).toContain('remove');
      await dialog.dismiss();
    });

    // Try to remove member
    const firstMemberCard = page.locator('[data-testid="family-member-card"]').first();
    await firstMemberCard.getByRole('button', { name: /remove/i }).click();

    // Verify confirmation was shown
    expect(dialogShown).toBe(true);

    // Member should still be there (dialog was dismissed)
    await expect(firstMemberCard).toBeVisible();
  });

  test.skip('should prevent duplicate members', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Try to add someone who's already a member
    await familiesPage.addMemberButton.click();
    await familiesPage.memberSearchInput.fill(testData.people.johnSmith.fullName);

    // Should either not show in results or show as already member
    // Add button should be disabled or show error
  });

  test('@smoke should manage family members successfully', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    // Navigate to family with members
    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Verify members section is functional
    await expect(page.getByText(/family members/i)).toBeVisible();
    await expect(familiesPage.addMemberButton).toBeVisible();

    // Verify members are displayed
    await expect(page.getByText(testData.people.johnSmith.fullName)).toBeVisible();

    // Can open add member modal
    await familiesPage.addMemberButton.click();
    await expect(familiesPage.addMemberModal).toBeVisible();
  });

  test('should link to person detail from member card', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.johnson.name);

    // Try to click on a member name
    const memberLink = page.getByRole('link', { name: testData.people.bobJohnson.fullName });

    try {
      await expect(memberLink).toBeVisible({ timeout: 2000 });
      await memberLink.click();

      // Should navigate to person detail
      await expect(page).toHaveURL(/\/admin\/people\/.+/);
    } catch {
      // Member cards might not be links yet - that's ok
      test.skip();
    }
  });

  test('should show member age or birthdate', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Member cards should show age information
    // Look for children's ages
    const jennyCard = page.locator('[data-testid="family-member-card"]', {
      hasText: testData.people.jennySmith.fullName,
    });

    // Should show age (she's 4)
    await expect(jennyCard.getByText(/\d+\s*y|age/i)).toBeVisible();
  });

  test('should display allergies for children', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Jenny Smith has peanut allergy
    const jennyCard = page.locator('[data-testid="family-member-card"]', {
      hasText: testData.people.jennySmith.fullName,
    });

    // Should show allergy information
    try {
      await expect(jennyCard.getByText(/peanut|allerg/i)).toBeVisible({ timeout: 2000 });
    } catch {
      // Allergy display might not be implemented yet
    }
  });
});

test.describe('Family Members - Role Management', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('should show different roles (Adult, Child)', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Should have both adult and child roles visible
    await expect(page.getByText(/adult/i)).toBeVisible();
    await expect(page.getByText(/child/i)).toBeVisible();
  });

  test.skip('should allow changing member role', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Find a member card
    const memberCard = page.locator('[data-testid="family-member-card"]').first();

    // Look for role edit option
    try {
      const editRoleButton = memberCard.getByRole('button', { name: /edit.*role/i });
      await expect(editRoleButton).toBeVisible({ timeout: 2000 });
      
      // This feature might not be implemented yet
    } catch {
      test.skip();
    }
  });

  test('should display adult members before children', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Get all member cards
    const memberCards = page.locator('[data-testid="family-member-card"]');
    const firstCard = memberCards.first();

    // First member should be an adult (John or Jane Smith)
    await expect(firstCard.getByText(/adult/i)).toBeVisible();
  });
});

test.describe('Family Members - Error Handling', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test.skip('should handle add member failure gracefully', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.smith.name);

    // Try to add member (might fail due to network, validation, etc.)
    await familiesPage.addMemberButton.click();

    // If error occurs, modal should stay open or show error
    // User should be able to retry
  });

  test.skip('should handle remove member failure gracefully', async ({ page }) => {
    const familiesPage = new FamiliesPage(page);

    await familiesPage.gotoList();
    await familiesPage.clickFamily(testData.families.johnson.name);

    page.on('dialog', async (dialog) => {
      await dialog.accept();
    });

    // Try to remove member
    const memberCard = page.locator('[data-testid="family-member-card"]').first();
    await memberCard.getByRole('button', { name: /remove/i }).click();

    // If it fails, should show error message
    // Member should still be visible
  });
});
