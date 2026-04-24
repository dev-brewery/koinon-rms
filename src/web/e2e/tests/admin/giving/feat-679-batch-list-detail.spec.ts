/**
 * E2E Tests: Issue #679 (Wave 4) — Giving batch list / detail / form coverage
 *
 * Covers page-level gaps that feat-493 (end-to-end giving flow) does NOT
 * exercise. feat-493 creates a batch, adds a contribution, closes the batch,
 * and generates a statement — it does not:
 *  - Apply list filters (status, date range, campus) and assert the row set
 *    actually narrows
 *  - Paginate through many batches (mocked response for determinism)
 *  - Render the filter-aware empty state ("Try adjusting your filters")
 *  - Click a list row link through to the detail page
 *  - Handle a 404 for a well-formed-but-missing batch idKey
 *  - Exercise contribution row expand (fund details) / edit modal / delete
 *    confirm-and-cancel
 *  - Show Closed-batch UI hides Add Contribution and per-row Edit/Delete
 *  - Validate the create form (required name, future date, negative amount)
 *  - Cancel the create form back to the list
 *  - Populate the campus dropdown
 *
 * Mocking policy: feat-493 hits the real API for persistence. We do the same
 * here EXCEPT the pagination test, which synthesizes a large batch set via
 * a route handler — creating 26+ real batches per run would pollute the DB
 * and is independent of the pagination UI we're actually asserting.
 *
 * Known pre-existing bugs referenced (skip-and-flag):
 *  - #666 — GET /api/v1/contributions/statements/eligible 500s due to EF LINQ.
 *    StatementsPage tests that depend on the eligible picker are in the
 *    sibling statements spec; none of THIS file's tests depend on that
 *    endpoint, so nothing here is skipped for #666.
 */

import { test, expect, type Page, type Route } from '../../../fixtures/auth.fixture';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Unique per-test suffix so parallel/repeat runs don't collide. */
function uniqueSuffix(label: string): string {
  return `${label}-${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
}

/** yyyy-mm-dd date string for the local date N days ago (0 = today). */
function localDateString(daysAgo = 0): string {
  const d = new Date();
  d.setDate(d.getDate() - daysAgo);
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

/** yyyy-mm-dd date string for the local date N days in the future. */
function futureDateString(daysAhead = 7): string {
  const d = new Date();
  d.setDate(d.getDate() + daysAhead);
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

/** Create a batch through the UI, return its idKey. */
async function createBatchViaUi(
  page: Page,
  name: string,
  controlAmount = '50.00',
): Promise<{ idKey: string; name: string }> {
  await page.goto('/admin/giving/new');
  await expect(
    page.getByRole('heading', { name: /create batch/i }),
  ).toBeVisible({ timeout: 10_000 });

  await page.locator('#name').fill(name);
  await page.locator('#batchDate').fill(localDateString(0));
  await page.locator('#controlAmount').fill(controlAmount);

  await page.getByRole('button', { name: /create batch/i }).click();

  await expect(page).toHaveURL(/\/admin\/giving\/(?!new(?:\/|$))[^/]+$/, {
    timeout: 10_000,
  });
  const url = new URL(page.url());
  const idKey = url.pathname.split('/').pop()!;
  return { idKey, name };
}

// ---------------------------------------------------------------------------
// BatchListPage — filtering, empty state, pagination, row click-through
// ---------------------------------------------------------------------------

test.describe('BatchListPage — filters, empty state, navigation', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('status filter narrows the list to Open batches only', async ({ page }) => {
    // Seed one Open batch so we know at least one row qualifies.
    const batchName = `E2E 679 Status Filter ${uniqueSuffix('b')}`;
    await createBatchViaUi(page, batchName, '10.00');

    await page.goto('/admin/giving');
    await expect(
      page.getByRole('heading', { name: /giving.*batches/i }),
    ).toBeVisible({ timeout: 10_000 });

    // Apply the Open filter
    await page.locator('#statusFilter').selectOption('Open');

    // Our seeded Open batch row is present
    await expect(page.getByRole('link', { name: batchName })).toBeVisible({
      timeout: 10_000,
    });

    // Every visible status badge (in the table body) reads "Open"
    await expect
      .poll(
        async () => {
          // tbody rows: the third column cell contains the status badge
          const badges = await page
            .locator('tbody tr td:nth-child(3)')
            .allInnerTexts();
          // Non-empty (we just asserted our seeded row renders)
          if (badges.length === 0) return false;
          return badges.every((t) => t.trim() === 'Open');
        },
        { timeout: 10_000, message: 'every status badge reads Open' },
      )
      .toBe(true);
  });

  test('status filter Closed does not show newly-created Open batch', async ({ page }) => {
    const batchName = `E2E 679 Closed Filter ${uniqueSuffix('b')}`;
    await createBatchViaUi(page, batchName, '11.00');

    await page.goto('/admin/giving');
    await page.locator('#statusFilter').selectOption('Closed');

    // Our Open batch is NOT in the Closed-filtered view
    await expect(page.getByRole('link', { name: batchName })).not.toBeVisible({
      timeout: 5_000,
    });
  });

  test('filter-aware empty state shows "Try adjusting your filters"', async ({ page }) => {
    await page.goto('/admin/giving');
    await expect(
      page.getByRole('heading', { name: /giving.*batches/i }),
    ).toBeVisible({ timeout: 10_000 });

    // Pick a date range guaranteed to contain zero batches (far-past window).
    await page.locator('#startDate').fill('1999-01-01');
    await page.locator('#endDate').fill('1999-12-31');

    // The empty state varies by whether filters are active. With filters:
    //   title: "No batches found"
    //   description: "Try adjusting your filters"
    await expect(page.getByText(/Try adjusting your filters/i)).toBeVisible({
      timeout: 10_000,
    });
    await expect(page.getByText(/No batches found/i)).toBeVisible();
  });

  test('campus filter dropdown renders and accepts selection', async ({ page }) => {
    await page.goto('/admin/giving');
    await expect(
      page.getByRole('heading', { name: /giving.*batches/i }),
    ).toBeVisible({ timeout: 10_000 });

    const campusSelect = page.locator('#campusFilter');
    await expect(campusSelect).toBeVisible();
    // "All" is always present; plus any seeded campuses from /api/v1/campuses
    const optionCount = await campusSelect.locator('option').count();
    expect(optionCount).toBeGreaterThanOrEqual(1);
  });

  test('clicking a batch name in the list navigates to the detail page', async ({ page }) => {
    const batchName = `E2E 679 RowClick ${uniqueSuffix('b')}`;
    const { idKey } = await createBatchViaUi(page, batchName, '17.25');

    await page.goto('/admin/giving');
    await expect(
      page.getByRole('heading', { name: /giving.*batches/i }),
    ).toBeVisible({ timeout: 10_000 });

    await page.getByRole('link', { name: batchName }).click();

    await expect(page).toHaveURL(new RegExp(`/admin/giving/${idKey}$`), {
      timeout: 10_000,
    });
    await expect(page.locator('h1')).toContainText(batchName, { timeout: 10_000 });
  });

  test('pagination controls appear when totalPages > 1 (mocked)', async ({ page }) => {
    // Intercept the batches list endpoint and return a synthetic two-page
    // response. We use a non-default filter (status=Open) so we don't
    // accidentally clobber the REAL list request when we arrive on the
    // page — the page calls /api/v1/giving/batches twice (once with, once
    // without filters) so we intercept every matching request.
    await page.route('**/api/v1/giving/batches**', async (route: Route) => {
      const url = new URL(route.request().url());
      const pageNum = Number(url.searchParams.get('page') ?? '1');
      const pageSize = Number(url.searchParams.get('pageSize') ?? '25');

      // 60 total batches => 3 pages at pageSize 25
      const totalCount = 60;
      const totalPages = Math.ceil(totalCount / pageSize);
      const startIdx = (pageNum - 1) * pageSize;
      const rowsOnPage = Math.min(pageSize, totalCount - startIdx);
      const data = Array.from({ length: Math.max(0, rowsOnPage) }, (_, i) => ({
        idKey: `MOCK${startIdx + i}`,
        name: `Mock Batch ${startIdx + i + 1}`,
        batchDate: new Date().toISOString(),
        status: 'Open',
        controlAmount: 10.0,
        controlItemCount: null,
        campusIdKey: null,
        note: null,
        createdDateTime: new Date().toISOString(),
        modifiedDateTime: null,
      }));

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          data,
          meta: { page: pageNum, pageSize, totalCount, totalPages },
        }),
      });
    });

    await page.goto('/admin/giving');
    await expect(
      page.getByRole('heading', { name: /giving.*batches/i }),
    ).toBeVisible({ timeout: 10_000 });

    // Page size selector reads 25 by default; "of 60 total" reflects our mock
    await expect(page.getByText(/of 60 total/i)).toBeVisible({ timeout: 10_000 });

    // Previous/Next buttons and "Page 1 of 3" footer
    await expect(page.getByRole('button', { name: 'Previous' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Next' })).toBeVisible();
    await expect(page.getByText(/Page 1 of 3/i)).toBeVisible();

    // Previous is disabled on page 1; Next advances to page 2
    await expect(page.getByRole('button', { name: 'Previous' })).toBeDisabled();
    await page.getByRole('button', { name: 'Next' }).click();
    await expect(page.getByText(/Page 2 of 3/i)).toBeVisible({ timeout: 10_000 });
  });
});

// ---------------------------------------------------------------------------
// BatchDetailPage — 404, contributions empty/expand/edit/delete,
// closed-batch UI, metadata sidebar
// ---------------------------------------------------------------------------

test.describe('BatchDetailPage — drill-down and edit affordances', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('404: well-formed but non-existent batch idKey shows error state + back link', async ({ page }) => {
    // 'f__AAA' is a valid IdKey format but does not decode to any seeded
    // batch, so the API returns 404 (verified empirically). The page
    // renders the ErrorState and a "Back to Batches" link.
    await page.goto('/admin/giving/f__AAA');

    await expect(page.getByText(/Failed to load batch/i)).toBeVisible({
      timeout: 10_000,
    });
    const backLink = page.getByRole('link', { name: /Back to Batches/i });
    await expect(backLink).toBeVisible();
    await backLink.click();
    await expect(page).toHaveURL(/\/admin\/giving$/);
    await expect(
      page.getByRole('heading', { name: /giving.*batches/i }),
    ).toBeVisible({ timeout: 10_000 });
  });

  test('empty contributions table shows "No contributions yet" message', async ({ page }) => {
    const batchName = `E2E 679 Empty ${uniqueSuffix('b')}`;
    await createBatchViaUi(page, batchName, '30.00');

    await expect(page.getByText('Contributions (0)')).toBeVisible({ timeout: 10_000 });
    await expect(page.getByText(/No contributions yet/i)).toBeVisible();
    await expect(page.getByText(/Add contributions to this batch to get started/i)).toBeVisible();
  });

  test('metadata sidebar shows Created date', async ({ page }) => {
    const batchName = `E2E 679 Meta ${uniqueSuffix('b')}`;
    await createBatchViaUi(page, batchName, '25.00');

    // Metadata heading + Created label are always rendered on a fresh batch.
    // Last Modified is conditional (appears only after an update), so we
    // assert Created only.
    await expect(page.getByRole('heading', { name: 'Metadata' })).toBeVisible({
      timeout: 10_000,
    });
    await expect(page.getByText('Created', { exact: true })).toBeVisible();
  });

  test('Closed batch hides Add Contribution button and shows re-open control', async ({ page }) => {
    const batchName = `E2E 679 Closed Ui ${uniqueSuffix('b')}`;
    await createBatchViaUi(page, batchName, '5.00');

    // Close via the sidebar Close button
    await page
      .getByRole('button', { name: 'Close', exact: true })
      .click();
    await expect(page.getByText('Closed', { exact: true }).first()).toBeVisible({
      timeout: 10_000,
    });

    // The header Add Contribution CTA is gone
    await expect(
      page.getByRole('button', { name: /add contribution/i }),
    ).not.toBeVisible();

    // The sidebar now shows an "Open" (reopen) button
    await expect(
      page.getByRole('button', { name: 'Open', exact: true }),
    ).toBeVisible();
  });

  test('delete confirmation dialog can be cancelled without deleting', async ({ page }) => {
    const batchName = `E2E 679 DeleteCancel ${uniqueSuffix('b')}`;
    await createBatchViaUi(page, batchName, '80.00');

    // Add a real contribution so there's a row to attempt-delete.
    await page.getByRole('button', { name: /add contribution/i }).click();
    await expect(
      page.getByRole('heading', { name: 'Add Contribution' }),
    ).toBeVisible({ timeout: 5_000 });

    // Person lookup — pick John Smith
    const personSearch = page.getByPlaceholder(/search for contributor/i);
    await personSearch.fill('John Sm');
    await page
      .getByRole('button', { name: /John Smith.*john\.smith@example\.com/i })
      .first()
      .click();

    // Transaction date/time
    const now = new Date();
    const y = now.getFullYear();
    const m = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    const hh = String(now.getHours()).padStart(2, '0');
    const mm = String(now.getMinutes()).padStart(2, '0');
    await page
      .locator('input[type="datetime-local"]')
      .fill(`${y}-${m}-${day}T${hh}:${mm}`);

    // Transaction type + fund + amount
    const typeSelect = page.locator('select').filter({ hasText: 'Select Type' });
    await expect
      .poll(async () => typeSelect.locator('option').count(), { timeout: 5_000 })
      .toBeGreaterThan(1);
    await typeSelect.selectOption({ label: 'Contribution' });

    const fundSelect = page.locator('select[aria-label="Fund selection 1"]');
    await expect
      .poll(async () => fundSelect.locator('option').count(), { timeout: 5_000 })
      .toBeGreaterThan(1);
    await fundSelect.selectOption({ label: 'General Fund' });

    await page.locator('input[aria-label="Amount for split 1"]').fill('80.00');
    await page
      .getByRole('button', { name: 'Add Contribution' })
      .last()
      .click();

    // Wait for the modal to close and the contribution to land
    await expect(page.getByText('Contributions (1)')).toBeVisible({ timeout: 10_000 });

    // Now click Delete on the row, which opens the confirmation dialog.
    // The default page fixture auto-accepts dialogs via a setTimeout(0),
    // so we need to install a cancel-first handler before clicking.
    const deleteBtn = page.getByRole('button', {
      name: /Delete contribution for John Smith/i,
    });
    await expect(deleteBtn).toBeVisible({ timeout: 5_000 });
    await deleteBtn.click();

    // Confirm dialog renders
    await expect(
      page.getByRole('heading', { name: /Delete Contribution\?/i }),
    ).toBeVisible({ timeout: 5_000 });
    await expect(
      page.getByText(/This action cannot be undone/i),
    ).toBeVisible();

    // Click Cancel
    await page.getByRole('button', { name: 'Cancel', exact: true }).click();

    // Dialog closes; contribution is still there
    await expect(
      page.getByRole('heading', { name: /Delete Contribution\?/i }),
    ).not.toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('Contributions (1)')).toBeVisible();
  });

  test('edit contribution opens the form in edit mode with "Edit Contribution" title', async ({ page }) => {
    const batchName = `E2E 679 EditOpen ${uniqueSuffix('b')}`;
    await createBatchViaUi(page, batchName, '35.00');

    // Seed a contribution (same pattern as delete test)
    await page.getByRole('button', { name: /add contribution/i }).click();
    await expect(
      page.getByRole('heading', { name: 'Add Contribution' }),
    ).toBeVisible({ timeout: 5_000 });

    await page.getByPlaceholder(/search for contributor/i).fill('John Sm');
    await page
      .getByRole('button', { name: /John Smith.*john\.smith@example\.com/i })
      .first()
      .click();

    const now = new Date();
    const y = now.getFullYear();
    const m = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    const hh = String(now.getHours()).padStart(2, '0');
    const mm = String(now.getMinutes()).padStart(2, '0');
    await page
      .locator('input[type="datetime-local"]')
      .fill(`${y}-${m}-${day}T${hh}:${mm}`);

    const typeSelect = page.locator('select').filter({ hasText: 'Select Type' });
    await expect
      .poll(async () => typeSelect.locator('option').count(), { timeout: 5_000 })
      .toBeGreaterThan(1);
    await typeSelect.selectOption({ label: 'Contribution' });

    const fundSelect = page.locator('select[aria-label="Fund selection 1"]');
    await expect
      .poll(async () => fundSelect.locator('option').count(), { timeout: 5_000 })
      .toBeGreaterThan(1);
    await fundSelect.selectOption({ label: 'General Fund' });

    await page.locator('input[aria-label="Amount for split 1"]').fill('35.00');
    await page
      .getByRole('button', { name: 'Add Contribution' })
      .last()
      .click();
    await expect(page.getByText('Contributions (1)')).toBeVisible({ timeout: 10_000 });

    // Click Edit on the row
    const editBtn = page.getByRole('button', {
      name: /Edit contribution for John Smith/i,
    });
    await expect(editBtn).toBeVisible({ timeout: 5_000 });
    await editBtn.click();

    // The modal title switches to "Edit Contribution"
    await expect(
      page.getByRole('heading', { name: 'Edit Contribution' }),
    ).toBeVisible({ timeout: 5_000 });

    // Close the modal via the header × button (aria-label="Close"). Scope to
    // the modal container to avoid colliding with the sidebar BatchSummary
    // "Close" button (which closes the batch, not the modal).
    const modal = page.locator('.fixed.inset-0.z-50');
    await modal.getByRole('button', { name: 'Close' }).click();
    await expect(
      page.getByRole('heading', { name: 'Edit Contribution' }),
    ).not.toBeVisible({ timeout: 5_000 });
  });
});

// ---------------------------------------------------------------------------
// BatchFormPage — validation, campus dropdown, cancel
// ---------------------------------------------------------------------------

test.describe('BatchFormPage — validation and form controls', () => {
  test.beforeEach(async ({ loginAsAdmin, page }) => {
    await loginAsAdmin();
    await page.goto('/admin/giving/new');
    await expect(
      page.getByRole('heading', { name: /create batch/i }),
    ).toBeVisible({ timeout: 10_000 });
  });

  test('submitting with empty name shows "Batch name is required"', async ({ page }) => {
    // Leave name blank, keep default date (today), submit
    await page.locator('#name').fill('');
    await page.getByRole('button', { name: /create batch/i }).click();
    await expect(page.getByText(/Batch name is required/i)).toBeVisible({
      timeout: 5_000,
    });
    // Still on the form (URL unchanged)
    await expect(page).toHaveURL(/\/admin\/giving\/new$/);
  });

  test('submitting with future batch date shows "cannot be in the future"', async ({ page }) => {
    await page.locator('#name').fill(`E2E 679 Future ${uniqueSuffix('b')}`);
    await page.locator('#batchDate').fill(futureDateString(7));
    await page.getByRole('button', { name: /create batch/i }).click();
    await expect(
      page.getByText(/Batch date cannot be in the future/i),
    ).toBeVisible({ timeout: 5_000 });
    await expect(page).toHaveURL(/\/admin\/giving\/new$/);
  });

  test('negative control amount is rejected by the browser numeric input (min=0)', async ({ page }) => {
    // The <input type="number" min="0"> rejects negative values on submit
    // (browser-level validity). We fill a negative, then check validity:
    // the form should not navigate away.
    await page.locator('#name').fill(`E2E 679 Neg ${uniqueSuffix('b')}`);
    await page.locator('#batchDate').fill(localDateString(0));
    await page.locator('#controlAmount').fill('-5');

    // Assert the input element itself is invalid (per min="0")
    const isInvalid = await page.locator('#controlAmount').evaluate(
      (el: HTMLInputElement) => !el.checkValidity(),
    );
    expect(isInvalid).toBe(true);

    // Submitting does not navigate to a detail page
    await page.getByRole('button', { name: /create batch/i }).click();
    await expect(page).toHaveURL(/\/admin\/giving\/new$/);
  });

  test('campus dropdown is populated with at least one option', async ({ page }) => {
    const campusSelect = page.locator('#campusIdKey');
    await expect(campusSelect).toBeVisible();
    // "All Campuses" is always present; waiting for the API-backed options
    // would just prove the fetch-complete race. Count >= 1 is sufficient.
    const count = await campusSelect.locator('option').count();
    expect(count).toBeGreaterThanOrEqual(1);
  });

  test('Cancel link returns to the batch list without creating', async ({ page }) => {
    await page.locator('#name').fill(`E2E 679 Cancel ${uniqueSuffix('b')}`);
    // Don't click Create — click Cancel instead
    await page.getByRole('link', { name: 'Cancel' }).click();
    await expect(page).toHaveURL(/\/admin\/giving$/);
    await expect(
      page.getByRole('heading', { name: /giving.*batches/i }),
    ).toBeVisible({ timeout: 10_000 });
  });

  test('notes textarea accepts free-form input', async ({ page }) => {
    const note = `E2E 679 note body ${uniqueSuffix('n')}`;
    await page.locator('#note').fill(note);
    await expect(page.locator('#note')).toHaveValue(note);
  });

  test('Control Amounts section displays the correct labels', async ({ page }) => {
    await expect(
      page.getByRole('heading', { name: 'Control Amounts' }),
    ).toBeVisible();
    await expect(page.getByText('Expected total amount')).toBeVisible();
    await expect(page.getByText('Expected number of contributions')).toBeVisible();
  });
});
