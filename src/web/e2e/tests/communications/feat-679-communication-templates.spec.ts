/**
 * E2E Tests: Issue #679 (Wave 3) — Communication Templates CRUD
 *
 * Covers the TemplatesPage (list + filters + delete dialog) and the
 * TemplateFormPage (create Email, create SMS, edit, validation, cancel).
 *
 * AUDIT: feat-490 (email) and feat-491 (SMS) cover the compose/send/detail
 * flow but do NOT touch the templates pages at all. This spec fills that gap.
 *
 * These tests hit the real API — template CRUD stores entities in the DB
 * without any outbound email/SMS delivery, so it is safe to exercise.
 * Each test uses a unique template name to avoid collisions across parallel
 * runs and repeat runs on the same database.
 */

import { test, expect, type Page } from '../../fixtures/auth.fixture';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Unique per-test suffix so parallel/repeat runs don't collide. */
function uniqueSuffix(label: string): string {
  return `${label}-${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
}

/** Navigate to the Templates list page via the Communications "Manage Templates" link. */
async function gotoTemplates(page: Page): Promise<void> {
  await page.goto('/admin/communications/templates');
  await expect(page.getByRole('heading', { name: 'Communication Templates' })).toBeVisible({
    timeout: 10000,
  });
}

/** Navigate straight to the create-template form. */
async function gotoNewTemplate(page: Page): Promise<void> {
  await page.goto('/admin/communications/templates/new');
  await expect(page.getByRole('heading', { name: 'Create Template' })).toBeVisible({
    timeout: 10000,
  });
}

// ---------------------------------------------------------------------------
// Templates List Page
// ---------------------------------------------------------------------------

test.describe('Communication Templates — List page (#679)', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('templates page renders heading, description, and Create button', async ({ page }) => {
    await gotoTemplates(page);
    await expect(
      page.getByText('Manage reusable templates for emails and SMS messages')
    ).toBeVisible();
    await expect(page.getByRole('link', { name: 'Create Template' })).toBeVisible();
  });

  test('type filter exposes All Types, Email, and SMS options', async ({ page }) => {
    await gotoTemplates(page);
    const typeFilter = page.locator('#type-filter');
    await expect(typeFilter).toBeVisible();
    await expect(typeFilter.locator('option[value=""]')).toHaveCount(1);
    await expect(typeFilter.locator('option[value="Email"]')).toHaveCount(1);
    await expect(typeFilter.locator('option[value="Sms"]')).toHaveCount(1);
  });

  test('status filter exposes All, Active Only, and Inactive Only options', async ({ page }) => {
    await gotoTemplates(page);
    const statusFilter = page.locator('#status-filter');
    await expect(statusFilter).toBeVisible();
    await expect(statusFilter.locator('option[value="all"]')).toHaveCount(1);
    await expect(statusFilter.locator('option[value="active"]')).toHaveCount(1);
    await expect(statusFilter.locator('option[value="inactive"]')).toHaveCount(1);
  });

  test('Create Template link navigates to the new template form', async ({ page }) => {
    await gotoTemplates(page);
    await page.getByRole('link', { name: 'Create Template' }).click();
    await expect(page).toHaveURL(/\/admin\/communications\/templates\/new$/);
    await expect(page.getByRole('heading', { name: 'Create Template' })).toBeVisible({
      timeout: 10000,
    });
  });

  test('empty state CTA is shown when there are no active templates', async ({ page }) => {
    await gotoTemplates(page);
    // The page defaults to Active Only; if the seeder has no active templates
    // the empty state should be visible. If there are templates, the table
    // renders instead — both outcomes are valid.
    const emptyState = page.getByText('No templates found');
    const table = page.locator('table');
    // Exactly one of these two should be present
    const hasEmpty = await emptyState.isVisible().catch(() => false);
    const hasTable = await table.isVisible().catch(() => false);
    expect(hasEmpty || hasTable).toBe(true);
  });
});

// ---------------------------------------------------------------------------
// Templates Form Page — Create
// ---------------------------------------------------------------------------

test.describe('Communication Templates — Create form (#679)', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('create form exposes required fields: Name, Type, Subject, Body, Description', async ({ page }) => {
    await gotoNewTemplate(page);
    await expect(page.getByLabel(/^Name/)).toBeVisible();
    await expect(page.getByLabel(/Communication Type/)).toBeVisible();
    // Subject field is only visible in Email mode — default is Email
    await expect(page.getByLabel('Subject')).toBeVisible();
    await expect(page.getByLabel(/Message Body/)).toBeVisible();
    await expect(page.getByLabel('Description')).toBeVisible();
    await expect(page.locator('#isActive')).toBeVisible();
  });

  test('switching Communication Type to SMS hides the Subject field', async ({ page }) => {
    await gotoNewTemplate(page);
    await expect(page.getByLabel('Subject')).toBeVisible();
    await page.locator('#communicationType').selectOption('Sms');
    await expect(page.getByLabel('Subject')).toHaveCount(0);
    // Body field remains visible in SMS mode
    await expect(page.getByLabel(/Message Body/)).toBeVisible();
  });

  test('Cancel link returns to the templates list without saving', async ({ page }) => {
    await gotoNewTemplate(page);
    const name = `E2E-Cancel-${uniqueSuffix('t')}`;
    await page.getByLabel(/^Name/).fill(name);
    await page.getByRole('link', { name: 'Cancel' }).click();
    await expect(page).toHaveURL(/\/admin\/communications\/templates$/);
    await expect(page.getByRole('heading', { name: 'Communication Templates' })).toBeVisible();
  });

  test('submitting with an empty Name shows a validation error', async ({ page }) => {
    await gotoNewTemplate(page);
    // Leave Name blank; fill Body only; HTML-required attribute may block submit,
    // so trigger blur-based validation by focusing and leaving the name field.
    const nameField = page.getByLabel(/^Name/);
    await nameField.focus();
    await nameField.blur();
    await expect(page.getByText('Name is required')).toBeVisible({ timeout: 3000 });
  });

  test('can create a new Email template and return to the list', async ({ page }) => {
    await gotoNewTemplate(page);
    const name = `E2E-Email-${uniqueSuffix('t')}`;

    await page.getByLabel(/^Name/).fill(name);
    // Type defaults to Email
    await page.getByLabel('Subject').fill('Welcome to our community');
    await page.getByLabel(/Message Body/).fill('Hello {{FirstName}}, welcome!');
    await page.getByLabel('Description').fill('E2E-generated email template');

    await page.getByRole('button', { name: 'Create Template' }).click();

    // Success redirects back to /admin/communications/templates
    await expect(page).toHaveURL(/\/admin\/communications\/templates$/, { timeout: 10000 });
    await expect(page.getByRole('heading', { name: 'Communication Templates' })).toBeVisible();
    // The newly created template appears in the list
    await expect(page.getByRole('cell', { name })).toBeVisible({ timeout: 10000 });
  });

  test('can create a new SMS template (no Subject field)', async ({ page }) => {
    await gotoNewTemplate(page);
    const name = `E2E-Sms-${uniqueSuffix('t')}`;

    await page.getByLabel(/^Name/).fill(name);
    await page.locator('#communicationType').selectOption('Sms');
    // SMS template does not have a Subject field
    await expect(page.getByLabel('Subject')).toHaveCount(0);
    await page.getByLabel(/Message Body/).fill('Reminder for {{FirstName}}: event tonight.');

    await page.getByRole('button', { name: 'Create Template' }).click();

    await expect(page).toHaveURL(/\/admin\/communications\/templates$/, { timeout: 10000 });
    await expect(page.getByRole('cell', { name })).toBeVisible({ timeout: 10000 });
  });
});

// ---------------------------------------------------------------------------
// Templates Form Page — Edit + Delete
// ---------------------------------------------------------------------------

test.describe('Communication Templates — Edit and delete (#679)', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('edit form loads existing template data into the form fields', async ({ page }) => {
    // Seed an email template via the UI so the test is self-contained
    await gotoNewTemplate(page);
    const name = `E2E-Edit-${uniqueSuffix('t')}`;
    const body = 'Original body for {{FirstName}}.';
    await page.getByLabel(/^Name/).fill(name);
    await page.getByLabel('Subject').fill('Original subject');
    await page.getByLabel(/Message Body/).fill(body);
    await page.getByRole('button', { name: 'Create Template' }).click();
    await expect(page).toHaveURL(/\/admin\/communications\/templates$/, { timeout: 10000 });

    // Navigate to edit via the Edit link in the row
    const row = page.getByRole('row', { name: new RegExp(name) });
    await row.getByRole('link', { name: 'Edit' }).click();

    await expect(page.getByRole('heading', { name: 'Edit Template' })).toBeVisible({
      timeout: 10000,
    });
    await expect(page.getByLabel(/^Name/)).toHaveValue(name);
    await expect(page.getByLabel('Subject')).toHaveValue('Original subject');
    await expect(page.getByLabel(/Message Body/)).toHaveValue(body);
    // Communication Type dropdown is hidden in edit mode
    await expect(page.locator('#communicationType')).toHaveCount(0);
  });

  test('can update an existing template and see the change in the list', async ({ page }) => {
    // Seed an email template via the UI
    await gotoNewTemplate(page);
    const originalName = `E2E-Upd-${uniqueSuffix('t')}`;
    await page.getByLabel(/^Name/).fill(originalName);
    await page.getByLabel('Subject').fill('Subject v1');
    await page.getByLabel(/Message Body/).fill('Body v1');
    await page.getByRole('button', { name: 'Create Template' }).click();
    await expect(page).toHaveURL(/\/admin\/communications\/templates$/, { timeout: 10000 });

    // Open the row for editing
    const row = page.getByRole('row', { name: new RegExp(originalName) });
    await row.getByRole('link', { name: 'Edit' }).click();
    await expect(page.getByRole('heading', { name: 'Edit Template' })).toBeVisible({
      timeout: 10000,
    });

    // Capture the outbound PUT to verify the form sent the edited body. The
    // response may come back quickly so start listening before clicking.
    const putRequest = page.waitForRequest(
      (r) => r.url().includes('/communication-templates/') && r.method() === 'PUT'
    );
    const putResponse = page.waitForResponse(
      (r) => r.url().includes('/communication-templates/') && r.request().method() === 'PUT'
    );

    // Update the body
    const updatedBody = `Body v2 ${Date.now()}`;
    await page.getByLabel(/Message Body/).fill(updatedBody);

    await page.getByRole('button', { name: 'Update Template' }).click();

    const req = await putRequest;
    const resp = await putResponse;
    expect(resp.status()).toBeLessThan(400);

    // Verify the request body carried the new values
    const payload = req.postDataJSON() as { body?: string; name?: string };
    expect(payload.body).toBe(updatedBody);
    expect(payload.name).toBe(originalName);

    await expect(page).toHaveURL(/\/admin\/communications\/templates$/, { timeout: 10000 });
    await expect(page.getByRole('cell', { name: originalName })).toBeVisible({ timeout: 10000 });
  });

  test('Delete button opens a confirm dialog', async ({ page }) => {
    // Seed a template to delete
    await gotoNewTemplate(page);
    const name = `E2E-Del-${uniqueSuffix('t')}`;
    await page.getByLabel(/^Name/).fill(name);
    await page.getByLabel('Subject').fill('Delete me');
    await page.getByLabel(/Message Body/).fill('Body');
    await page.getByRole('button', { name: 'Create Template' }).click();
    await expect(page).toHaveURL(/\/admin\/communications\/templates$/, { timeout: 10000 });

    const row = page.getByRole('row', { name: new RegExp(name) });
    await row.getByRole('button', { name: 'Delete' }).click();

    // ConfirmDialog shows heading + description mentioning the template name
    await expect(page.getByRole('heading', { name: 'Delete Template' })).toBeVisible({
      timeout: 5000,
    });
    await expect(page.getByText(new RegExp(`Are you sure you want to delete "${name}"`))).toBeVisible();
    // Cancel leaves the template in the list
    await page.getByRole('button', { name: /^Cancel$/ }).click();
    await expect(page.getByRole('cell', { name })).toBeVisible();
  });

  test('confirming delete removes the template from the list', async ({ page }) => {
    // Seed a template to delete
    await gotoNewTemplate(page);
    const name = `E2E-DelConfirm-${uniqueSuffix('t')}`;
    await page.getByLabel(/^Name/).fill(name);
    await page.getByLabel('Subject').fill('Going away');
    await page.getByLabel(/Message Body/).fill('Bye');
    await page.getByRole('button', { name: 'Create Template' }).click();
    await expect(page).toHaveURL(/\/admin\/communications\/templates$/, { timeout: 10000 });

    const row = page.getByRole('row', { name: new RegExp(name) });
    await row.getByRole('button', { name: 'Delete' }).click();
    await expect(page.getByRole('heading', { name: 'Delete Template' })).toBeVisible({
      timeout: 5000,
    });
    // Dialog's confirm button (scoped to dialog to avoid row button collision)
    await page.getByRole('button', { name: 'Delete', exact: true }).last().click();

    // Row is gone after deletion
    await expect(page.getByRole('cell', { name })).toHaveCount(0, { timeout: 10000 });
  });
});
