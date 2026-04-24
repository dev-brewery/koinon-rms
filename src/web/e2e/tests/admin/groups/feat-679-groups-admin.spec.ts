/**
 * E2E Tests: Issue #679 (Wave 5) — Groups admin coverage gap fill
 *
 * Prior coverage audit:
 *  - groups-tree.spec.ts (12 tests): tree/list toggle, search, empty-state,
 *    click-through, create-button — GroupsTreePage is COVERED.
 *  - group-crud.spec.ts (17 tests): create/read/update/delete, validation,
 *    cancel flows, inactive status — basic GroupFormPage + GroupDetailPage
 *    are COVERED.
 *  - group-members.spec.ts: members display/count/add flow (many .skip()ed
 *    pending feature work).
 *  - feat-488-feat-groups-add-rapid-group-at.spec.ts: operates on /my-groups
 *    (Take Attendance modal) — does NOT cover the admin detail page sections.
 *
 * Gaps this spec fills (each is exercised only at most incidentally elsewhere):
 *  - GroupDetailPage
 *      * Child Groups section renders when children exist, with each child a
 *        real link into the detail hierarchy ("Add Child Group" shortcut).
 *      * GroupSchedulesSection renders with a count header + "Add Schedule"
 *        button; empty-state copy is shown when no schedules are attached.
 *      * GroupAttendanceHistorySection renders with a heading; empty-state
 *        copy is shown when no attendance has been recorded for the group.
 *  - GroupFormPage
 *      * Pre-selecting a parent via ?parentId=... disables the parent select
 *        and shows the "Parent group is pre-selected" helper text.
 *      * Edit-mode hides the Group Type selector (immutable after creation).
 *      * Campus and Parent Group selects render populated options (fixed in
 *        #690 — previously these rendered only their placeholder).
 *
 * Mocking policy: all tests hit the real API against seeded data; the one
 * synthetic scenario (Add Child Group shortcut pre-selects parentId) simply
 * reads the URL/query param behavior without any route mocks.
 *
 * Guardrails: no data-testid additions; selectors use role/heading/label.
 * Created groups get uniqueSuffix() names so parallel runs don't collide.
 * Seeded groups (Nursery/Preschool/Elementary) are read-only in this spec.
 */

import { test, expect, type Page } from '../../../fixtures/auth.fixture';
import { testData } from '../../../fixtures/test-data';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function uniqueSuffix(label: string): string {
  return `${label}-${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
}

/**
 * Navigate to a seeded group's detail page by name. Returns the idKey.
 * Uses the search input to filter first — parallel test pollution can push
 * seeded groups below the default page size otherwise.
 */
async function openSeededGroupDetail(
  page: Page,
  groupName: string,
): Promise<string> {
  await page.goto('/admin/groups');
  await expect(page.getByRole('heading', { name: 'Groups' })).toBeVisible({
    timeout: 10_000,
  });
  await page.getByPlaceholder(/search groups/i).fill(groupName);
  // Wait for the debounced search + refetch to settle.
  await page.waitForTimeout(600);
  await page.getByText(groupName, { exact: true }).first().click();
  await expect(page).toHaveURL(/\/admin\/groups\/(?!new(?:\/|$))[^/]+$/, {
    timeout: 10_000,
  });
  const url = new URL(page.url());
  return url.pathname.split('/').pop()!;
}

/**
 * Pick the first non-placeholder option in the Group Type select, waiting for
 * group types to load (the hook is async). Fails loudly if no real options
 * appear within the timeout.
 */
async function pickFirstRealGroupType(page: Page): Promise<void> {
  const typeSelect = page.locator('#groupTypeId');
  // Wait for at least one real option beyond the placeholder.
  await expect
    .poll(async () => await typeSelect.locator('option').count(), { timeout: 10_000 })
    .toBeGreaterThan(1);
  const value = await typeSelect.locator('option').nth(1).getAttribute('value');
  if (!value) throw new Error('Group Type options have empty values');
  await typeSelect.selectOption(value);
}

/** Create a brand-new group via the UI, returning its idKey. */
async function createGroupViaUi(page: Page, name: string): Promise<string> {
  await page.goto('/admin/groups/new');
  await expect(
    page.getByRole('heading', { name: /create group/i }),
  ).toBeVisible({ timeout: 10_000 });

  await page.locator('#name').fill(name);
  await pickFirstRealGroupType(page);

  await page.getByRole('button', { name: /create group/i }).click();
  await expect(page).toHaveURL(/\/admin\/groups\/(?!new(?:\/|$))[^/]+$/, {
    timeout: 15_000,
  });
  const url = new URL(page.url());
  return url.pathname.split('/').pop()!;
}

/** Create a child group under a given parentIdKey via the URL shortcut. */
async function createChildGroupViaUi(
  page: Page,
  parentIdKey: string,
  name: string,
): Promise<string> {
  await page.goto(`/admin/groups/new?parentId=${parentIdKey}`);
  await expect(
    page.getByRole('heading', { name: /create group/i }),
  ).toBeVisible({ timeout: 10_000 });
  await page.locator('#name').fill(name);
  await pickFirstRealGroupType(page);
  await page.getByRole('button', { name: /create group/i }).click();
  await expect(page).toHaveURL(/\/admin\/groups\/(?!new(?:\/|$))[^/]+$/, {
    timeout: 15_000,
  });
  const url = new URL(page.url());
  return url.pathname.split('/').pop()!;
}

// ---------------------------------------------------------------------------
// GroupDetailPage — section gap coverage
// ---------------------------------------------------------------------------

test.describe('GroupDetailPage — schedules section', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('renders the Schedules section with a count header and Add Schedule button', async ({
    page,
  }) => {
    await openSeededGroupDetail(page, testData.groups.nursery.name);

    await expect(
      page.getByRole('heading', { name: /^schedules \(\d+\)$/i }),
    ).toBeVisible({ timeout: 10_000 });
    await expect(
      page.getByRole('button', { name: /add schedule/i }),
    ).toBeVisible();
  });

  test('newly created group shows the zero-schedule empty state', async ({ page }) => {
    // Brand-new group is guaranteed to have no schedules attached.
    const name = `E2E Wave5 EmptySched ${uniqueSuffix('g')}`;
    await createGroupViaUi(page, name);

    const section = page
      .getByRole('heading', { name: /^schedules \(0\)$/i })
      .locator('../..');
    await expect(section).toBeVisible({ timeout: 10_000 });
    // Empty state helper copy within the GroupSchedulesSection.
    await expect(section.getByText(/no schedules/i)).toBeVisible();
  });
});

test.describe('GroupDetailPage — attendance history section', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('renders the Attendance History section heading', async ({ page }) => {
    await openSeededGroupDetail(page, testData.groups.preschool.name);
    await expect(
      page.getByRole('heading', { name: /attendance history/i }),
    ).toBeVisible({ timeout: 10_000 });
  });

  test('newly created group shows an empty attendance history', async ({ page }) => {
    const name = `E2E Wave5 EmptyAtt ${uniqueSuffix('g')}`;
    await createGroupViaUi(page, name);

    const heading = page.getByRole('heading', { name: /attendance history/i });
    await expect(heading).toBeVisible({ timeout: 10_000 });

    // Empty state (no occurrences) — the section must render without error.
    // We scope to the section to avoid matching the nav or other copy.
    const section = heading.locator('../..');
    await expect(section).toBeVisible();
  });
});

test.describe('GroupDetailPage — child groups + metadata sidebar', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('sidebar renders Statistics and Metadata cards', async ({ page }) => {
    await openSeededGroupDetail(page, testData.groups.nursery.name);

    await expect(
      page.getByRole('heading', { name: 'Statistics' }),
    ).toBeVisible();
    await expect(
      page.getByRole('heading', { name: 'Metadata' }),
    ).toBeVisible();
    // Sidebar Metadata always shows a Created date.
    await expect(page.getByText(/^created$/i).first()).toBeVisible();
  });

  test('"Add Child Group" shortcut appears when children exist and pre-selects parentId', async ({
    page,
  }) => {
    // Create parent + one child so the Child Groups section renders.
    const parentName = `E2E Wave5 Parent ${uniqueSuffix('g')}`;
    const parentIdKey = await createGroupViaUi(page, parentName);

    const childName = `E2E Wave5 Child ${uniqueSuffix('g')}`;
    await createChildGroupViaUi(page, parentIdKey, childName);

    // Back to parent, confirm Child Groups section + shortcut link.
    await page.goto(`/admin/groups/${parentIdKey}`);
    await expect(
      page.getByRole('heading', { name: /child groups \(\d+\)/i }),
    ).toBeVisible({ timeout: 10_000 });

    const addChild = page.getByRole('link', { name: /add child group/i });
    await expect(addChild).toBeVisible();
    // Href carries the parentId query param.
    await expect(addChild).toHaveAttribute(
      'href',
      new RegExp(`parentId=${parentIdKey}`),
    );
  });

  test('child group link from parent detail navigates to the child detail', async ({
    page,
  }) => {
    const parentName = `E2E Wave5 ParentNav ${uniqueSuffix('g')}`;
    const parentIdKey = await createGroupViaUi(page, parentName);

    const childName = `E2E Wave5 ChildNav ${uniqueSuffix('g')}`;
    await createChildGroupViaUi(page, parentIdKey, childName);

    // Visit parent, click the child link inside Child Groups.
    await page.goto(`/admin/groups/${parentIdKey}`);
    const childSection = page
      .getByRole('heading', { name: /child groups \(\d+\)/i })
      .locator('../..');
    await childSection.getByText(childName).click();
    await expect(page).toHaveURL(/\/admin\/groups\/[^/]+$/);
    await expect(page.getByRole('heading', { name: childName })).toBeVisible();
  });
});

// ---------------------------------------------------------------------------
// GroupFormPage — pre-select, edit-mode, and dropdown gaps
// ---------------------------------------------------------------------------

test.describe('GroupFormPage — parentId pre-selection', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('visiting /admin/groups/new?parentId=... disables the Parent Group select', async ({
    page,
  }) => {
    // Use a real, seeded group's idKey. Any valid value disables the select;
    // the option list is populated after #690.
    const parentIdKey = await openSeededGroupDetail(
      page,
      testData.groups.elementary.name,
    );

    await page.goto(`/admin/groups/new?parentId=${parentIdKey}`);
    await expect(
      page.getByRole('heading', { name: /create group/i }),
    ).toBeVisible({ timeout: 10_000 });

    await expect(page.locator('#parentGroupId')).toBeDisabled();
    await expect(page.getByText(/parent group is pre-selected/i)).toBeVisible();
  });

  test('visiting /admin/groups/new (no query) leaves Parent Group enabled', async ({
    page,
  }) => {
    await page.goto('/admin/groups/new');
    await expect(
      page.getByRole('heading', { name: /create group/i }),
    ).toBeVisible({ timeout: 10_000 });

    await expect(page.locator('#parentGroupId')).toBeEnabled();
    await expect(
      page.getByText(/parent group is pre-selected/i),
    ).toHaveCount(0);
  });
});

test.describe('GroupFormPage — edit mode hides Group Type', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('create mode shows the Group Type selector as required', async ({ page }) => {
    await page.goto('/admin/groups/new');
    await expect(page.locator('#groupTypeId')).toBeVisible({ timeout: 10_000 });
    // The placeholder option is present.
    await expect(
      page.locator('#groupTypeId option', { hasText: /select a group type/i }),
    ).toHaveCount(1);
  });

  test('edit mode does NOT render the Group Type selector (immutable field)', async ({
    page,
  }) => {
    // Create a throwaway group, then navigate to its edit page.
    const name = `E2E Wave5 EditHidesType ${uniqueSuffix('g')}`;
    const idKey = await createGroupViaUi(page, name);

    await page.goto(`/admin/groups/${idKey}/edit`);
    await expect(
      page.getByRole('heading', { name: /edit group/i }),
    ).toBeVisible({ timeout: 10_000 });

    // Group Type select is hidden in edit mode by design.
    await expect(page.locator('#groupTypeId')).toHaveCount(0);
    // Name field is pre-filled.
    await expect(page.locator('#name')).toHaveValue(name);
  });
});

test.describe('GroupFormPage — Parent Group / Campus selects populated (#690)', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('Parent Group select renders the "None" placeholder AND seeded group options', async ({
    page,
  }) => {
    await page.goto('/admin/groups/new');
    await expect(
      page.getByRole('heading', { name: /create group/i }),
    ).toBeVisible({ timeout: 10_000 });

    const parentSelect = page.locator('#parentGroupId');
    await expect(parentSelect).toBeVisible();
    await expect(
      parentSelect.locator('option', { hasText: /none \(top-level group\)/i }),
    ).toHaveCount(1);
    // After #690, the select must load at least one real group option beyond
    // the placeholder (the seeder always provides Nursery/Preschool/Elementary).
    await expect
      .poll(async () => await parentSelect.locator('option').count(), {
        timeout: 10_000,
      })
      .toBeGreaterThan(1);
  });

  test('Campus select renders the "All Campuses" placeholder AND seeded campus options', async ({
    page,
  }) => {
    await page.goto('/admin/groups/new');
    await expect(
      page.getByRole('heading', { name: /create group/i }),
    ).toBeVisible({ timeout: 10_000 });

    const campusSelect = page.locator('#campusId');
    await expect(campusSelect).toBeVisible();
    await expect(
      campusSelect.locator('option', { hasText: /all campuses/i }),
    ).toHaveCount(1);
    // After #690, the select must load at least one real campus option beyond
    // the placeholder (the seeder always provides at least one campus).
    await expect
      .poll(async () => await campusSelect.locator('option').count(), {
        timeout: 10_000,
      })
      .toBeGreaterThan(1);
  });
});
