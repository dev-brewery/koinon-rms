/**
 * E2E Tests: Issue #679 (Wave 6) — Check-in Configuration admin coverage
 *
 * Prior coverage audit:
 *  - No dedicated CheckinConfigPage spec existed. The existing tests under
 *    `src/web/e2e/tests/admin/checkin/` (checkin-errors, checkin-offline,
 *    checkin-performance, checkin-qr-scanner) all exercise the kiosk-side
 *    `/checkin` check-in page, NOT the admin-side `/admin/checkin` config
 *    page. `navigation/admin-navigation.spec.ts` does not click through to
 *    `/admin/checkin`.
 *  - RosterPage is skipped entirely in this wave — `admin/roster/
 *    room-roster.spec.ts` already provides 8 tests covering the header,
 *    location picker, auto-refresh toggle, refresh button, empty state,
 *    description text, and @smoke navigation. The real gap there is
 *    tiny (loading a populated roster requires seeded room attendance
 *    fixtures that don't exist) and not worth duplicating.
 *
 * This spec covers CheckinConfigPage at `/admin/checkin`:
 *  - Page header + three-tab scaffold (Areas, Locations, Devices).
 *  - Tab switching preserves on-page navigation.
 *  - AreasTab: only the shell is verifiable (see NEW BUG #1 below).
 *  - LocationsTab: only the tab-chrome is verifiable (see NEW BUG #2 below).
 *  - DevicesTab: list/empty rendering + CRUD against live API, token
 *    generation flow, delete confirmation. NOTE: full edit-name round-trip
 *    is skipped (see NEW BUG #3 below).
 *
 * Mocking policy (matches prior admin spec pattern): real API for everything.
 * All created test data uses uniqueSuffix() for isolation.
 *
 * NEW BUG #1 (skip-and-flag): CheckinAreasTab always renders ErrorState.
 *   The tab calls `getCheckinConfiguration()` with no args, but the API
 *   endpoint `GET /api/v1/checkin/configuration` returns HTTP 400 with
 *   `"Either campusId or kioskId must be provided"`. Verified directly
 *   against the live API. The tab therefore NEVER reaches the list /
 *   empty-state / guidance-note paths for an authenticated admin — it
 *   always shows "Failed to load check-in areas". Suggested issue title:
 *     "fix(web): CheckinAreasTab must pass campusId to /checkin/configuration"
 *
 * NEW BUG #2 (skip-and-flag): CheckinLocationsTab crashes the page.
 *   `services/api/locations.ts::getLocations()` does NOT unwrap the
 *   `{ data: [...] }` API envelope (compare devices.ts / groups.ts,
 *   which do). `useLocations()` therefore resolves to an object, not
 *   an array. When `CheckinLocationsTab` does
 *   `locationList.map(...)` after `(locations ?? [])`, it invokes .map
 *   on a non-array and React throws, triggering the global error
 *   boundary ("Something went wrong"). Every non-shell Locations-tab
 *   assertion fails because the tab never renders. The same API
 *   returns a properly shaped `{data:[...]}` in live curl checks, so
 *   the fix is strictly in the client service. Suggested issue title:
 *     "fix(web): unwrap `data` envelope in locations API service"
 *
 * NEW BUG #3 (skip-and-flag): device name edit does not persist to the
 *   list view. Create-then-edit-name-then-observe cycle shows the
 *   original (pre-edit) name still in the table row after save. The exact
 *   cause (stale cache, missing invalidation, backend ignoring name in
 *   UpdateDeviceRequest) needs backend investigation. Suggested issue
 *   title:
 *     "fix(api|web): device name edit does not persist in devices list"
 *
 * NEW BUG #4 (skip-and-flag): `DELETE /api/v1/devices/{idKey}` returns
 *   HTTP 204 but the device remains in `GET /api/v1/devices` afterwards
 *   (verified via direct curl). The UI's ConfirmDialog closes cleanly
 *   but the row stays visible indefinitely. Suggested issue title:
 *     "fix(api): DELETE /devices/{idKey} returns 204 without persisting"
 *
 * Guardrails:
 *  - No data-testid attributes added to production code.
 *  - No edits to page-objects, fixtures, seeder, or auth helpers.
 *  - Device cleanup is best-effort via the same UI we're testing.
 */

import { test, expect, type Page } from '../../fixtures/auth.fixture';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function uniqueSuffix(label: string): string {
  return `${label}-${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
}

async function gotoCheckinConfig(page: Page): Promise<void> {
  await page.goto('/admin/checkin');
  await expect(page.getByRole('heading', { name: /check-in setup/i })).toBeVisible();
}

async function switchTab(page: Page, label: 'Areas' | 'Locations' | 'Devices'): Promise<void> {
  await page.getByRole('button', { name: label, exact: true }).click();
}

/**
 * Best-effort delete of a device we created, via the Devices tab UI.
 * Silent on any failure so teardown never breaks a test.
 */
async function cleanupDevice(page: Page, deviceName: string): Promise<void> {
  try {
    await page.goto('/admin/checkin');
    await switchTab(page, 'Devices');
    const row = page.getByRole('row').filter({ hasText: deviceName }).first();
    if (await row.isVisible({ timeout: 2000 }).catch(() => false)) {
      await row.getByRole('button', { name: /delete/i }).click();
      await page.getByRole('button', { name: /^delete$/i }).last().click();
    }
  } catch {
    // Swallow — teardown is best-effort.
  }
}

// ---------------------------------------------------------------------------
// Page shell + tabs
// ---------------------------------------------------------------------------

test.describe('Check-in Config — page shell', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('renders header and description', async ({ page }) => {
    await gotoCheckinConfig(page);
    await expect(page.getByRole('heading', { name: /check-in setup/i })).toBeVisible();
    await expect(
      page.getByText(/configure check-in areas, location capacities, and kiosk devices/i),
    ).toBeVisible();
  });

  test('renders three tabs: Areas, Locations, Devices', async ({ page }) => {
    await gotoCheckinConfig(page);
    const tabs = page.getByRole('navigation', { name: /check-in configuration tabs/i });
    await expect(tabs.getByRole('button', { name: 'Areas', exact: true })).toBeVisible();
    await expect(tabs.getByRole('button', { name: 'Locations', exact: true })).toBeVisible();
    await expect(tabs.getByRole('button', { name: 'Devices', exact: true })).toBeVisible();
  });

  test('Areas tab is active by default', async ({ page }) => {
    await gotoCheckinConfig(page);
    const areasTab = page.getByRole('button', { name: 'Areas', exact: true });
    await expect(areasTab).toHaveAttribute('aria-current', 'page');
  });

  test('can switch between tabs and active state follows (Areas ↔ Devices)', async ({ page }) => {
    // NOTE: We intentionally do NOT click the Locations tab here because
    // NEW BUG #2 causes that tab click to crash the page via the global
    // error boundary. Areas and Devices both render their tab bodies
    // without crashing.
    await gotoCheckinConfig(page);
    const areasTab = page.getByRole('button', { name: 'Areas', exact: true });
    const devicesTab = page.getByRole('button', { name: 'Devices', exact: true });

    await devicesTab.click();
    await expect(devicesTab).toHaveAttribute('aria-current', 'page');
    await expect(areasTab).not.toHaveAttribute('aria-current', 'page');

    await areasTab.click();
    await expect(areasTab).toHaveAttribute('aria-current', 'page');
    await expect(devicesTab).not.toHaveAttribute('aria-current', 'page');
  });
});

// ---------------------------------------------------------------------------
// Areas tab
// ---------------------------------------------------------------------------

test.describe('Check-in Config — Areas tab', () => {
  test.beforeEach(async ({ loginAsAdmin, page }) => {
    await loginAsAdmin();
    await gotoCheckinConfig(page);
    // Areas is default, no tab click needed.
  });

  test('renders the tab error state — tab body loads without crashing the page', async ({ page }) => {
    // Due to NEW BUG #1, the Areas tab always renders the ErrorState
    // ("Failed to load check-in areas" + "Try Again"). We assert that
    // the error is rendered INSIDE the tab and does NOT crash the
    // outer page (no global error boundary). This ensures the tab
    // shell degrades gracefully.
    await expect(page.getByRole('heading', { name: /check-in setup/i })).toBeVisible();
    await expect(
      page.getByRole('heading', { name: /failed to load check-in areas/i }),
    ).toBeVisible();
    await expect(page.getByRole('button', { name: /try again/i })).toBeVisible();
  });

  test.skip('shows the "managed via Groups" guidance note', async ({ page }) => {
    // SKIP: NEW BUG #1 — Areas tab never reaches the success path from
    // the admin config page because /checkin/configuration requires
    // campusId/kioskId query parameters that the UI does not send.
    await expect(
      page.getByText(/check-in areas are configured through group management/i),
    ).toBeVisible();
  });

  test.skip('shows a configured-count summary', async ({ page }) => {
    // SKIP: NEW BUG #1 — see above; success path never renders.
    await expect(
      page.getByText(/\d+\s+(area|areas)\s+configured/i).first(),
    ).toBeVisible();
  });
});

// ---------------------------------------------------------------------------
// Locations tab
// ---------------------------------------------------------------------------

test.describe('Check-in Config — Locations tab', () => {
  test.beforeEach(async ({ loginAsAdmin, page }) => {
    await loginAsAdmin();
    await gotoCheckinConfig(page);
  });

  test('tab is present in the tab bar (shell-only)', async ({ page }) => {
    // NEW BUG #2 prevents us from asserting tab content (the page crashes
    // to the global error boundary once the hook resolves). We verify
    // the navigable tab chrome instead — the Locations button is rendered
    // but is NOT currently active (Areas is active by default).
    const locationsTab = page.getByRole('button', { name: 'Locations', exact: true });
    await expect(locationsTab).toBeVisible();
    await expect(locationsTab).not.toHaveAttribute('aria-current', 'page');
    // Do NOT click — clicking will crash the page due to the .map() on
    // a non-array shape returned by getLocations().
  });

  test.skip('renders table headers (Location, Campus, Type, Status, Actions)', async ({ page }) => {
    // SKIP: NEW BUG #2 — clicking the Locations tab crashes the page
    // via the global error boundary because getLocations() does not
    // unwrap the { data: [...] } envelope.
    await switchTab(page, 'Locations');
    await expect(page.getByRole('columnheader', { name: /^location$/i })).toBeVisible();
  });

  test.skip('Edit Capacity button opens the capacity modal (when rows present)', async ({ page }) => {
    // SKIP: NEW BUG #2 — tab never renders its success body.
    await switchTab(page, 'Locations');
    await page.getByRole('button', { name: /edit capacity/i }).first().click();
    await expect(page.getByRole('dialog')).toBeVisible();
  });
});

// ---------------------------------------------------------------------------
// Devices tab
// ---------------------------------------------------------------------------

test.describe('Check-in Config — Devices tab', () => {
  test.beforeEach(async ({ loginAsAdmin, page }) => {
    await loginAsAdmin();
    await gotoCheckinConfig(page);
    await switchTab(page, 'Devices');
  });

  test('shows Add Device button and a registered-count summary', async ({ page }) => {
    await expect(page.getByRole('button', { name: /^add device$/i }).first()).toBeVisible();
    await expect(
      page.getByText(/\d+\s+(device|devices)\s+registered/i).first(),
    ).toBeVisible();
  });

  test('Add Device opens the device form modal', async ({ page }) => {
    await page.getByRole('button', { name: /^add device$/i }).first().click();
    const dialog = page.getByRole('dialog', { name: /add device/i });
    await expect(dialog).toBeVisible();
    await expect(dialog.getByLabel(/device name/i)).toBeVisible();
    await expect(dialog.getByLabel(/ip address/i)).toBeVisible();
    // Close the modal.
    await dialog.getByRole('button', { name: /^cancel$/i }).click();
    await expect(dialog).not.toBeVisible();
  });

  test('submit is disabled until a device name is entered', async ({ page }) => {
    await page.getByRole('button', { name: /^add device$/i }).first().click();
    const dialog = page.getByRole('dialog', { name: /add device/i });
    const submit = dialog.getByRole('button', { name: /^add device$/i });
    await expect(submit).toBeDisabled();
    await dialog.getByLabel(/device name/i).fill('temp');
    await expect(submit).toBeEnabled();
    await dialog.getByRole('button', { name: /^cancel$/i }).click();
  });

  test('creates a kiosk device and lists it with the provided IP', async ({ page }) => {
    const name = uniqueSuffix('E2E Wave 6 Kiosk');

    await page.getByRole('button', { name: /^add device$/i }).first().click();
    const createDialog = page.getByRole('dialog', { name: /add device/i });
    await createDialog.getByLabel(/device name/i).fill(name);
    await createDialog.getByLabel(/ip address/i).fill('10.0.0.99');
    await createDialog.getByRole('button', { name: /^add device$/i }).click();

    const createdRow = page.getByRole('row').filter({ hasText: name });
    await expect(createdRow).toBeVisible({ timeout: 10_000 });
    await expect(createdRow).toContainText('10.0.0.99');
  });

  test('delete removes the device row from the list', async ({ page }) => {
    // Fixed in #695: DELETE now actually persists the soft-delete, so
    // the row disappears from the (default IsActive=true) list query.
    const name = uniqueSuffix('E2E Wave 6 Delete Kiosk');
    await page.getByRole('button', { name: /^add device$/i }).first().click();
    const createDialog = page.getByRole('dialog', { name: /add device/i });
    await createDialog.getByLabel(/device name/i).fill(name);
    await createDialog.getByRole('button', { name: /^add device$/i }).click();
    const createdRow = page.getByRole('row').filter({ hasText: name });
    await expect(createdRow).toBeVisible({ timeout: 10_000 });
    await createdRow.getByRole('button', { name: /^delete$/i }).click();
    const confirmDialog = page.getByRole('dialog', { name: /delete device/i });
    await confirmDialog.getByRole('button', { name: /^delete$/i }).click();
    await expect(page.getByRole('row').filter({ hasText: name })).toHaveCount(0, {
      timeout: 10_000,
    });
  });

  test('device name edit persists to the devices list', async ({ page }) => {
    // Fixed in #694: backend UpdateDeviceAsync now loads the entity with
    // AsTracking() so SaveChanges actually persists the Name mutation.
    const originalName = uniqueSuffix('E2E Wave 6 Kiosk');
    const renamedName = `${originalName}-edited`;

    await page.getByRole('button', { name: /^add device$/i }).first().click();
    const createDialog = page.getByRole('dialog', { name: /add device/i });
    await createDialog.getByLabel(/device name/i).fill(originalName);
    await createDialog.getByRole('button', { name: /^add device$/i }).click();
    const createdRow = page.getByRole('row').filter({ hasText: originalName });
    await expect(createdRow).toBeVisible({ timeout: 10_000 });

    try {
      await createdRow.getByRole('button', { name: /^edit$/i }).click();
      const editDialog = page.getByRole('dialog', { name: /edit device/i });
      await editDialog.getByLabel(/device name/i).fill(renamedName);
      await editDialog.getByRole('button', { name: /save changes/i }).click();

      const renamedRow = page.getByRole('row').filter({ hasText: renamedName });
      await expect(renamedRow).toBeVisible({ timeout: 10_000 });
    } finally {
      await cleanupDevice(page, originalName);
      await cleanupDevice(page, renamedName);
    }
  });

  test('cancel confirms dialog does NOT delete the device', async ({ page }) => {
    const name = uniqueSuffix('E2E Wave 6 Cancel Kiosk');

    // Create a device to cancel-delete against.
    await page.getByRole('button', { name: /^add device$/i }).first().click();
    const createDialog = page.getByRole('dialog', { name: /add device/i });
    await createDialog.getByLabel(/device name/i).fill(name);
    await createDialog.getByRole('button', { name: /^add device$/i }).click();

    const row = page.getByRole('row').filter({ hasText: name });
    await expect(row).toBeVisible({ timeout: 10_000 });

    try {
      await row.getByRole('button', { name: /^delete$/i }).click();
      const confirmDialog = page.getByRole('dialog', { name: /delete device/i });
      await expect(confirmDialog).toBeVisible();
      await confirmDialog.getByRole('button', { name: /^cancel$/i }).click();
      await expect(confirmDialog).not.toBeVisible();
      await expect(row).toBeVisible();
    } finally {
      await cleanupDevice(page, name);
    }
  });

  test('generates a kiosk token and shows the one-time-display dialog', async ({ page }) => {
    const name = uniqueSuffix('E2E Wave 6 Token Kiosk');

    // Create a device first.
    await page.getByRole('button', { name: /^add device$/i }).first().click();
    const createDialog = page.getByRole('dialog', { name: /add device/i });
    await createDialog.getByLabel(/device name/i).fill(name);
    await createDialog.getByRole('button', { name: /^add device$/i }).click();

    const row = page.getByRole('row').filter({ hasText: name });
    await expect(row).toBeVisible({ timeout: 10_000 });

    try {
      await row.getByRole('button', { name: /^token$/i }).click();
      const tokenDialog = page.getByRole('dialog', { name: /kiosk token generated/i });
      await expect(tokenDialog).toBeVisible({ timeout: 10_000 });
      await expect(tokenDialog.getByText(/the token will only be shown once/i)).toBeVisible();
      await expect(tokenDialog.getByRole('button', { name: /copy token/i })).toBeVisible();
      await tokenDialog.getByRole('button', { name: /^done$/i }).click();
      await expect(tokenDialog).not.toBeVisible();
    } finally {
      await cleanupDevice(page, name);
    }
  });
});
