/**
 * E2E Tests: Kiosk Family Registration Flow (Issue #679, wave 2)
 *
 * Covers the first-time-family registration wizard reached from the kiosk
 * check-in screen when a phone/name search returns no results. This is the
 * "Not Found? Register New Family" path driven by:
 *   src/pages/CheckinPage.tsx           (search → register step transition)
 *   src/components/checkin/KioskFamilyRegistration.tsx (3-step wizard)
 *
 * Wizard steps exercised:
 *   parent → children → review → submit
 *
 * API endpoints mocked (same pattern as checkin-complete-flow.spec.ts):
 *   GET  /api/v1/checkin/configuration
 *   POST /api/v1/checkin/search              (empty result → triggers register CTA)
 *   POST /api/v1/checkin/register-family     (final submit)
 *   GET  /api/v1/checkin/opportunities/:id   (post-register auto-advance)
 *
 * Coverage strategy notes:
 *  - All API calls are mocked, so no DB writes happen. Tests are re-run-safe
 *    and never leave permanent data behind.
 *  - uniqueSuffix() scopes family/child names per test run even though the
 *    backend is mocked, to keep the spec robust if a future reviewer swaps
 *    to live-server mode.
 *  - No data-testid attributes are added to production; selectors use
 *    role/label/text patterns consistent with the surrounding admin specs.
 */

import { test, expect, type Page } from '@playwright/test';
import { CheckinPage } from '../../fixtures/page-objects/checkin.page';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Unique per-test suffix so parallel/repeat runs don't collide on mock matches. */
function uniqueSuffix(label: string): string {
  return `${label}-${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
}

/**
 * Build a `/checkin/register-family` response in the backend DTO shape.
 * The frontend (mapSearchResultToFamily) reads familyIdKey/familyName/members
 * and maps members via personIdKey, so we must return those exact field names.
 */
function buildRegisteredFamily(parentLastName: string, childFirstName: string) {
  const familyIdKey = 'fam_new_' + Date.now();
  const personIdKey = 'per_child_' + Date.now();
  return {
    data: {
      familyIdKey,
      familyName: `The ${parentLastName} Family`,
      members: [
        {
          personIdKey,
          firstName: childFirstName,
          lastName: parentLastName,
          fullName: `${childFirstName} ${parentLastName}`,
          gender: 'Unknown',
          roleName: 'Child',
          isChild: true,
          hasRecentCheckIn: false,
          hasCriticalAllergies: false,
        },
      ],
      recentCheckInCount: 0,
    },
  };
}

/**
 * Wire the minimum set of API mocks required for the registration flow:
 *  - configuration: so the page doesn't hang on missing backend
 *  - search:        returns empty array so the "Register New Family" CTA renders
 *  - register:      accepts registration and returns a new family (or error)
 *  - opportunities: mocked so the post-register auto-advance step doesn't error
 */
async function setupRegistrationMocks(
  page: Page,
  opts: {
    registerResponse?: unknown;
    registerStatus?: number;
  } = {},
) {
  const registerResp = opts.registerResponse ?? buildRegisteredFamily('Testerson', 'Tommy');
  const registerStatus = opts.registerStatus ?? 200;

  await page.route('**/api/v1/checkin/configuration**', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        data: {
          campus: { idKey: 'campus1', name: 'Main Campus' },
          areas: [],
          activeSchedules: [],
          serverTime: new Date().toISOString(),
        },
      }),
    }),
  );

  // Empty search result — forces the "Not Found? Register New Family" CTA.
  await page.route('**/api/v1/checkin/search', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ data: [] }),
    }),
  );

  // Registration endpoint — the call under test.
  await page.route('**/api/v1/checkin/register-family', (route) =>
    route.fulfill({
      status: registerStatus,
      contentType: 'application/json',
      body: JSON.stringify(registerResp),
    }),
  );

  // Post-register: the app calls opportunities/:familyIdKey to show member
  // selection. Return an empty opportunity set so the test doesn't depend on
  // opportunity UI, only on the transition itself.
  await page.route('**/api/v1/checkin/opportunities/**', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        data: {
          family: (registerResp as { data: unknown }).data,
          opportunities: [],
        },
      }),
    }),
  );
}

/**
 * Navigate from initial search screen → empty search → click register button.
 * Leaves the page on the wizard's parent step.
 *
 * @param mode - 'phone' pre-fills the wizard with the search phone (mirrors
 *   real usage). 'name' leaves the wizard's phone field empty, useful for
 *   phone-validation tests.
 */
async function gotoRegistrationWizard(
  page: Page,
  opts: { mode?: 'phone' | 'name'; query?: string } = {},
) {
  const mode = opts.mode ?? 'phone';
  const checkin = new CheckinPage(page);
  await checkin.goto();

  if (mode === 'name') {
    // Switch to name search so defaultPhone stays undefined
    await page.getByRole('button', { name: /search by name/i }).click();
    const nameInput = page.getByPlaceholder(/name|search/i).first();
    await nameInput.fill(opts.query ?? 'NoSuchFamily');
    await page.getByRole('button', { name: /search|find/i }).click();
  } else {
    await checkin.enterPhone(opts.query ?? '5550000000');
    await checkin.submitPhone();
  }

  // The no-results CTA should render
  const registerCta = page.getByRole('button', { name: /register new family/i });
  await expect(registerCta).toBeVisible({ timeout: 5000 });
  await registerCta.click();

  // Wizard step 1 (parent info) should be visible
  await expect(
    page.getByRole('heading', { name: 'Parent Information' }),
  ).toBeVisible();
}

/**
 * Type a 10-digit phone via the on-screen numpad.
 * The numpad buttons are unique (single-char labels 0-9) inside the registration
 * form, so we scope to the "Phone Number" section.
 */
async function typePhoneOnNumpad(page: Page, digits: string) {
  for (const d of digits) {
    // Each digit button has exactly the digit as its accessible name.
    // Multiple of the same digit button may exist (numpad reuses slots),
    // but .first() + sequential clicks is enough here.
    await page.getByRole('button', { name: new RegExp(`^${d}$`) }).first().click();
  }
}

// ===========================================================================
// 1. Wizard structure — step 1: parent info
// ===========================================================================

test.describe('Kiosk Family Registration — Wizard Structure', () => {
  test.beforeEach(async ({ page }) => {
    await setupRegistrationMocks(page);
  });

  test('route loads and reaches the register step from empty search', async ({ page }) => {
    await gotoRegistrationWizard(page);

    // The three step-indicator labels should all be present. "Parent Info" and
    // "Children" both appear as a step label AND (slightly different) as a
    // form heading, so scope to exact-text for the step strip.
    await expect(page.getByText('Parent Info', { exact: true })).toBeVisible();
    await expect(page.getByText('Children', { exact: true }).first()).toBeVisible();
    await expect(page.getByText('Review', { exact: true })).toBeVisible();
  });

  test('parent step renders required form fields (first, last, phone numpad)', async ({
    page,
  }) => {
    await gotoRegistrationWizard(page);

    // Form labels for the parent step
    await expect(page.getByText('First Name', { exact: true }).first()).toBeVisible();
    await expect(page.getByText('Last Name', { exact: true }).first()).toBeVisible();
    await expect(page.getByText('Phone Number', { exact: true })).toBeVisible();

    // The on-screen numpad renders digits 0-9 plus Clear and backspace.
    // At least one digit button must be clickable.
    await expect(page.getByRole('button', { name: /^1$/ }).first()).toBeVisible();
    await expect(page.getByRole('button', { name: /^Clear$/ })).toBeVisible();

    // Forward and back buttons
    await expect(page.getByRole('button', { name: /back to search/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /next: children/i })).toBeVisible();
  });

  test('parent step — validation: missing fields block advancing', async ({ page }) => {
    // Use name-search entry so phone field starts empty (phone search pre-fills
    // defaultPhone). This lets us exercise the phone validation path.
    await gotoRegistrationWizard(page, { mode: 'name' });

    // Click Next with everything blank
    await page.getByRole('button', { name: /next: children/i }).click();

    // Validation messages from KioskFamilyRegistration.validateParent()
    await expect(page.getByText('First name is required.')).toBeVisible();
    await expect(page.getByText('Last name is required.')).toBeVisible();
    await expect(page.getByText(/10-digit phone number/i)).toBeVisible();

    // We should still be on the parent step
    await expect(page.getByRole('heading', { name: 'Parent Information' })).toBeVisible();
  });

  test('parent step — partial phone blocks advance with 10-digit error', async ({ page }) => {
    // Name-search entry so the wizard starts with an empty phone.
    await gotoRegistrationWizard(page, { mode: 'name' });

    const parentFirst = page.getByPlaceholder('First name...').first();
    const parentLast = page.getByPlaceholder('Last name...').first();
    await parentFirst.fill('Alex');
    await parentLast.fill(uniqueSuffix('Parent'));

    // Enter only 5 digits — should still fail the phone check
    await typePhoneOnNumpad(page, '12345');
    await page.getByRole('button', { name: /next: children/i }).click();

    await expect(page.getByText(/10-digit phone number/i)).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Parent Information' })).toBeVisible();
  });
});

// ===========================================================================
// 2. Children step
// ===========================================================================

test.describe('Kiosk Family Registration — Children Step', () => {
  test.beforeEach(async ({ page }) => {
    await setupRegistrationMocks(page);
    await gotoRegistrationWizard(page);

    // Fill parent step with valid data to advance
    await page.getByPlaceholder('First name...').first().fill('Pat');
    await page.getByPlaceholder('Last name...').first().fill('Parentson');
    await typePhoneOnNumpad(page, '5551234567');
    await page.getByRole('button', { name: /next: children/i }).click();

    await expect(page.getByRole('heading', { name: 'Children' })).toBeVisible();
  });

  test('children step — renders first child block with pre-filled last name', async ({
    page,
  }) => {
    // The Child 1 block should be visible
    await expect(page.getByText('Child 1')).toBeVisible();

    // Last name should have defaulted to parent's last name ("Parentson").
    // Use a value attribute selector since getByDisplayValue is unavailable
    // in the installed Playwright version.
    const childLast = page.locator('input[value="Parentson"]');
    await expect(childLast.first()).toBeVisible();

    // Add-child and review buttons exist
    await expect(page.getByRole('button', { name: /add another child/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /^review$/i })).toBeVisible();
  });

  test('children step — validation: first name required to advance', async ({ page }) => {
    // Click Review without entering first name
    await page.getByRole('button', { name: /^review$/i }).click();

    await expect(page.getByText('First name is required.')).toBeVisible();
    // Should remain on children step
    await expect(page.getByRole('heading', { name: 'Children' })).toBeVisible();
  });

  test('children step — add and remove second child', async ({ page }) => {
    // Add a second child
    await page.getByRole('button', { name: /add another child/i }).click();
    await expect(page.getByText('Child 2')).toBeVisible();

    // Remove button on Child 2 should exist (only shown when >1 child)
    const removeButtons = page.getByRole('button', { name: /^remove$/i });
    await expect(removeButtons.first()).toBeVisible();

    // Click the last Remove button (Child 2) — Child 2 should disappear
    const count = await removeButtons.count();
    await removeButtons.nth(count - 1).click();
    await expect(page.getByText('Child 2')).not.toBeVisible();
  });

  test('children step — back navigates to parent step', async ({ page }) => {
    await page.getByRole('button', { name: /^back$/i }).click();
    await expect(page.getByRole('heading', { name: 'Parent Information' })).toBeVisible();
  });
});

// ===========================================================================
// 3. Review + Submit
// ===========================================================================

test.describe('Kiosk Family Registration — Review and Submit', () => {
  async function completeThroughReview(page: Page, parentLast: string, childFirst: string) {
    // Name-search entry leaves the wizard's phone field empty so we can
    // author a predictable 10-digit phone below.
    await gotoRegistrationWizard(page, { mode: 'name' });

    // Step 1: parent
    await page.getByPlaceholder('First name...').first().fill('Jordan');
    await page.getByPlaceholder('Last name...').first().fill(parentLast);
    await typePhoneOnNumpad(page, '5551234567');
    await page.getByRole('button', { name: /next: children/i }).click();

    // Step 2: children — fill first child first name
    await page.getByPlaceholder('First name...').first().fill(childFirst);
    await page.getByRole('button', { name: /^review$/i }).click();

    await expect(page.getByRole('heading', { name: /review.*register/i })).toBeVisible();
  }

  test('review step — shows parent summary, child summary, and action buttons', async ({
    page,
  }) => {
    const parentLast = uniqueSuffix('Fam');
    const childFirst = uniqueSuffix('Kid');
    await setupRegistrationMocks(page, {
      registerResponse: buildRegisteredFamily(parentLast, childFirst),
    });

    await completeThroughReview(page, parentLast, childFirst);

    // Parent block
    await expect(page.getByText('Parent / Guardian')).toBeVisible();
    await expect(page.getByText(`Jordan ${parentLast}`)).toBeVisible();

    // Child block (heading may be "Child" or "Children")
    await expect(page.getByText(/^Child(ren)?$/).first()).toBeVisible();
    await expect(page.getByText(`${childFirst} ${parentLast}`)).toBeVisible();

    // Submit + back buttons
    await expect(
      page.getByRole('button', { name: /complete registration/i }),
    ).toBeVisible();
    await expect(page.getByRole('button', { name: /^back$/i })).toBeVisible();
  });

  test('review step — back returns to children step with entered data intact', async ({
    page,
  }) => {
    const parentLast = uniqueSuffix('Fam');
    const childFirst = uniqueSuffix('Kid');
    await setupRegistrationMocks(page, {
      registerResponse: buildRegisteredFamily(parentLast, childFirst),
    });

    await completeThroughReview(page, parentLast, childFirst);

    await page.getByRole('button', { name: /^back$/i }).click();

    // Back on the children step with the child name preserved.
    // Use a value attribute selector (escape CSS special chars in the suffix).
    await expect(page.getByRole('heading', { name: 'Children' })).toBeVisible();
    const escaped = childFirst.replace(/"/g, '\\"');
    await expect(page.locator(`input[value="${escaped}"]`)).toBeVisible();
  });

  test('@smoke happy path — submit creates family and advances past the wizard', async ({
    page,
  }) => {
    const parentLast = uniqueSuffix('Fam');
    const childFirst = uniqueSuffix('Kid');
    await setupRegistrationMocks(page, {
      registerResponse: buildRegisteredFamily(parentLast, childFirst),
    });

    // Capture the registration POST payload to assert it contains trimmed data
    const requestPromise = page.waitForRequest(
      (req) =>
        req.url().includes('/api/v1/checkin/register-family') && req.method() === 'POST',
    );

    await completeThroughReview(page, parentLast, childFirst);

    await page.getByRole('button', { name: /complete registration/i }).click();

    const request = await requestPromise;
    const postBody = JSON.parse(request.postData() ?? '{}');
    expect(postBody.parentFirstName).toBe('Jordan');
    expect(postBody.parentLastName).toBe(parentLast);
    expect(postBody.phoneNumber).toBe('5551234567');
    expect(Array.isArray(postBody.children)).toBe(true);
    expect(postBody.children[0]?.firstName).toBe(childFirst);

    // After onComplete, CheckinPage transitions to 'select-members' step —
    // the "Who's Checking In?" heading is the signal this happened.
    await expect(
      page.getByRole('heading', { name: /who.*checking in/i }),
    ).toBeVisible({ timeout: 5000 });
    // The wizard itself (parent/children/review headings) is gone
    await expect(
      page.getByRole('heading', { name: /review.*register/i }),
    ).not.toBeVisible();
  });

  test('submit — API error shows friendly message and keeps wizard on review step', async ({
    page,
  }) => {
    const parentLast = uniqueSuffix('Fam');
    const childFirst = uniqueSuffix('Kid');
    await setupRegistrationMocks(page, {
      registerStatus: 500,
      registerResponse: { error: 'Server exploded' },
    });

    await completeThroughReview(page, parentLast, childFirst);

    await page.getByRole('button', { name: /complete registration/i }).click();

    // The review heading should still be visible (did NOT advance)
    await expect(page.getByRole('heading', { name: /review.*register/i })).toBeVisible();

    // An error surface must appear — either the backend's message or the
    // generic "Registration failed" fallback from KioskFamilyRegistration.
    await expect(
      page
        .getByText(/registration failed/i)
        .or(page.getByText(/server exploded/i)),
    ).toBeVisible({ timeout: 5000 });
  });
});

// ===========================================================================
// 4. Cancel / navigation back to search
// ===========================================================================

test.describe('Kiosk Family Registration — Cancel', () => {
  test.beforeEach(async ({ page }) => {
    await setupRegistrationMocks(page);
  });

  test('cancel from parent step returns to the search screen', async ({ page }) => {
    await gotoRegistrationWizard(page);

    await page.getByRole('button', { name: /back to search/i }).click();

    // Back on the search step — "Enter Phone Number" heading from PhoneSearch
    // is the stable signal. The "Search by Phone" toggle button is only shown
    // when no search input is typed, so we don't rely on it here.
    await expect(
      page.getByRole('heading', { name: /enter phone number/i }),
    ).toBeVisible();
    // The wizard should be gone
    await expect(
      page.getByRole('heading', { name: 'Parent Information' }),
    ).not.toBeVisible();
  });

  test('cancel then re-enter — wizard state is reset (no stale data)', async ({ page }) => {
    await gotoRegistrationWizard(page);

    // Fill some data
    await page.getByPlaceholder('First name...').first().fill('WillBeForgotten');
    await typePhoneOnNumpad(page, '1234');

    // Cancel
    await page.getByRole('button', { name: /back to search/i }).click();
    await expect(
      page.getByRole('heading', { name: /enter phone number/i }),
    ).toBeVisible();

    // Re-enter the wizard: the no-results card + Register CTA is still shown
    // from the previous empty search (searchValue is preserved by CheckinPage).
    await page.getByRole('button', { name: /register new family/i }).click();

    // Parent Information heading visible again
    await expect(
      page.getByRole('heading', { name: 'Parent Information' }),
    ).toBeVisible();

    // First name field should be empty — wizard was remounted, state reset
    const firstName = page.getByPlaceholder('First name...').first();
    await expect(firstName).toHaveValue('');
  });
});
