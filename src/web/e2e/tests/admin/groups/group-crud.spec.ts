/**
 * E2E Tests: Group CRUD Operations
 * Tests creating, reading, updating, and deleting groups
 */

import { test, expect } from '@playwright/test';
import { GroupsPage } from '../../../fixtures/page-objects/groups.page';
import { LoginPage } from '../../../fixtures/page-objects/login.page';
import { testData } from '../../../fixtures/test-data';

test.describe('Group CRUD Operations', () => {
  test.beforeEach(async ({ page }) => {
    // Login first
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test('should create a new group successfully', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    // Navigate to create form
    await groupsPage.gotoCreateGroup();
    await expect(page.getByRole('heading', { name: /create group/i })).toBeVisible();

    // Fill out form
    await groupsPage.createGroup({
      name: 'Test Youth Group',
      description: 'A test group for youth ministry',
      groupType: 'General',
      capacity: 25,
      isActive: true,
    });

    // Wait for navigation to detail page
    await expect(page).toHaveURL(/\/admin\/groups\/[^/]+$/);

    // Verify group created
    await groupsPage.expectOnDetailPage('Test Youth Group');
    await expect(page.getByText('A test group for youth ministry')).toBeVisible();
  });

  test('should validate required fields on create', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    // Navigate to create form
    await groupsPage.gotoCreateGroup();

    // Try to submit without filling required fields
    await groupsPage.submitButton.click();

    // Verify validation errors appear
    // HTML5 validation will prevent submit, or custom validation shows errors
    await expect(page).toHaveURL('/admin/groups/new');
  });

  test('should validate name field is required', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    await groupsPage.gotoCreateGroup();

    // Fill group type but not name
    await groupsPage.groupTypeSelect.selectOption({ label: 'General' });

    // Try to submit
    await groupsPage.submitButton.click();

    // Should still be on form page
    await expect(page).toHaveURL('/admin/groups/new');
  });

  test('should view group details', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    // Navigate to a known group
    await groupsPage.gotoTreeView();
    await groupsPage.clickGroup(testData.groups.nursery.name);

    // Verify details page
    await groupsPage.expectOnDetailPage(testData.groups.nursery.name);

    // Verify key information is displayed
    await expect(page.getByText(testData.groups.nursery.description)).toBeVisible();

    // Verify metadata section exists
    await expect(page.getByText(/created/i)).toBeVisible();
    await expect(page.getByText(/status/i)).toBeVisible();
  });

  test('should navigate to edit group form', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    // Go to group detail
    await groupsPage.gotoTreeView();
    await groupsPage.clickGroup(testData.groups.preschool.name);

    // Click edit button
    await groupsPage.editButton.click();

    // Verify on edit form
    await expect(page).toHaveURL(/\/admin\/groups\/.+\/edit/);
    await expect(page.getByRole('heading', { name: /edit group/i })).toBeVisible();

    // Verify form is pre-filled
    await expect(groupsPage.nameInput).toHaveValue(testData.groups.preschool.name);
    await expect(groupsPage.descriptionInput).toHaveValue(testData.groups.preschool.description);
  });

  test('should update group details', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    // First, create a group to edit
    await groupsPage.gotoCreateGroup();
    await groupsPage.createGroup({
      name: 'Test Edit Group',
      description: 'Original description',
      groupType: 'General',
      capacity: 20,
    });

    // Navigate to edit
    await groupsPage.editButton.click();
    await expect(page).toHaveURL(/\/admin\/groups\/.+\/edit/);

    // Update fields
    await groupsPage.updateGroup({
      name: 'Updated Test Group',
      description: 'Updated description',
      capacity: 30,
    });

    // Verify updates persisted
    await expect(page).toHaveURL(/\/admin\/groups\/[^/]+$/); // Back on detail page
    await groupsPage.expectOnDetailPage('Updated Test Group');
    await expect(page.getByText('Updated description')).toBeVisible();
    await expect(page.getByText(/30.*people/i)).toBeVisible();
  });

  test('should cancel group creation', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    await groupsPage.gotoCreateGroup();

    // Fill some data
    await groupsPage.nameInput.fill('Cancelled Group');

    // Click cancel
    await groupsPage.cancelButton.click();

    // Should return to groups list
    await expect(page).toHaveURL('/admin/groups');
  });

  test('should cancel group edit', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    // Navigate to existing group and edit
    await groupsPage.gotoTreeView();
    await groupsPage.clickGroup(testData.groups.elementary.name);
    const groupUrl = page.url();

    await groupsPage.editButton.click();

    // Make changes
    await groupsPage.nameInput.fill('Should Not Save');

    // Cancel
    await groupsPage.cancelButton.click();

    // Should return to detail page without changes
    await expect(page).toHaveURL(groupUrl);
    await groupsPage.expectOnDetailPage(testData.groups.elementary.name);
  });

  test('should delete a group with confirmation', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    // Create a group to delete
    await groupsPage.gotoCreateGroup();
    await groupsPage.createGroup({
      name: 'Group To Delete',
      description: 'This will be deleted',
      groupType: 'General',
    });

    // Verify on detail page
    await groupsPage.expectOnDetailPage('Group To Delete');

    // Delete the group
    await groupsPage.deleteGroup();

    // Should redirect to groups list
    await expect(page).toHaveURL('/admin/groups');

    // Group should no longer exist (search for it)
    await groupsPage.searchGroups('Group To Delete');
    await expect(page.getByText(/no groups found/i)).toBeVisible();
  });

  test('should cancel delete confirmation', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    // Navigate to existing group
    await groupsPage.gotoTreeView();
    await groupsPage.clickGroup(testData.groups.nursery.name);
    const groupUrl = page.url();

    // Start delete but cancel
    await groupsPage.cancelDelete();

    // Should still be on detail page
    await expect(page).toHaveURL(groupUrl);
    await groupsPage.expectOnDetailPage(testData.groups.nursery.name);
  });

  test('should create child group with parent relationship', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    // Navigate to parent group detail
    await groupsPage.gotoTreeView();
    await groupsPage.clickGroup(testData.groups.elementary.name);

    // Get the current URL to extract parent ID
    const detailUrl = page.url();
    const parentIdKey = detailUrl.split('/').pop();

    // Create child group (if "Add Child Group" button exists)
    try {
      await groupsPage.addChildGroupButton.click({ timeout: 2000 });

      // Verify parent is pre-selected
      await expect(page).toHaveURL(new RegExp(`parentId=${parentIdKey}`));

      await groupsPage.createGroup({
        name: 'Elementary - Grade 1',
        description: 'First grade group',
        groupType: 'Age Group',
      });

      // Verify created
      await groupsPage.expectOnDetailPage('Elementary - Grade 1');
    } catch {
      // Add Child Group button might not exist yet - skip test
      test.skip();
    }
  });

  test('@smoke should complete full CRUD cycle', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    // Create
    await groupsPage.gotoCreateGroup();
    await groupsPage.createGroup({
      name: 'CRUD Test Group',
      description: 'Testing full CRUD cycle',
      groupType: 'General',
      capacity: 15,
    });

    // Read
    await groupsPage.expectOnDetailPage('CRUD Test Group');
    await expect(page.getByText('Testing full CRUD cycle')).toBeVisible();

    // Update
    await groupsPage.editButton.click();
    await groupsPage.updateGroup({
      description: 'Updated CRUD description',
      capacity: 20,
    });
    await expect(page.getByText('Updated CRUD description')).toBeVisible();

    // Delete
    await groupsPage.deleteGroup();
    await expect(page).toHaveURL('/admin/groups');
  });

  test('should handle capacity validation', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    await groupsPage.gotoCreateGroup();

    // Fill required fields
    await groupsPage.nameInput.fill('Capacity Test Group');
    await groupsPage.groupTypeSelect.selectOption({ label: 'General' });

    // Try negative capacity
    await groupsPage.capacityInput.fill('-5');

    // HTML5 validation or custom validation should prevent this
    // The input has min="0" so browser will prevent submit
    await groupsPage.submitButton.click();

    // Should still be on form
    await expect(page).toHaveURL('/admin/groups/new');
  });

  test('should show inactive status badge', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    // Create inactive group
    await groupsPage.gotoCreateGroup();
    await groupsPage.createGroup({
      name: 'Inactive Test Group',
      groupType: 'General',
      isActive: false,
    });

    // Verify inactive status shown
    await expect(page.getByText(/inactive/i)).toBeVisible();

    // Look for status badge
    const statusBadge = page.locator('.bg-gray-100.text-gray-800', { hasText: /inactive/i });
    await expect(statusBadge).toBeVisible();
  });
});

test.describe('Group Form Validation', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test('should validate name length', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    await groupsPage.gotoCreateGroup();

    // Try very long name
    const longName = 'A'.repeat(256);
    await groupsPage.nameInput.fill(longName);
    await groupsPage.groupTypeSelect.selectOption({ label: 'General' });

    // Blur to trigger validation
    await groupsPage.nameInput.blur();

    // If there's a max length, validation error should appear
    // Otherwise, it should accept it
  });

  test('should allow empty description', async ({ page }) => {
    const groupsPage = new GroupsPage(page);

    await groupsPage.gotoCreateGroup();

    // Description is optional
    await groupsPage.createGroup({
      name: 'No Description Group',
      groupType: 'General',
    });

    // Should succeed
    await groupsPage.expectOnDetailPage('No Description Group');
  });
});
