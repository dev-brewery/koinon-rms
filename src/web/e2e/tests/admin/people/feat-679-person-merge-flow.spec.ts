/**
 * E2E Tests: Person Merge Flow (Issue #679, wave 1)
 *
 * Covers the merge flow pages that are not exercised by any other admin spec:
 *  - DuplicateReviewPage   (src/pages/admin/people/DuplicateReviewPage.tsx)
 *  - MergeHistoryPage      (src/pages/admin/people/MergeHistoryPage.tsx)
 *  - PersonMergeWizard     (src/pages/admin/people/PersonMergePage.tsx -> PersonMergeWizard)
 *  - PersonComparisonPage  (src/pages/admin/people/PersonComparisonPage.tsx)
 *
 * Coverage strategy notes:
 *  - The seed data does not contain a pre-ingested duplicate pair, so duplicate-
 *    review assertions focus on page structure + CTA navigation + the empty
 *    state. We do NOT force the API to produce duplicates.
 *  - Merge-history starts empty on a fresh seed; after the golden-path merge
 *    test in this file runs once, the history page will no longer be empty.
 *    The empty-state test therefore relies on being run against a clean DB.
 *    If the DB already has merges, it still passes because we scope the
 *    assertion to "either empty state OR the merge history table is present"
 *    using a tolerant check.
 *  - Wizard tests create two persons through the UI with uniqueSuffix() so
 *    each run gets its own scoped pair.
 *  - Selectors follow the pattern already in admin/people/*.spec.ts: role,
 *    heading text, button names, and labels. No data-testid attributes are
 *    added to production pages (per task constraints).
 */

import { test, expect, type Page } from '../../../fixtures/auth.fixture';
import { PeoplePage } from '../../../fixtures/page-objects/people.page';

/** Unique per-test suffix so parallel/repeat runs don't collide. */
function uniqueSuffix(label: string): string {
  return `${label}-${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
}

/**
 * Creates a person via the already-covered PersonFormPage UI and returns
 * the assigned idKey (extracted from the detail-page URL).
 */
async function createPersonViaUi(
  page: Page,
  firstName: string,
  lastName: string,
  email?: string,
): Promise<{ idKey: string; firstName: string; lastName: string }> {
  const peoplePage = new PeoplePage(page);
  await peoplePage.gotoCreatePerson();
  await peoplePage.createPerson({
    firstName,
    lastName,
    email,
  });

  // After successful create the app navigates to /admin/people/<idKey>.
  // Explicitly wait until the URL is NOT /admin/people/new anymore — the
  // create form lives at /new and /new satisfies a naive /[^/]+$/ regex.
  await page.waitForURL(
    (url) => /\/admin\/people\/[^/]+$/.test(url.pathname)
      && !url.pathname.endsWith('/admin/people/new'),
    { timeout: 15_000 },
  );
  const url = new URL(page.url());
  const idKey = url.pathname.split('/').pop() || '';
  expect(idKey.length).toBeGreaterThan(0);
  expect(idKey).not.toBe('new');
  return { idKey, firstName, lastName };
}

// ---------------------------------------------------------------------------
// DuplicateReviewPage
// ---------------------------------------------------------------------------

test.describe('DuplicateReviewPage', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('renders heading, description, and View Merge History CTA', async ({ page }) => {
    await page.goto('/admin/people/duplicates');

    await expect(
      page.getByRole('heading', { name: 'Duplicate Review' }),
    ).toBeVisible();
    await expect(
      page.getByText(/review and merge potential duplicate people/i),
    ).toBeVisible();
    await expect(
      page.getByRole('button', { name: 'View Merge History' }),
    ).toBeVisible();
  });

  test('View Merge History link navigates to merge-history route', async ({ page }) => {
    await page.goto('/admin/people/duplicates');
    await page.getByRole('button', { name: 'View Merge History' }).click();
    await expect(page).toHaveURL('/admin/people/merge-history');
    await expect(
      page.getByRole('heading', { name: 'Merge History' }),
    ).toBeVisible();
  });

  test('shows empty-state guidance when no duplicates are seeded', async ({ page }) => {
    await page.goto('/admin/people/duplicates');
    // Wait for either the empty state OR the duplicate list to render.
    const emptyHeading = page.getByRole('heading', { name: 'No duplicates found' });
    const duplicateList = page.getByRole('button', { name: 'Merge' });
    await expect(emptyHeading.or(duplicateList).first()).toBeVisible();

    // On the demo seed we expect empty; if the demo seed ever has duplicates,
    // we still verify guidance text is present for the empty case by falling
    // through to the duplicate list assertion above.
    if (await emptyHeading.isVisible()) {
      await expect(
        page.getByText(/all people records are unique/i),
      ).toBeVisible();
    }
  });
});

// ---------------------------------------------------------------------------
// MergeHistoryPage
// ---------------------------------------------------------------------------

test.describe('MergeHistoryPage', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('renders heading, description, and Review Duplicates CTA', async ({ page }) => {
    await page.goto('/admin/people/merge-history');

    await expect(
      page.getByRole('heading', { name: 'Merge History' }),
    ).toBeVisible();
    await expect(
      page.getByText(/historical record of completed person merges/i),
    ).toBeVisible();
    await expect(
      page.getByRole('button', { name: 'Review Duplicates' }),
    ).toBeVisible();
  });

  test('Review Duplicates CTA navigates back to duplicate review page', async ({ page }) => {
    await page.goto('/admin/people/merge-history');
    await page.getByRole('button', { name: 'Review Duplicates' }).click();
    await expect(page).toHaveURL('/admin/people/duplicates');
    await expect(
      page.getByRole('heading', { name: 'Duplicate Review' }),
    ).toBeVisible();
  });

  test('renders either empty state or history table', async ({ page }) => {
    await page.goto('/admin/people/merge-history');

    // Empty-state on fresh seed; after any merge runs in this file the table
    // will have rows. Accept either.
    const emptyHeading = page.getByRole('heading', { name: 'No merge history' });
    const historyTable = page.getByRole('columnheader', { name: 'Survivor Person' });
    await expect(emptyHeading.or(historyTable).first()).toBeVisible();
  });
});

// ---------------------------------------------------------------------------
// PersonComparisonPage
// ---------------------------------------------------------------------------

test.describe('PersonComparisonPage', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('shows missing-ids warning when query params are absent', async ({ page }) => {
    await page.goto('/admin/people/compare');

    await expect(
      page.getByRole('heading', { name: 'Person Comparison' }),
    ).toBeVisible();
    await expect(
      page.getByText(/missing person ids/i),
    ).toBeVisible();
  });

  test('renders both person cards side-by-side with query params', async ({ page }) => {
    const suffix = uniqueSuffix('cmp');
    const p1 = await createPersonViaUi(page, `CompareA${suffix}`, 'Tester');
    const p2 = await createPersonViaUi(page, `CompareB${suffix}`, 'Tester');

    await page.goto(
      `/admin/people/compare?person1=${p1.idKey}&person2=${p2.idKey}`,
    );

    await expect(
      page.getByRole('heading', { name: 'Person Comparison' }),
    ).toBeVisible();
    // Both person headers present (first and last name render together).
    await expect(
      page.getByRole('heading', { name: `${p1.firstName} ${p1.lastName}` }),
    ).toBeVisible();
    await expect(
      page.getByRole('heading', { name: `${p2.firstName} ${p2.lastName}` }),
    ).toBeVisible();
    // Field comparison section renders.
    await expect(
      page.getByRole('heading', { name: 'Field Comparison' }),
    ).toBeVisible();
    // Action buttons present.
    await expect(
      page.getByRole('button', { name: 'Merge These People' }),
    ).toBeVisible();
  });
});

// ---------------------------------------------------------------------------
// PersonMergeWizard
// ---------------------------------------------------------------------------

test.describe('PersonMergeWizard', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('shows missing-ids warning when query params are absent', async ({ page }) => {
    await page.goto('/admin/people/merge');

    await expect(
      page.getByRole('heading', { name: 'Merge People' }),
    ).toBeVisible();
    await expect(
      page.getByText(/missing person ids/i),
    ).toBeVisible();
  });

  test('step 1 renders both person cards and step indicator', async ({ page }) => {
    const suffix = uniqueSuffix('w1');
    const p1 = await createPersonViaUi(page, `MergeA${suffix}`, 'Tester');
    const p2 = await createPersonViaUi(page, `MergeB${suffix}`, 'Tester');

    await page.goto(
      `/admin/people/merge?person1=${p1.idKey}&person2=${p2.idKey}`,
    );

    await expect(
      page.getByRole('heading', { name: 'Merge People' }),
    ).toBeVisible();
    await expect(
      page.getByRole('heading', { name: 'Select Survivor' }),
    ).toBeVisible();
    // Step indicator labels render (these are <span>s in the step indicator
    // bar; the h2 heading "Select Survivor" also exists so disambiguate by
    // filtering the locator to the span role).
    await expect(
      page.locator('span').filter({ hasText: /^Select Survivor$/ }),
    ).toBeVisible();
    await expect(
      page.locator('span').filter({ hasText: /^Choose Fields$/ }),
    ).toBeVisible();
    await expect(
      page.locator('span').filter({ hasText: /^Preview$/ }),
    ).toBeVisible();
    await expect(
      page.locator('span').filter({ hasText: /^Confirm$/ }),
    ).toBeVisible();
    // Both person cards show up (names match both the step-1 card and the
    // step-indicator would be different — names only appear in the survivor
    // selection cards).
    await expect(
      page.getByRole('heading', { name: `${p1.firstName} ${p1.lastName}` }),
    ).toBeVisible();
    await expect(
      page.getByRole('heading', { name: `${p2.firstName} ${p2.lastName}` }),
    ).toBeVisible();
  });

  test('step 1 Next advances to step 2 once a survivor is picked', async ({ page }) => {
    const suffix = uniqueSuffix('w2');
    const p1 = await createPersonViaUi(page, `MergeA${suffix}`, 'Tester');
    const p2 = await createPersonViaUi(page, `MergeB${suffix}`, 'Tester');

    await page.goto(
      `/admin/people/merge?person1=${p1.idKey}&person2=${p2.idKey}`,
    );

    // Pick person 1 as survivor by clicking the card (button role).
    await page
      .getByRole('button', { name: new RegExp(`${p1.firstName} ${p1.lastName}`) })
      .click();

    await page.getByRole('button', { name: 'Next' }).click();

    // Now on step 2 (Choose Fields).
    await expect(
      page.getByRole('heading', { name: 'Choose Fields' }),
    ).toBeVisible();
    // Back button appears on steps >= 2.
    await expect(page.getByRole('button', { name: 'Back' })).toBeVisible();
  });

  test('Back button returns from step 2 to step 1', async ({ page }) => {
    const suffix = uniqueSuffix('w3');
    const p1 = await createPersonViaUi(page, `MergeA${suffix}`, 'Tester');
    const p2 = await createPersonViaUi(page, `MergeB${suffix}`, 'Tester');

    await page.goto(
      `/admin/people/merge?person1=${p1.idKey}&person2=${p2.idKey}`,
    );

    await page
      .getByRole('button', { name: new RegExp(`${p1.firstName} ${p1.lastName}`) })
      .click();
    await page.getByRole('button', { name: 'Next' }).click();
    await expect(
      page.getByRole('heading', { name: 'Choose Fields' }),
    ).toBeVisible();

    await page.getByRole('button', { name: 'Back' }).click();
    await expect(
      page.getByRole('heading', { name: 'Select Survivor' }),
    ).toBeVisible();
  });

  test('advancing through Preview renders preview card with merged name', async ({ page }) => {
    const suffix = uniqueSuffix('w4');
    const p1 = await createPersonViaUi(page, `MergeA${suffix}`, 'Tester');
    const p2 = await createPersonViaUi(page, `MergeB${suffix}`, 'Tester');

    await page.goto(
      `/admin/people/merge?person1=${p1.idKey}&person2=${p2.idKey}`,
    );

    await page
      .getByRole('button', { name: new RegExp(`${p1.firstName} ${p1.lastName}`) })
      .click();
    await page.getByRole('button', { name: 'Next' }).click(); // -> choose-fields
    await page.getByRole('button', { name: 'Next' }).click(); // -> preview

    await expect(
      page.getByRole('heading', { name: 'Preview Merged Person' }),
    ).toBeVisible();
    // Survivor's name appears on the preview card.
    await expect(
      page.getByRole('heading', { name: `${p1.firstName} ${p1.lastName}` }),
    ).toBeVisible();
    // Summary bullet block for records-to-be-updated is present.
    await expect(page.getByText(/will be transferred to survivor/i).first()).toBeVisible();
  });

  test('step 4 Confirm button is disabled until confirmation checkbox is checked', async ({ page }) => {
    const suffix = uniqueSuffix('w5');
    const p1 = await createPersonViaUi(page, `MergeA${suffix}`, 'Tester');
    const p2 = await createPersonViaUi(page, `MergeB${suffix}`, 'Tester');

    await page.goto(
      `/admin/people/merge?person1=${p1.idKey}&person2=${p2.idKey}`,
    );

    await page
      .getByRole('button', { name: new RegExp(`${p1.firstName} ${p1.lastName}`) })
      .click();
    await page.getByRole('button', { name: 'Next' }).click();
    await page.getByRole('button', { name: 'Next' }).click();
    await page.getByRole('button', { name: 'Next' }).click();

    await expect(
      page.getByRole('heading', { name: 'Confirm Merge' }),
    ).toBeVisible();

    const confirmButton = page.getByRole('button', { name: 'Confirm Merge' });
    await expect(confirmButton).toBeDisabled();

    await page
      .getByRole('checkbox', {
        name: /i understand that this action cannot be undone/i,
      })
      .check();

    await expect(confirmButton).toBeEnabled();
  });

  test('Cancel button on wizard returns to duplicate review page', async ({ page }) => {
    const suffix = uniqueSuffix('w6');
    const p1 = await createPersonViaUi(page, `MergeA${suffix}`, 'Tester');
    const p2 = await createPersonViaUi(page, `MergeB${suffix}`, 'Tester');

    await page.goto(
      `/admin/people/merge?person1=${p1.idKey}&person2=${p2.idKey}`,
    );

    await page.getByRole('button', { name: 'Cancel' }).click();
    await expect(page).toHaveURL('/admin/people/duplicates');
    await expect(
      page.getByRole('heading', { name: 'Duplicate Review' }),
    ).toBeVisible();
  });

  test('@smoke golden-path: completes full merge and redirects to survivor detail', async ({ page }) => {
    const suffix = uniqueSuffix('golden');
    const survivorFirst = `Survivor${suffix}`;
    const mergedFirst = `Merged${suffix}`;
    const p1 = await createPersonViaUi(page, survivorFirst, 'Tester');
    const p2 = await createPersonViaUi(page, mergedFirst, 'Tester');

    await page.goto(
      `/admin/people/merge?person1=${p1.idKey}&person2=${p2.idKey}`,
    );

    // Step 1: pick p1 as survivor.
    await page
      .getByRole('button', { name: new RegExp(`${p1.firstName} ${p1.lastName}`) })
      .click();
    await page.getByRole('button', { name: 'Next' }).click();

    // Step 2: accept default field selections (survivor).
    await expect(
      page.getByRole('heading', { name: 'Choose Fields' }),
    ).toBeVisible();
    await page.getByRole('button', { name: 'Next' }).click();

    // Step 3: preview.
    await expect(
      page.getByRole('heading', { name: 'Preview Merged Person' }),
    ).toBeVisible();
    await page.getByRole('button', { name: 'Next' }).click();

    // Step 4: check confirmation and submit.
    await expect(
      page.getByRole('heading', { name: 'Confirm Merge' }),
    ).toBeVisible();
    await page
      .getByRole('checkbox', {
        name: /i understand that this action cannot be undone/i,
      })
      .check();

    await page.getByRole('button', { name: 'Confirm Merge' }).click();

    // Success redirects to survivor's detail page. Toast appears with the
    // "People merged successfully" message.
    await page.waitForURL(`**/admin/people/${p1.idKey}`, { timeout: 20_000 });
    await expect(page).toHaveURL(new RegExp(`/admin/people/${p1.idKey}$`));
    // Survivor's name is shown on the detail page (as the h1).
    await expect(
      page.getByRole('heading', { name: new RegExp(`${survivorFirst}`) }),
    ).toBeVisible();

    // The non-survivor should no longer appear in the active people list.
    // Search by unique first name we created; no row should match.
    const peoplePage = new PeoplePage(page);
    await peoplePage.gotoList();
    await peoplePage.searchPeople(mergedFirst);
    // Either the empty-state appears, or the merged person is explicitly
    // absent. The search input debounces, so wait for network idle happened
    // inside searchPeople().
    await expect(
      page.getByText(mergedFirst, { exact: false }),
    ).toHaveCount(0);
  });
});
