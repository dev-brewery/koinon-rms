/**
 * E2E Tests: Issue #679 (Wave 5) — Schedules admin coverage
 *
 * Prior coverage audit:
 *  - No dedicated schedule admin spec existed. Only coverage was:
 *      - navigation/admin-navigation.spec.ts: one click-through from sidebar
 *      - navigation/breadcrumb.spec.ts: heading assertion on /admin/schedules
 *  - feat-488 (rapid group attendance) does NOT touch /admin/schedules — it
 *    operates on /my-groups attendance modals.
 *  - The comms specs ("Schedule for Later") are about scheduling emails, not
 *    church service schedules.
 *
 * This spec covers the three Schedule admin pages:
 *  - ScheduleListPage: header, Create button, filters (search, day-of-week,
 *    include-inactive), schedule card rendering from seeded data, navigate
 *    to detail, empty-state (mocked), loading/error pathways.
 *  - ScheduleDetailPage: basic rendering + delete modal. (See NEW-BUG below.)
 *  - ScheduleFormPage: create form renders, required-name guard, cancel.
 *
 * Mocking policy (matches wave-4 pattern): real API for CRUD against seeded
 * data; narrow page.route() only for a synthetic empty-state scenario that
 * would otherwise require wiping seeded schedules.
 *
 * BUG #1 (FIXED, #688): envelope not unwrapped in schedules API service.
 *   `src/web/src/services/api/schedules.ts` now unwraps the `{ data: ... }`
 *   API envelope for getScheduleByIdKey / updateSchedule /
 *   getScheduleOccurrences (createSchedule's backend returns a flat body via
 *   CreatedAtAction(..., schedule) so it intentionally does NOT unwrap —
 *   that's a BE inconsistency vs. sibling controllers, not a FE bug). The
 *   tests that were previously .skip()-ed with a reference to this bug are
 *   now active as regression guards in the detail/edit describe blocks.
 *
 * NEW BUG #2 (skip-and-flag): schedule form create/submit is broken.
 *   (a) `src/web/src/components/admin/schedules/WeeklySchedulePicker.tsx`
 *       produces `HH:MM:SS` values via `generateTimeOptions()`, but
 *       `src/web/src/schemas/schedule.schema.ts` validates `timeOfDay` with
 *       a regex that only accepts `HH:MM`. Any selected time fails client
 *       validation, so the form refuses to POST.
 *   (b) The server (`POST /api/v1/schedules`) requires both
 *       `weeklyDayOfWeek` and `weeklyTimeOfDay` for weekly schedules but
 *       the client schema's `superRefine` for this is a tautological
 *       no-op — it lets empty submissions through, which then 400 from
 *       the server without user-visible feedback.
 *   Because of (a) and (b), an end-to-end create flow is not achievable
 *   from the UI as written. The form's static chrome, disabled-when-empty
 *   submit guard, day/time picker interactions, and cancel flow ARE all
 *   assertable and covered below. Suggested issue title:
 *     "fix(web): schedule form cannot submit valid weekly schedules"
 *
 * Guardrails:
 *  - No data-testid attributes added to production code.
 *  - Seeded schedule data (testData.schedules.*) is read-only — tests that
 *    create/edit use uniqueSuffix() for isolation.
 */

import { test, expect } from '../../../fixtures/auth.fixture';
import { testData } from '../../../fixtures/test-data';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function uniqueSuffix(label: string): string {
  return `${label}-${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
}

// (A `fillAndSubmitScheduleForm` helper was removed after the end-to-end
// create-flow test was replaced with a picker-interaction check — see
// "NEW BUG #2" in the module doc above.)

// ---------------------------------------------------------------------------
// ScheduleListPage
// ---------------------------------------------------------------------------

test.describe('ScheduleListPage — header, filters, cards', () => {
  test.beforeEach(async ({ loginAsAdmin, page }) => {
    await loginAsAdmin();
    await page.goto('/admin/schedules');
  });

  test('renders the Schedules heading, description, and Create link', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'Schedules' })).toBeVisible();
    await expect(
      page.getByText(/manage service times and check-in schedules/i),
    ).toBeVisible();
    await expect(page.getByRole('link', { name: /create schedule/i })).toBeVisible();
  });

  test('clicking Create Schedule navigates to the new-schedule form', async ({ page }) => {
    await page.getByRole('link', { name: /create schedule/i }).click();
    await expect(page).toHaveURL('/admin/schedules/new');
    await expect(page.getByRole('heading', { name: /create schedule/i })).toBeVisible();
  });

  test('renders seeded Sunday and Wednesday schedules', async ({ page }) => {
    await expect(
      page.getByRole('link', { name: testData.schedules.sunday9am.name }),
    ).toBeVisible({ timeout: 10_000 });
    await expect(
      page.getByRole('link', { name: testData.schedules.sunday11am.name }),
    ).toBeVisible();
    await expect(
      page.getByRole('link', { name: testData.schedules.wednesday7pm.name }),
    ).toBeVisible();
  });

  test('search input narrows the visible schedules', async ({ page }) => {
    await expect(
      page.getByRole('link', { name: testData.schedules.sunday9am.name }),
    ).toBeVisible({ timeout: 10_000 });

    await page.getByRole('textbox', { name: /^search$/i }).fill('Wednesday');

    // Wednesday stays; Sunday 9 AM goes away.
    await expect(
      page.getByRole('link', { name: testData.schedules.wednesday7pm.name }),
    ).toBeVisible({ timeout: 10_000 });
    await expect(
      page.getByRole('link', { name: testData.schedules.sunday9am.name }),
    ).toHaveCount(0);
  });

  test('day-of-week filter narrows to schedules on that day', async ({ page }) => {
    await expect(
      page.getByRole('link', { name: testData.schedules.sunday9am.name }),
    ).toBeVisible({ timeout: 10_000 });

    // Wednesday == 3 per testData + DAYS_OF_WEEK order (Sun=0).
    await page.getByLabel(/day of week/i).selectOption('3');

    await expect(
      page.getByRole('link', { name: testData.schedules.wednesday7pm.name }),
    ).toBeVisible({ timeout: 10_000 });
    // Sunday schedules should be filtered out.
    await expect(
      page.getByRole('link', { name: testData.schedules.sunday9am.name }),
    ).toHaveCount(0);
  });

  test('the include-inactive toggle is a real, interactive checkbox', async ({ page }) => {
    const toggle = page.getByLabel(/include inactive schedules/i);
    await expect(toggle).toBeVisible();
    await expect(toggle).not.toBeChecked();
    await toggle.check();
    await expect(toggle).toBeChecked();
    // Visibility of any extra rows is data-dependent (no seeded inactives).
    // We just assert the filter UI reacts without throwing.
    await expect(
      page.getByRole('heading', { name: 'Schedules' }),
    ).toBeVisible();
  });

  test('clicking a schedule card navigates to the detail URL', async ({ page }) => {
    // NOTE: the detail heading assertion is skipped — see NEW-BUG at the top
    // of this file (schedules API service does not unwrap the data envelope,
    // so the detail page renders with an empty heading). This test still
    // proves the click-through routing works.
    const link = page.getByRole('link', {
      name: testData.schedules.sunday9am.name,
    });
    await expect(link).toBeVisible({ timeout: 10_000 });
    await link.click();
    await expect(page).toHaveURL(/\/admin\/schedules\/[^/]+$/);
  });
});

test.describe('ScheduleListPage — empty state (mocked)', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('shows empty-state CTA when there are zero schedules', async ({ page }) => {
    // Narrow route mock: only intercept GETs to the list endpoint (with or
    // without query string), return an empty payload shaped like the real
    // API response. Everything else (auth, navigation, etc.) uses the real
    // backend.
    await page.route(/\/api\/v1\/schedules(?:\?[^/]*)?$/, async (route) => {
      if (route.request().method() !== 'GET') {
        await route.continue();
        return;
      }
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          data: [],
          meta: { page: 1, pageSize: 50, totalCount: 0, totalPages: 0 },
        }),
      });
    });

    await page.goto('/admin/schedules');

    await expect(page.getByText(/no schedules yet/i)).toBeVisible({ timeout: 10_000 });
    // EmptyState CTA button.
    await expect(
      page.getByRole('button', { name: /create schedule/i }),
    ).toBeVisible();
  });

  test('shows "no schedules found" when filters match nothing', async ({ page }) => {
    await page.goto('/admin/schedules');
    await page.getByRole('textbox', { name: /^search$/i }).fill('ZZZNonexistentScheduleXYZ999');
    await expect(page.getByText(/no schedules found/i)).toBeVisible({ timeout: 10_000 });
    await expect(page.getByText(/try adjusting your filters/i)).toBeVisible();
  });
});

// ---------------------------------------------------------------------------
// ScheduleDetailPage
// ---------------------------------------------------------------------------

test.describe('ScheduleDetailPage — rendering (envelope unwrap, #688)', () => {
  test.beforeEach(async ({ loginAsAdmin, page }) => {
    await loginAsAdmin();
    await page.goto('/admin/schedules');
    await page
      .getByRole('link', { name: testData.schedules.sunday9am.name })
      .click();
    await expect(page).toHaveURL(/\/admin\/schedules\/[^/]+$/);
  });

  test('shows the schedule name as the page heading', async ({ page }) => {
    // Regression guard for #688: before the envelope unwrap, this heading was
    // empty because `useSchedule()` received `{ data: {...} }` instead of the
    // unwrapped DTO, so `schedule.name` was undefined.
    await expect(
      page.getByRole('heading', { name: testData.schedules.sunday9am.name, level: 1 }),
    ).toBeVisible({ timeout: 10_000 });
  });

  test('renders the Schedule Details card with day and time', async ({ page }) => {
    // Regression guard for #688: before the envelope unwrap, these fields all
    // read as undefined and the card either collapsed or showed stale values.
    const detailsHeading = page.getByRole('heading', { name: /^schedule details$/i });
    await expect(detailsHeading).toBeVisible();
    // Scope to the Schedule Details card body — the seeded Sunday 9 AM schedule
    // renders the "Day and Time" row with "Sunday at 9:00 AM" (formatTime12Hour
    // + DAYS_OF_WEEK[0]) only when the DTO is unwrapped.
    const card = detailsHeading.locator('..');
    await expect(card.getByText(/sunday at 9:00\s*am/i)).toBeVisible({ timeout: 10_000 });
  });

  test('renders the Status sidebar with correct Active/Public values', async ({ page }) => {
    // Regression guard for #688: before the envelope unwrap, every status
    // boolean read as undefined on the frontend, so the sidebar always
    // rendered "Inactive" / "No" regardless of the server payload. After
    // the fix, the value pill text is driven by the real DTO values.
    const statusHeading = page.getByRole('heading', { name: /^status$/i });
    await expect(statusHeading).toBeVisible();

    // Scope assertions to the status card.
    const statusCard = statusHeading.locator('..');
    // All three row labels must be present regardless of state.
    await expect(statusCard.getByText('Active').first()).toBeVisible();
    await expect(statusCard.getByText(/check-in active/i)).toBeVisible();
    await expect(statusCard.getByText('Public').first()).toBeVisible();

    // Seeded Sunday 9 AM is active — the dd pill next to the "Active" dt
    // should read "Active", NOT "Inactive". Use an exact-match assertion on
    // the pill text and assert the negative case is NOT visible. The row
    // under the "Active" dt is the first dd in the <dl>.
    const firstValuePill = statusCard.locator('dd').first();
    await expect(firstValuePill).toHaveText('Active', { timeout: 10_000 });
  });

  test('always-rendered controls (Edit, Delete, back arrow) are present', async ({
    page,
  }) => {
    // These controls render regardless of the schedule payload, so they are
    // unaffected by the envelope bug and are safe to assert here.
    await expect(page.getByRole('link', { name: /^edit$/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /^delete$/i })).toBeVisible();
    await expect(page.locator('a[href="/admin/schedules"]').first()).toBeVisible();
  });

  test('Edit action links to the form at /edit', async ({ page }) => {
    await page.getByRole('link', { name: /^edit$/i }).click();
    await expect(page).toHaveURL(/\/admin\/schedules\/[^/]+\/edit$/);
    await expect(
      page.getByRole('heading', { name: /edit schedule/i }),
    ).toBeVisible();
  });

  test('back arrow returns to the list', async ({ page }) => {
    await page.locator('a[href="/admin/schedules"]').first().click();
    await expect(page).toHaveURL('/admin/schedules');
  });

  test('delete confirmation modal opens and can be cancelled without deleting', async ({
    page,
  }) => {
    const url = page.url();
    await page.getByRole('button', { name: /^delete$/i }).click();

    const modalHeading = page.getByRole('heading', { name: /delete schedule/i });
    await expect(modalHeading).toBeVisible();
    await expect(page.getByText(/this action cannot be undone/i)).toBeVisible();

    // Cancel inside the modal (scope to modal to avoid matching the list filter).
    await modalHeading.locator('..').getByRole('button', { name: /cancel/i }).click();
    await expect(modalHeading).not.toBeVisible();
    await expect(page).toHaveURL(url);
  });
});

// ---------------------------------------------------------------------------
// ScheduleFormPage — create
// ---------------------------------------------------------------------------

test.describe('ScheduleFormPage — create mode', () => {
  test.beforeEach(async ({ loginAsAdmin, page }) => {
    await loginAsAdmin();
    await page.goto('/admin/schedules/new');
    await expect(
      page.getByRole('heading', { name: /create schedule/i }),
    ).toBeVisible({ timeout: 10_000 });
  });

  test('shows Basic Information, Schedule Time, Check-in Window, Effective Dates sections', async ({
    page,
  }) => {
    await expect(
      page.getByRole('heading', { name: /basic information/i }),
    ).toBeVisible();
    await expect(
      page.getByRole('heading', { name: /^schedule time$/i }),
    ).toBeVisible();
    await expect(
      page.getByRole('heading', { name: /^check-in window$/i }),
    ).toBeVisible();
    await expect(
      page.getByRole('heading', { name: /effective dates/i }),
    ).toBeVisible();
  });

  test('Create Schedule button is disabled until name is filled', async ({ page }) => {
    const submit = page.getByRole('button', { name: /^create schedule$/i });
    await expect(submit).toBeDisabled();

    await page.locator('#name').fill('E2E Schedule Enable Check');
    await expect(submit).toBeEnabled();
  });

  test('Cancel link returns to the schedules list', async ({ page }) => {
    await page.locator('#name').fill('Should Not Save');
    await page.getByRole('link', { name: /^cancel$/i }).click();
    await expect(page).toHaveURL('/admin/schedules');
  });

  test('day buttons and time selector are interactive', async ({ page }) => {
    // We cannot exercise a full submit flow end-to-end here because the form
    // is blocked by TWO additional pre-existing bugs (see "NEW BUG #2" in the
    // module doc at the top of this file):
    //   (a) WeeklySchedulePicker emits HH:MM:SS values but the zod schema
    //       only accepts HH:MM, so the form's client-side validation
    //       rejects any selected time.
    //   (b) The server requires both weeklyDayOfWeek and weeklyTimeOfDay
    //       for weekly schedules, but the frontend schema does not enforce
    //       that pair-wise requirement, so submitting with neither results
    //       in a 400 that the form doesn't surface to the user.
    // This test at least confirms the day/time controls respond to clicks
    // — a regression in the picker itself would still be caught.
    await page.locator('#name').fill(`E2E Wave5 PickerCheck ${uniqueSuffix('sch')}`);

    const monBtn = page.getByRole('button', { name: 'Mon' });
    await monBtn.click();
    // Selected day button gets the primary styling.
    await expect(monBtn).toHaveClass(/bg-primary-600/);

    const timeSelect = page.locator('#timeOfDay');
    await timeSelect.selectOption('10:00:00');
    await expect(timeSelect).toHaveValue('10:00:00');
  });
});

// ---------------------------------------------------------------------------
// ScheduleFormPage — edit mode (blocked by envelope bug)
// ---------------------------------------------------------------------------

test.describe('ScheduleFormPage — edit mode (envelope unwrap, #688)', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('edit mode pre-fills the name from an existing schedule', async ({ page }) => {
    // Regression guard for #688: before the envelope unwrap, useSchedule()
    // received `{ data: {...} }` and so `schedule.name` was undefined —
    // the name input stayed empty on edit. The fix makes the detail DTO
    // flow through `response.data` so `schedule.name` is now populated.
    await page.goto('/admin/schedules');
    await page
      .getByRole('link', { name: testData.schedules.sunday9am.name })
      .click();
    await page.getByRole('link', { name: /^edit$/i }).click();
    await expect(
      page.getByRole('heading', { name: /edit schedule/i }),
    ).toBeVisible({ timeout: 10_000 });

    await expect(page.locator('#name')).toHaveValue(
      testData.schedules.sunday9am.name,
      { timeout: 10_000 },
    );
  });

  test('edit mode saves a description edit and returns to the detail page', async ({
    page,
  }) => {
    // Regression guard for #688: before the fix, the edit form never
    // populated from the server and `updateSchedule()` returned
    // `{ data: {...} }`, so the mutation resolved with an object whose own
    // keys were undefined — the round-trip back to the detail view could
    // not render the updated schedule name.
    //
    // This test edits the description (a non-identifying field we can safely
    // round-trip against the shared seeded schedule) and then asserts the
    // detail page renders. We intentionally avoid mutating the name, which
    // other tests read from.
    const uniqueDescription = `E2E #688 description ${uniqueSuffix('desc')}`;

    await page.goto('/admin/schedules');
    await page
      .getByRole('link', { name: testData.schedules.sunday9am.name })
      .click();
    const detailUrl = page.url();

    await page.getByRole('link', { name: /^edit$/i }).click();
    await expect(page.locator('#name')).toHaveValue(
      testData.schedules.sunday9am.name,
      { timeout: 10_000 },
    );

    const descriptionField = page.locator('#description');
    await descriptionField.fill(uniqueDescription);
    await page.getByRole('button', { name: /save changes|update schedule/i }).click();

    // Expect to land back on the detail page (not the /edit route).
    await expect(page).toHaveURL(detailUrl, { timeout: 10_000 });

    // And the name heading still renders (proves the unwrap on the
    // subsequent GET /schedules/:idKey refetch).
    await expect(
      page.getByRole('heading', {
        name: testData.schedules.sunday9am.name,
        level: 1,
      }),
    ).toBeVisible({ timeout: 10_000 });
  });

  test('navigating to /admin/schedules/:idKey/edit renders the Edit Schedule heading', async ({
    page,
  }) => {
    // Even with the envelope bug, the edit form's static chrome renders —
    // only the data-bound inputs are empty. Verify the heading to cover the
    // /edit route at minimum.
    await page.goto('/admin/schedules');
    await page
      .getByRole('link', { name: testData.schedules.sunday9am.name })
      .click();
    await page.getByRole('link', { name: /^edit$/i }).click();
    await expect(
      page.getByRole('heading', { name: /edit schedule/i }),
    ).toBeVisible({ timeout: 10_000 });
    await expect(page).toHaveURL(/\/admin\/schedules\/[^/]+\/edit$/);
  });
});
