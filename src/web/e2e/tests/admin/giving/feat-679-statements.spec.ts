/**
 * E2E Tests: Issue #679 (Wave 4) — Statements admin page coverage
 *
 * feat-493 (AC5) already exercises: heading, start/end date inputs, and the
 * "Find Eligible" button is present. It also asserts the API-driven
 * statement generation pipeline.
 *
 * Page-level gaps NOT covered by feat-493:
 *  - Minimum Amount filter input is present
 *  - "Generated Statements" section heading renders
 *  - Find Eligible button disabled state when date range is cleared
 *  - Validation toast when the Generate Selected button has zero selection
 *    (requires the eligible list to load — blocked by #666, skipped)
 *
 * Known pre-existing bugs (skip-and-flag):
 *  - #666 — GET /api/v1/contributions/statements/eligible returns 500 due to
 *    an EF Core LINQ translation bug. StatementsPage auto-invokes this on
 *    mount through useEligiblePeople; any test that needs the eligible
 *    picker to populate will hit this. We skip the one test that depends
 *    on the picker list rendering with rows and reference #666.
 */

import { test, expect } from '../../../fixtures/auth.fixture';

test.describe('StatementsPage — filters and list sections', () => {
  test.beforeEach(async ({ loginAsAdmin, page }) => {
    await loginAsAdmin();
    await page.goto('/admin/giving/statements');
    await expect(
      page.getByRole('heading', { name: /contribution statements/i }),
    ).toBeVisible({ timeout: 10_000 });
  });

  test('"Find Eligible Contributors" filters section renders date + amount inputs', async ({ page }) => {
    await expect(
      page.getByRole('heading', { name: /Find Eligible Contributors/i }),
    ).toBeVisible();
    await expect(page.locator('#startDate')).toBeVisible();
    await expect(page.locator('#endDate')).toBeVisible();
    await expect(page.locator('#minimumAmount')).toBeVisible();
    await expect(page.getByRole('button', { name: /Find Eligible/i })).toBeVisible();
  });

  test('Minimum Amount filter accepts a numeric value', async ({ page }) => {
    const minAmount = page.locator('#minimumAmount');
    await minAmount.fill('25.00');
    await expect(minAmount).toHaveValue('25.00');
  });

  test('Find Eligible button is disabled when start date is cleared', async ({ page }) => {
    // Default state: both dates are prefilled with the current year range,
    // so the button is enabled. Clearing start date disables it.
    const findBtn = page.getByRole('button', { name: /Find Eligible/i });
    await expect(findBtn).toBeEnabled();

    await page.locator('#startDate').fill('');
    await expect(findBtn).toBeDisabled();
  });

  test('"Generated Statements" section heading renders', async ({ page }) => {
    // The statements list (lower section) always renders a heading. Whether
    // rows exist depends on DB state; feat-493 may or may not have run
    // previously, so we only assert the section is present.
    await expect(
      page.getByRole('heading', { name: /Generated Statements/i }),
    ).toBeVisible({ timeout: 10_000 });
  });

  test('date range defaults to the current year (Jan 1 – Dec 31)', async ({ page }) => {
    const year = new Date().getFullYear();
    await expect(page.locator('#startDate')).toHaveValue(`${year}-01-01`);
    await expect(page.locator('#endDate')).toHaveValue(`${year}-12-31`);
  });

  // ---------------------------------------------------------------------
  // Skipped (pre-existing bug #666)
  // ---------------------------------------------------------------------

  test.skip('eligible contributors list renders rows after Find Eligible — BLOCKED by #666', async ({ page }) => {
    // /api/v1/contributions/statements/eligible returns 500 due to an EF
    // Core LINQ translation bug in GetEligiblePeopleAsync. Until #666
    // lands, clicking Find Eligible puts the page in the ErrorState path
    // ("Failed to load eligible people"), not the populated-table path.
    // Re-enable once #666 is fixed.
    await page.getByRole('button', { name: /Find Eligible/i }).click();
    await expect(page.getByRole('heading', { name: /Eligible Contributors/i })).toBeVisible();
  });

  test.skip('Generate Selected emits a toast with no people selected — BLOCKED by #666', async ({ page }) => {
    // Button is gated behind the eligible-people list rendering; see above.
    // Once #666 is resolved, this test should click Select All then
    // Deselect All and click Generate Selected (0 selected) to assert the
    // "No people selected" toast.
    await page.getByRole('button', { name: /Find Eligible/i }).click();
  });
});
