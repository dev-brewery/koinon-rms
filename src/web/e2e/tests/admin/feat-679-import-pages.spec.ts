/**
 * E2E Tests: Issue #679 (Wave 6) — Import admin pages coverage
 *
 * Prior coverage audit:
 *  - No dedicated spec for ImportHistoryPage, PeopleImportPage, or
 *    FamiliesImportPage existed. Grep for `import/history`, `import/people`,
 *    `import/families` in e2e/tests/** returned no prior spec hits beyond
 *    this new file.
 *  - `navigation/admin-navigation.spec.ts` does not cover the Import links.
 *  - `settings/import` (ImportSettingsPage) is a separate page and is out of
 *    scope for wave 6.
 *
 * This spec covers the three admin import pages at:
 *  - /admin/import/history
 *  - /admin/import/people
 *  - /admin/import/families
 *
 * Coverage:
 *  - ImportHistoryPage: header, filter control, navigation CTAs to People
 *    and Families import, empty / populated state renders without error,
 *    Refresh action is wired.
 *  - PeopleImportPage: header + breadcrumb, 4-step progress indicator,
 *    upload step (CSV drop zone), uploading a tiny in-memory CSV advances
 *    to the mapping step, "Validate & Continue" guard requires mapping a
 *    required target field, Back returns to upload.
 *  - FamiliesImportPage: header + 4-step progress indicator, upload step,
 *    uploading a tiny in-memory CSV advances to mapping.
 *
 * Mocking policy:
 *  - Real API. The backend accepts CSV upload for preview parsing on the
 *    client-side (no API call) so the upload→mapping transition does not
 *    hit the server.
 *  - Validation (`Validate & Continue`) and Execute are NOT invoked — they
 *    spin up background jobs and are outside the scope of this wave.
 *  - A tiny CSV (3 data rows) is uploaded via page.setInputFiles using a
 *    Buffer — no fixture file is written to disk.
 *
 * Fixed in #696: IDataImportService is now registered in DI, so
 *   GET /api/v1/import/jobs returns 200 (no longer the DI 500).
 *
 * SECOND BUG (not fixed in #696, flagged separately): the backend returns
 *   `{ data: PagedResult<ImportJobDto> }` (i.e. `{ data: { items, totalCount, … } }`)
 *   from GET /api/v1/import/jobs, but the frontend client
 *   `getImportJobs()` in `src/web/src/services/api/import.ts` unwraps it as
 *   `{ data: ImportJobDto[] }`. The mismatched shape causes the hook's
 *   `data.filter()` call to throw and the page renders the global
 *   ErrorState. We mock `**\/api/v1/import/jobs**` with an array-shaped
 *   response so the page chrome tests can still run; filing this as a
 *   follow-up issue (title suggestion: "fix(web): align getImportJobs
 *   unwrapping with PagedResult<ImportJobDto> shape").
 *
 * Guardrails:
 *  - No data-testid attributes added to production code.
 *  - No edits to page-objects, fixtures, seeder, or auth helpers.
 */

import { test, expect, type Page } from '../../fixtures/auth.fixture';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function uniqueSuffix(label: string): string {
  return `${label}-${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
}

/**
 * Builds a small CSV buffer for upload.
 * Kept tiny (3 data rows) so parsing is instant and nothing is persisted.
 */
function buildCsv(headers: string[], rows: string[][]): Buffer {
  const lines = [headers.join(','), ...rows.map((r) => r.join(','))];
  return Buffer.from(lines.join('\n'), 'utf-8');
}

/**
 * Sets a CSV file on the CsvUploader's hidden file input and waits for the
 * mapping step to render.
 */
async function uploadCsv(page: Page, fileName: string, content: Buffer): Promise<void> {
  await page.getByLabel(/csv file upload/i).setInputFiles({
    name: fileName,
    mimeType: 'text/csv',
    buffer: content,
  });
  // After a successful parse, CsvUploader calls onFileSelected which flips
  // the wizard into the "mapping" step. The FieldMappingEditor renders a
  // "Field Mapping" heading or similar structure; we wait on the visible
  // "Validate & Continue" button that only exists on the mapping step.
  await expect(
    page.getByRole('button', { name: /validate & continue/i }),
  ).toBeVisible({ timeout: 10_000 });
}

// ---------------------------------------------------------------------------
// ImportHistoryPage
//
// DI resolution for /import/jobs is fixed in #696 (endpoint no longer
// returns 500). A separate shape mismatch between the controller's
// PagedResult payload and the frontend's array-expectation remains open
// (see SECOND BUG in the header). Until that is resolved, we mock the
// jobs list with the shape the frontend currently expects so the page
// chrome tests can exercise heading, filter, CTAs, and refresh.
// ---------------------------------------------------------------------------

test.describe('Import History page', () => {
  test.beforeEach(async ({ loginAsAdmin, page }) => {
    await loginAsAdmin();
    // Shape matches what getImportJobs() unwraps today: { data: ImportJobDto[] }.
    // This mock is temporary; remove once the backend/frontend contract is
    // aligned (see SECOND BUG in the spec header).
    await page.route('**/api/v1/import/jobs**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ data: [] }),
      });
    });
    await page.goto('/admin/import/history');
  });

  test('renders page heading and description', async ({ page }) => {
    await expect(page.getByRole('heading', { name: /import history/i })).toBeVisible();
    await expect(
      page.getByText(/view past import jobs and download error reports/i),
    ).toBeVisible();
  });

  test('exposes navigation CTAs to People and Families import', async ({ page }) => {
    const peopleLink = page.getByRole('link', { name: /import people/i });
    const familiesLink = page.getByRole('link', { name: /import families/i });
    await expect(peopleLink).toBeVisible();
    await expect(familiesLink).toBeVisible();
    await expect(peopleLink).toHaveAttribute('href', '/admin/import/people');
    await expect(familiesLink).toHaveAttribute('href', '/admin/import/families');
  });

  test('type filter exposes all expected options', async ({ page }) => {
    const filter = page.getByLabel(/filter by type/i);
    await expect(filter).toBeVisible();
    const options = filter.locator('option');
    // Page renders five fixed options.
    await expect(options).toHaveCount(5);
    await expect(options.nth(0)).toHaveText(/all types/i);
    await expect(options.nth(1)).toHaveText('People');
    await expect(options.nth(2)).toHaveText('Families');
  });

  test('changing the type filter shows the empty state for the chosen type', async ({ page }) => {
    const filter = page.getByLabel(/filter by type/i);
    await filter.selectOption('People');
    await expect(page).toHaveURL(/\/admin\/import\/history/);
    await expect(page.getByText(/no people imports found/i)).toBeVisible({ timeout: 10_000 });
  });

  test('Refresh button is present and clickable', async ({ page }) => {
    const refresh = page.getByRole('button', { name: /^refresh$/i });
    await expect(refresh).toBeVisible();
    await refresh.click();
    // We do not assert a specific server response, only that the page did
    // not crash and the refresh button stays visible.
    await expect(refresh).toBeVisible();
  });

  test('clicking "Import People" CTA navigates to the people import wizard', async ({ page }) => {
    await page.getByRole('link', { name: /import people/i }).click();
    await expect(page).toHaveURL('/admin/import/people');
    await expect(page.getByRole('heading', { name: /import people/i })).toBeVisible();
  });
});

// ---------------------------------------------------------------------------
// PeopleImportPage
// ---------------------------------------------------------------------------

test.describe('People Import wizard', () => {
  test.beforeEach(async ({ loginAsAdmin, page }) => {
    await loginAsAdmin();
    await page.goto('/admin/import/people');
  });

  test('renders header and four-step progress indicator', async ({ page }) => {
    await expect(page.getByRole('heading', { name: /import people/i })).toBeVisible();
    await expect(page.getByText(/1\. upload csv/i)).toBeVisible();
    await expect(page.getByText(/2\. map fields/i)).toBeVisible();
    await expect(page.getByText(/3\. validate/i)).toBeVisible();
    await expect(page.getByText(/4\. import/i)).toBeVisible();
  });

  test('upload step shows the CSV drop zone', async ({ page }) => {
    await expect(
      page.getByText(/drag and drop a csv file here, or click to select/i),
    ).toBeVisible();
    await expect(page.getByLabel(/csv file upload/i)).toBeAttached();
  });

  test('uploading a tiny CSV advances the wizard to the mapping step', async ({ page }) => {
    const fileName = `${uniqueSuffix('people-wave6')}.csv`;
    const csv = buildCsv(
      ['FirstName', 'LastName', 'Email'],
      [
        ['Ada', 'Lovelace', 'ada@example.com'],
        ['Alan', 'Turing', 'alan@example.com'],
        ['Grace', 'Hopper', 'grace@example.com'],
      ],
    );

    await uploadCsv(page, fileName, csv);

    // Mapping step is now visible.
    await expect(page.getByRole('button', { name: /validate & continue/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /^back$/i })).toBeVisible();
  });

  test('"Validate & Continue" is disabled until required fields are mapped', async ({ page }) => {
    // Upload a CSV with headers that do NOT auto-map to required fields
    // (FirstName / LastName / BirthDate are required for People).
    const csv = buildCsv(
      ['AlphaCol', 'BetaCol', 'GammaCol'],
      [
        ['x', 'y', 'z'],
      ],
    );
    await uploadCsv(page, 'people-no-map.csv', csv);

    await expect(
      page.getByRole('button', { name: /validate & continue/i }),
    ).toBeDisabled();
  });

  test('Back button returns to the upload step after upload', async ({ page }) => {
    const csv = buildCsv(
      ['FirstName', 'LastName'],
      [['Ada', 'Lovelace']],
    );
    await uploadCsv(page, 'people-back.csv', csv);

    await page.getByRole('button', { name: /^back$/i }).click();

    // Upload step is restored (drop zone text is visible again).
    await expect(
      page.getByText(/drag and drop a csv file here, or click to select/i),
    ).toBeVisible();
  });
});

// ---------------------------------------------------------------------------
// FamiliesImportPage
// ---------------------------------------------------------------------------

test.describe('Families Import wizard', () => {
  test.beforeEach(async ({ loginAsAdmin, page }) => {
    await loginAsAdmin();
    await page.goto('/admin/import/families');
  });

  test('renders header and four-step progress indicator', async ({ page }) => {
    await expect(page.getByRole('heading', { name: /import families/i })).toBeVisible();
    await expect(page.getByText(/1\. upload csv/i)).toBeVisible();
    await expect(page.getByText(/2\. map fields/i)).toBeVisible();
    await expect(page.getByText(/3\. validate/i)).toBeVisible();
    await expect(page.getByText(/4\. import/i)).toBeVisible();
  });

  test('upload step shows the CSV drop zone', async ({ page }) => {
    await expect(
      page.getByText(/drag and drop a csv file here, or click to select/i),
    ).toBeVisible();
    await expect(page.getByLabel(/csv file upload/i)).toBeAttached();
  });

  test('uploading a tiny CSV advances the wizard to the mapping step', async ({ page }) => {
    const fileName = `${uniqueSuffix('families-wave6')}.csv`;
    const csv = buildCsv(
      ['FamilyName', 'Email', 'City'],
      [
        ['Lovelace Family', 'lovelace@example.com', 'London'],
        ['Turing Family', 'turing@example.com', 'Manchester'],
        ['Hopper Family', 'hopper@example.com', 'New York'],
      ],
    );

    await uploadCsv(page, fileName, csv);

    await expect(page.getByRole('button', { name: /validate & continue/i })).toBeVisible();
  });
});
