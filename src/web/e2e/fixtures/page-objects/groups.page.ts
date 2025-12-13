/**
 * Groups Page Object Model
 * 
 * NOTE: This page object uses aspirational selectors. When the groups UI is
 * implemented, update locators to use data-testid attributes instead of:
 * - CSS classes (.bg-white, .text-red-600)
 * - Positional selectors (.first(), .nth(1))
 * - Parent traversal (..)
 * 
 * See Issue #182 for tracking.
 */
import { type Page, type Locator, expect } from '@playwright/test';

/**
 * Page Object for Groups pages
 * Covers tree view, detail view, form, and member management
 */
export class GroupsPage {
  readonly page: Page;

  // Tree view elements
  readonly searchInput: Locator;
  readonly createGroupButton: Locator;
  readonly treeViewButton: Locator;
  readonly listViewButton: Locator;
  readonly groupTreeItems: Locator;
  readonly groupListItems: Locator;

  // Detail page elements
  readonly groupName: Locator;
  readonly groupType: Locator;
  readonly editButton: Locator;
  readonly deleteButton: Locator;
  readonly addChildGroupButton: Locator;
  readonly childGroupsList: Locator;
  readonly membersSection: Locator;

  // Form elements
  readonly nameInput: Locator;
  readonly descriptionInput: Locator;
  readonly groupTypeSelect: Locator;
  readonly parentGroupSelect: Locator;
  readonly campusSelect: Locator;
  readonly capacityInput: Locator;
  readonly isActiveCheckbox: Locator;
  readonly submitButton: Locator;
  readonly cancelButton: Locator;
  readonly validationError: Locator;

  // Member management elements
  readonly addMemberButton: Locator;
  readonly removeMemberButton: Locator;
  readonly memberCards: Locator;
  readonly memberSearchInput: Locator;

  // Common elements
  readonly loadingSpinner: Locator;
  readonly errorMessage: Locator;
  readonly successMessage: Locator;
  readonly deleteConfirmModal: Locator;
  readonly deleteConfirmButton: Locator;
  readonly deleteCancelButton: Locator;

  constructor(page: Page) {
    this.page = page;

    // Tree view
    this.searchInput = page.getByPlaceholder(/search groups/i);
    this.createGroupButton = page.getByRole('link', { name: /create group/i });
    this.treeViewButton = page.locator('button').filter({ has: page.locator('svg') }).first();
    this.listViewButton = page.locator('button').filter({ has: page.locator('svg') }).nth(1);
    this.groupTreeItems = page.locator('[data-testid="group-tree-item"]');
    this.groupListItems = page.locator('tbody tr');

    // Detail page
    this.groupName = page.locator('h1');
    this.groupType = page.locator('h1 + p');
    this.editButton = page.getByRole('link', { name: /edit/i });
    this.deleteButton = page.getByRole('button', { name: /delete/i });
    this.addChildGroupButton = page.getByRole('link', { name: /add child group/i });
    this.childGroupsList = page.getByText(/child groups/i).locator('..').locator('..');
    this.membersSection = page.getByText(/members/i).locator('..').locator('..');

    // Form
    this.nameInput = page.getByLabel(/^name/i);
    this.descriptionInput = page.getByLabel(/description/i);
    this.groupTypeSelect = page.getByLabel(/group type/i);
    this.parentGroupSelect = page.getByLabel(/parent group/i);
    this.campusSelect = page.getByLabel(/campus/i);
    this.capacityInput = page.getByLabel(/capacity/i);
    this.isActiveCheckbox = page.getByLabel(/active/i);
    this.submitButton = page.getByRole('button', { name: /(create|update) group/i });
    this.cancelButton = page.getByRole('link', { name: /cancel/i });
    this.validationError = page.locator('.text-red-600');

    // Member management
    this.addMemberButton = page.getByRole('button', { name: /add member/i });
    this.removeMemberButton = page.getByRole('button', { name: /remove/i });
    this.memberCards = page.locator('[data-testid="member-card"]');
    this.memberSearchInput = page.getByPlaceholder(/search.*member/i);

    // Common
    this.loadingSpinner = page.getByTestId('loading-spinner');
    this.errorMessage = page.getByRole('alert');
    this.successMessage = page.getByTestId('success-message');
    this.deleteConfirmModal = page.locator('.fixed.inset-0');
    this.deleteConfirmButton = page.getByRole('button', { name: /^delete$/i });
    this.deleteCancelButton = this.deleteConfirmModal.getByRole('button', { name: /cancel/i });
  }

  async gotoTreeView() {
    await this.page.goto('/admin/groups');
    await this.waitForLoad();
  }

  async gotoGroupDetail(idKey: string) {
    await this.page.goto(`/admin/groups/${idKey}`);
    await this.waitForLoad();
  }

  async gotoCreateGroup(parentId?: string) {
    const url = parentId
      ? `/admin/groups/new?parentId=${parentId}`
      : '/admin/groups/new';
    await this.page.goto(url);
  }

  async gotoEditGroup(idKey: string) {
    await this.page.goto(`/admin/groups/${idKey}/edit`);
  }

  async waitForLoad() {
    try {
      await expect(this.loadingSpinner).not.toBeVisible({ timeout: 10000 });
    } catch {
      // Spinner may not appear for fast loads
    }
  }

  async searchGroups(query: string) {
    await this.searchInput.fill(query);
    // Wait for search to take effect (debouncing)
    await this.page.waitForTimeout(500);
  }

  async switchToTreeView() {
    await this.treeViewButton.click();
  }

  async switchToListView() {
    await this.listViewButton.click();
  }

  async clickGroup(groupName: string) {
    await this.page.getByText(groupName).first().click();
  }

  async createGroup(data: {
    name: string;
    description?: string;
    groupType: string;
    parentGroup?: string;
    campus?: string;
    capacity?: number;
    isActive?: boolean;
  }) {
    await this.nameInput.fill(data.name);

    if (data.description) {
      await this.descriptionInput.fill(data.description);
    }

    if (data.groupType) {
      await this.groupTypeSelect.selectOption({ label: data.groupType });
    }

    if (data.parentGroup) {
      await this.parentGroupSelect.selectOption({ label: data.parentGroup });
    }

    if (data.campus) {
      await this.campusSelect.selectOption({ label: data.campus });
    }

    if (data.capacity !== undefined) {
      await this.capacityInput.fill(data.capacity.toString());
    }

    if (data.isActive !== undefined && !data.isActive) {
      await this.isActiveCheckbox.uncheck();
    }

    await this.submitButton.click();
  }

  async updateGroup(data: {
    name?: string;
    description?: string;
    capacity?: number;
    isActive?: boolean;
  }) {
    if (data.name) {
      await this.nameInput.fill(data.name);
    }

    if (data.description !== undefined) {
      await this.descriptionInput.fill(data.description);
    }

    if (data.capacity !== undefined) {
      await this.capacityInput.fill(data.capacity.toString());
    }

    if (data.isActive !== undefined) {
      if (data.isActive) {
        await this.isActiveCheckbox.check();
      } else {
        await this.isActiveCheckbox.uncheck();
      }
    }

    await this.submitButton.click();
  }

  async deleteGroup() {
    await this.deleteButton.click();
    await expect(this.deleteConfirmModal).toBeVisible();
    await this.deleteConfirmButton.click();
  }

  async cancelDelete() {
    await this.deleteButton.click();
    await expect(this.deleteConfirmModal).toBeVisible();
    await this.deleteCancelButton.click();
  }

  async expectGroupVisible(groupName: string) {
    await expect(this.page.getByText(groupName).first()).toBeVisible();
  }

  async expectGroupNotVisible(groupName: string) {
    await expect(this.page.getByText(groupName).first()).not.toBeVisible();
  }

  async expectOnDetailPage(groupName: string) {
    await expect(this.groupName).toContainText(groupName);
  }

  async expectValidationError(field: string, message?: string) {
    const error = this.page.locator(`#${field}`).locator('..').locator('.text-red-600');
    await expect(error).toBeVisible();
    if (message) {
      await expect(error).toContainText(message);
    }
  }

  async expectChildGroupCount(count: number) {
    const childGroupsText = await this.childGroupsList.getByText(/child groups/i).textContent();
    expect(childGroupsText).toContain(count.toString());
  }
}
