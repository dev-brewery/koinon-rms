/**
 * E2E Tests: Complete Check-in Flow
 *
 * Covers the full kiosk check-in journey end-to-end with mocked API
 * responses so the test suite runs without a live backend.
 *
 * Flow under test (from CheckinPage.tsx):
 *   search → (select-family) → select-members → confirmation → done
 *
 * API endpoints mocked:
 *   POST /api/v1/checkin/search
 *   GET  /api/v1/checkin/opportunities/:familyIdKey
 *   POST /api/v1/checkin/attendance
 *   GET  /api/v1/checkin/labels/:attendanceIdKey
 *   POST /api/v1/checkin/supervisor/login
 *   POST /api/v1/checkin/supervisor/logout
 *   GET  /api/v1/checkin/roster (supervisor attendance)
 *   GET  /api/v1/checkin/configuration
 */

import { test, expect, type Page } from '@playwright/test';
import { CheckinPage } from '../../fixtures/page-objects/checkin.page';
import { testData } from '../../fixtures/test-data';

// ---------------------------------------------------------------------------
// Shared mock data
// ---------------------------------------------------------------------------

const SMITH_FAMILY_ID = 'fam_smith_abc123';
const JOHNSON_FAMILY_ID = 'fam_johnson_def456';
const PERSON_JOHNNY_ID = 'per_johnny_111';
const PERSON_JENNY_ID = 'per_jenny_222';
const PERSON_BOB_ID = 'per_bob_333';
const GROUP_PRESCHOOL_ID = 'grp_preschool_aaa';
const GROUP_ELEMENTARY_ID = 'grp_elementary_bbb';
const LOC_ROOM101_ID = 'loc_room101_xxx';
const LOC_ROOM201_ID = 'loc_room201_yyy';
const SCHED_SUNDAY_9AM_ID = 'sch_sun9am_zzz';
const ATT_JOHNNY_ID = 'att_johnny_001';
const ATT_JENNY_ID = 'att_jenny_002';

/** Single-family search response — Smith family */
const smithSearchResponse = {
  data: [
    {
      idKey: SMITH_FAMILY_ID,
      name: testData.families.smith.name,
      members: [
        {
          idKey: PERSON_JOHNNY_ID,
          firstName: testData.people.johnnySmith.firstName,
          lastName: testData.people.johnnySmith.lastName,
          fullName: testData.people.johnnySmith.fullName,
          age: testData.people.johnnySmith.age,
          hasCriticalAllergies: false,
        },
        {
          idKey: PERSON_JENNY_ID,
          firstName: testData.people.jennySmith.firstName,
          lastName: testData.people.jennySmith.lastName,
          fullName: testData.people.jennySmith.fullName,
          age: testData.people.jennySmith.age,
          allergies: testData.people.jennySmith.allergies,
          hasCriticalAllergies: testData.people.jennySmith.hasCriticalAllergies,
        },
      ],
    },
  ],
};

/** Two-family search response — used to exercise the select-family step */
const multipleSearchResponse = {
  data: [
    ...smithSearchResponse.data,
    {
      idKey: JOHNSON_FAMILY_ID,
      name: testData.families.johnson.name,
      members: [
        {
          idKey: PERSON_BOB_ID,
          firstName: testData.people.bobJohnson.firstName,
          lastName: testData.people.bobJohnson.lastName,
          fullName: testData.people.bobJohnson.fullName,
          age: testData.people.bobJohnson.age,
          hasCriticalAllergies: false,
        },
      ],
    },
  ],
};

/** Opportunities for the Smith family */
const smithOpportunitiesResponse = {
  data: {
    family: smithSearchResponse.data[0],
    opportunities: [
      {
        person: {
          idKey: PERSON_JOHNNY_ID,
          firstName: testData.people.johnnySmith.firstName,
          lastName: testData.people.johnnySmith.lastName,
          fullName: testData.people.johnnySmith.fullName,
          age: testData.people.johnnySmith.age,
          hasCriticalAllergies: false,
        },
        currentAttendance: [],
        availableOptions: [
          {
            groupIdKey: GROUP_ELEMENTARY_ID,
            groupName: testData.groups.elementary.name,
            locations: [
              {
                locationIdKey: LOC_ROOM201_ID,
                locationName: 'Room 201',
                currentCount: 5,
                schedules: [
                  {
                    scheduleIdKey: SCHED_SUNDAY_9AM_ID,
                    scheduleName: testData.schedules.sunday9am.name,
                    startTime: '2026-03-29T09:00:00Z',
                    isSelected: true,
                  },
                ],
              },
            ],
          },
        ],
      },
      {
        person: {
          idKey: PERSON_JENNY_ID,
          firstName: testData.people.jennySmith.firstName,
          lastName: testData.people.jennySmith.lastName,
          fullName: testData.people.jennySmith.fullName,
          age: testData.people.jennySmith.age,
          allergies: testData.people.jennySmith.allergies,
          hasCriticalAllergies: testData.people.jennySmith.hasCriticalAllergies,
        },
        currentAttendance: [],
        availableOptions: [
          {
            groupIdKey: GROUP_PRESCHOOL_ID,
            groupName: testData.groups.preschool.name,
            locations: [
              {
                locationIdKey: LOC_ROOM101_ID,
                locationName: 'Room 101',
                currentCount: 3,
                schedules: [
                  {
                    scheduleIdKey: SCHED_SUNDAY_9AM_ID,
                    scheduleName: testData.schedules.sunday9am.name,
                    startTime: '2026-03-29T09:00:00Z',
                    isSelected: true,
                  },
                ],
              },
            ],
          },
        ],
      },
    ],
  },
};

/** Batch check-in result for a single child (Johnny) */
const singleCheckinResult = {
  results: [
    {
      success: true,
      attendanceIdKey: ATT_JOHNNY_ID,
      person: {
        idKey: PERSON_JOHNNY_ID,
        fullName: testData.people.johnnySmith.fullName,
        firstName: testData.people.johnnySmith.firstName,
        lastName: testData.people.johnnySmith.lastName,
      },
      location: { idKey: LOC_ROOM201_ID, name: 'Room 201', fullPath: 'Main > Room 201' },
      securityCode: 'ABC123',
      checkInTime: '2026-03-29T08:55:00Z',
    },
  ],
  successCount: 1,
  failureCount: 0,
  allSucceeded: true,
};

/** Batch check-in result for two children (Johnny + Jenny) */
const twoChildrenCheckinResult = {
  results: [
    {
      success: true,
      attendanceIdKey: ATT_JOHNNY_ID,
      person: {
        idKey: PERSON_JOHNNY_ID,
        fullName: testData.people.johnnySmith.fullName,
        firstName: testData.people.johnnySmith.firstName,
        lastName: testData.people.johnnySmith.lastName,
      },
      location: { idKey: LOC_ROOM201_ID, name: 'Room 201', fullPath: 'Main > Room 201' },
      securityCode: 'ABC123',
      checkInTime: '2026-03-29T08:55:00Z',
    },
    {
      success: true,
      attendanceIdKey: ATT_JENNY_ID,
      person: {
        idKey: PERSON_JENNY_ID,
        fullName: testData.people.jennySmith.fullName,
        firstName: testData.people.jennySmith.firstName,
        lastName: testData.people.jennySmith.lastName,
      },
      location: { idKey: LOC_ROOM101_ID, name: 'Room 101', fullPath: 'Main > Room 101' },
      securityCode: 'XYZ789',
      checkInTime: '2026-03-29T08:55:00Z',
    },
  ],
  successCount: 2,
  failureCount: 0,
  allSucceeded: true,
};

/** Label data returned after a successful check-in */
const labelResponse = {
  data: [
    {
      type: 'child',
      content: '^XA^FO50,50^FDJohnny Smith^FS^XZ',
      format: 'zpl',
      fields: { name: 'Johnny Smith', securityCode: 'ABC123' },
    },
  ],
};

/** Supervisor roster for a location */
const supervisorRosterResponse = {
  data: {
    locationIdKey: LOC_ROOM201_ID,
    locationName: 'Room 201',
    children: [
      {
        attendanceIdKey: ATT_JOHNNY_ID,
        personIdKey: PERSON_JOHNNY_ID,
        fullName: testData.people.johnnySmith.fullName,
        firstName: testData.people.johnnySmith.firstName,
        lastName: testData.people.johnnySmith.lastName,
        hasCriticalAllergies: false,
        securityCode: 'ABC123',
        checkInTime: '2026-03-29T08:55:00Z',
        isFirstTime: false,
      },
    ],
    totalCount: 1,
    generatedAt: '2026-03-29T08:55:00Z',
    isAtCapacity: false,
    isNearCapacity: false,
  },
};

/** Valid supervisor login response — built fresh each call so expiresAt is never stale */
function buildSupervisorLoginResponse() {
  return {
    sessionToken: 'test-supervisor-token-abc',
    supervisor: {
      idKey: 'sup_abc123',
      fullName: 'Test Supervisor',
      firstName: 'Test',
      lastName: 'Supervisor',
    },
    expiresAt: new Date(Date.now() + 120_000).toISOString(),
  };
}

// ---------------------------------------------------------------------------
// Helper: wire up the full set of API mocks needed for a happy-path check-in
// ---------------------------------------------------------------------------

async function setupCheckinMocks(
  page: Page,
  opts: {
    searchResponse?: unknown;
    checkinResult?: unknown;
  } = {}
) {
  const searchResp = opts.searchResponse ?? smithSearchResponse;
  const checkinResp = opts.checkinResult ?? singleCheckinResult;

  // Block configuration so the page doesn't hang on a missing backend
  await page.route('**/api/v1/checkin/configuration**', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ data: { campus: { idKey: 'campus1', name: 'Main Campus' }, areas: [], activeSchedules: [], serverTime: new Date().toISOString() } }),
    })
  );

  // Search (POST /checkin/search)
  await page.route('**/api/v1/families/search*', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(searchResp),
    })
  );

  // Opportunities (GET /checkin/opportunities/:familyIdKey)
  await page.route('**/api/v1/checkin/opportunities/**', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(smithOpportunitiesResponse),
    })
  );

  // Attendance / check-in (POST /checkin/attendance)
  await page.route('**/api/v1/checkin/attendance', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(checkinResp),
    })
  );

  // Labels (GET /checkin/labels/:attendanceIdKey)
  await page.route('**/api/v1/checkin/labels/**', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(labelResponse),
    })
  );
}

// ---------------------------------------------------------------------------
// Helper: wire supervisor-specific mocks
// ---------------------------------------------------------------------------

async function setupSupervisorMocks(page: Page) {
  await page.route('**/api/v1/checkin/supervisor/login', async (route) => {
    const body = JSON.parse(route.request().postData() ?? '{}') as { pin?: string };
    if (body.pin === '1234') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(buildSupervisorLoginResponse()),
      });
    } else {
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Invalid PIN' }),
      });
    }
  });

  await page.route('**/api/v1/checkin/supervisor/logout', (route) =>
    route.fulfill({ status: 204 })
  );

  await page.route('**/api/v1/checkin/supervisor/reprint/**', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ labels: labelResponse.data }),
    })
  );

  await page.route('**/api/v1/checkin/roster**', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(supervisorRosterResponse),
    })
  );
}

// ---------------------------------------------------------------------------
// Helper: authenticate supervisor through the PIN modal
// ---------------------------------------------------------------------------

async function authenticateSupervisor(page: Page) {
  await page.getByRole('button', { name: /supervisor/i }).click();
  await page.getByRole('button', { name: '1' }).click();
  await page.getByRole('button', { name: '2' }).click();
  await page.getByRole('button', { name: '3' }).click();
  await page.getByRole('button', { name: '4' }).click();
  await page.getByRole('button', { name: 'Submit' }).click();
  await expect(page.getByText('Supervisor Mode')).toBeVisible({ timeout: 5000 });
}

// ===========================================================================
// 1. Complete Check-in Flow
// ===========================================================================

test.describe('Complete Check-in Flow', () => {
  test.beforeEach(async ({ page }) => {
    await setupCheckinMocks(page);
    const checkin = new CheckinPage(page);
    await checkin.goto();
  });

  test('@smoke happy path — phone search → select member → check in → security code shown → done resets', async ({
    page,
  }) => {
    const checkin = new CheckinPage(page);

    // Step 1: Search by phone — single family auto-advances to member selection
    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();

    // Because smithSearchResponse has 1 family, CheckinPage auto-advances to
    // select-members step — the member list should be visible
    await expect(page.getByText(/who.*checking in/i)).toBeVisible({ timeout: 5000 });

    // Step 2: Select Johnny (first member card)
    await checkin.selectMember(0);

    // Confirm button should now be active (1 activity selected)
    await expect(checkin.confirmButton).toBeEnabled();

    // Step 3: Submit check-in
    await checkin.confirmCheckin();

    // Step 4: Confirmation screen
    await checkin.expectConfirmation();
    await expect(page.getByText('1 person checked in')).toBeVisible();

    // Step 5: Security code present
    const codes = await checkin.getSecurityCodes();
    expect(codes.length).toBeGreaterThan(0);
    expect(codes[0]).toMatch(/^[A-Z0-9]+$/);

    // Step 6: Done resets to search
    await checkin.doneButton.click();
    // After reset the phone search mode button should be back
    await expect(page.getByRole('button', { name: /search by phone/i })).toBeVisible();
  });

  test('selecting multiple members — both appear on confirmation', async ({ page }) => {
    // Override check-in result to return 2 children
    await page.route('**/api/v1/checkin/attendance', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(twoChildrenCheckinResult),
      })
    );

    const checkin = new CheckinPage(page);

    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();

    await expect(page.getByText(/who.*checking in/i)).toBeVisible({ timeout: 5000 });

    // Select both children
    await checkin.selectMember(0);
    await checkin.selectMember(1);

    await checkin.confirmCheckin();
    await checkin.expectConfirmation();

    // Both names on the confirmation screen
    await expect(page.getByText(testData.people.johnnySmith.fullName)).toBeVisible();
    await expect(page.getByText(testData.people.jennySmith.fullName)).toBeVisible();
    await expect(page.getByText('2 people checked in')).toBeVisible();
  });

  test('name search — same flow via name search mode', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Switch to name search mode
    await page.getByRole('button', { name: /search by name/i }).click();

    // The FamilySearch component renders a text input; fill and submit
    const nameInput = page.getByPlaceholder(/name|search/i).first();
    await nameInput.fill('Smith');
    await page.getByRole('button', { name: /search|find/i }).click();

    // Single family match → auto-advances
    await expect(page.getByText(/who.*checking in/i)).toBeVisible({ timeout: 5000 });

    await checkin.selectMember(0);
    await checkin.confirmCheckin();
    await checkin.expectConfirmation();
  });

  test('multiple families — select-family step shown, user picks one', async ({ page }) => {
    // Override search to return two families
    await page.route('**/api/v1/families/search*', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(multipleSearchResponse),
      })
    );

    const checkin = new CheckinPage(page);
    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();

    // select-family step: heading shown
    await expect(page.getByText('Select Your Family')).toBeVisible({ timeout: 5000 });

    // Family names rendered as h3 headings inside Card components
    await expect(page.getByRole('heading', { name: testData.families.smith.name })).toBeVisible();
    await expect(page.getByRole('heading', { name: testData.families.johnson.name })).toBeVisible();

    // Pick Smith family
    await page.getByRole('heading', { name: testData.families.smith.name }).click();

    // Should advance to member selection
    await expect(page.getByText(/who.*checking in/i)).toBeVisible({ timeout: 5000 });
  });

  test('no results — shows not-found message', async ({ page }) => {
    await page.route('**/api/v1/families/search*', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ data: [] }),
      })
    );

    const checkin = new CheckinPage(page);
    await checkin.enterPhone('0000000000');
    await checkin.submitPhone();

    await expect(page.getByText(/no families found/i)).toBeVisible();
  });

  test('API error during search — error message shown', async ({ page }) => {
    await page.route('**/api/v1/families/search*', (route) =>
      route.fulfill({ status: 500, contentType: 'application/json', body: '{}' })
    );

    const checkin = new CheckinPage(page);
    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();

    await expect(page.getByText(/search failed/i)).toBeVisible();
  });

  test('API error during check-in — friendly error message shown', async ({ page }) => {
    await page.route('**/api/v1/checkin/attendance', (route) =>
      route.fulfill({ status: 500, contentType: 'application/json', body: '{}' })
    );

    const checkin = new CheckinPage(page);
    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();
    await expect(page.getByText(/who.*checking in/i)).toBeVisible({ timeout: 5000 });

    await checkin.selectMember(0);
    await checkin.confirmCheckin();

    await expect(page.getByText(/check-in failed/i)).toBeVisible();
    // Should NOT navigate to confirmation
    await expect(checkin.checkInCompleteHeading).not.toBeVisible();
  });

  test('allergy warning visible for Jenny on member selection', async ({ page }) => {
    const checkin = new CheckinPage(page);
    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();
    await expect(page.getByText(/who.*checking in/i)).toBeVisible({ timeout: 5000 });

    // Jenny has a peanut allergy; the UI should surface some allergy indicator
    await expect(page.getByText(/peanut|allerg/i)).toBeVisible();
  });
});

// ===========================================================================
// 2. Security Codes
// ===========================================================================

test.describe('Security Codes', () => {
  test.beforeEach(async ({ page }) => {
    await setupCheckinMocks(page, { checkinResult: twoChildrenCheckinResult });
    const checkin = new CheckinPage(page);
    await checkin.goto();
  });

  test('security code is visible for each checked-in child on confirmation', async ({ page }) => {
    const checkin = new CheckinPage(page);

    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();
    await expect(page.getByText(/who.*checking in/i)).toBeVisible({ timeout: 5000 });

    await checkin.selectMember(0);
    await checkin.selectMember(1);
    await checkin.confirmCheckin();
    await checkin.expectConfirmation();

    // The label "Security Code" should appear once per checked-in child
    const labels = await page.getByText('Security Code').all();
    expect(labels.length).toBe(2);

    // The actual code values should be non-empty
    const codes = await checkin.getSecurityCodes();
    expect(codes.length).toBeGreaterThanOrEqual(2);
    codes.forEach((code) => expect(code).toMatch(/\S+/));
  });

  test('security codes are unique per checked-in child', async ({ page }) => {
    const checkin = new CheckinPage(page);

    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();
    await expect(page.getByText(/who.*checking in/i)).toBeVisible({ timeout: 5000 });

    await checkin.selectMember(0);
    await checkin.selectMember(1);
    await checkin.confirmCheckin();
    await checkin.expectConfirmation();

    const codes = await checkin.getSecurityCodes();
    // twoChildrenCheckinResult has ABC123 and XYZ789 — they must differ
    const unique = new Set(codes);
    expect(unique.size).toBe(codes.length);
  });

  test('security code present for single child check-in', async ({ page }) => {
    // Override with single result
    await page.route('**/api/v1/checkin/attendance', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(singleCheckinResult),
      })
    );

    const checkin = new CheckinPage(page);
    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();
    await expect(page.getByText(/who.*checking in/i)).toBeVisible({ timeout: 5000 });

    await checkin.selectMember(0);
    await checkin.confirmCheckin();
    await checkin.expectConfirmation();

    await expect(page.getByText('Security Code')).toBeVisible();
    // The specific code from mock data
    await expect(page.getByText('ABC123')).toBeVisible();
  });

  test('"Keep your security code" instructions shown on confirmation', async ({ page }) => {
    // Use single check-in result
    await page.route('**/api/v1/checkin/attendance', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(singleCheckinResult),
      })
    );

    const checkin = new CheckinPage(page);
    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();
    await expect(page.getByText(/who.*checking in/i)).toBeVisible({ timeout: 5000 });

    await checkin.selectMember(0);
    await checkin.confirmCheckin();
    await checkin.expectConfirmation();

    await expect(page.getByText(/keep your security code/i)).toBeVisible();
  });
});

// ===========================================================================
// 3. Offline Check-in
// ===========================================================================

test.describe('Offline Check-in', () => {
  test.beforeEach(async ({ page }) => {
    await setupCheckinMocks(page);
    const checkin = new CheckinPage(page);
    await checkin.goto();
    // Allow ServiceWorker / offline hooks to initialise
    await page.waitForLoadState('networkidle');
  });

  test('queues check-in when offline and shows queued status', async ({ page, context }) => {
    const checkin = new CheckinPage(page);

    // Search while online to pre-populate family data in React Query cache
    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();
    await expect(page.getByText(/who.*checking in/i)).toBeVisible({ timeout: 5000 });
    await checkin.selectMember(0);

    // Go offline
    await context.setOffline(true);

    // Attempt check-in
    await checkin.confirmCheckin();

    // The offline branch in CheckinPage shows a queued message instead of
    // navigating to the confirmation step
    await expect(
      page.getByText(/queued|pending|will sync/i)
    ).toBeVisible({ timeout: 5000 });

    // Confirmation heading should NOT be shown (we're in offline queue mode)
    await expect(checkin.checkInCompleteHeading).not.toBeVisible();

    await context.setOffline(false);
  });

  test('offline indicator visible when disconnected', async ({ page, context }) => {
    await context.setOffline(true);

    // Trigger a search to provoke network activity that the PWA status can detect
    const checkin = new CheckinPage(page);
    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();

    // OfflineIndicator or OfflineQueueIndicator should surface
    await expect(
      page.getByTestId('offline-indicator').or(page.getByText(/offline/i).first())
    ).toBeVisible({ timeout: 5000 });

    await context.setOffline(false);
  });

  test('syncs queued check-ins on reconnect', async ({ page, context }) => {
    const checkin = new CheckinPage(page);

    // Pre-cache: search and select while online
    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();
    await expect(page.getByText(/who.*checking in/i)).toBeVisible({ timeout: 5000 });
    await checkin.selectMember(0);

    // Go offline and queue
    await context.setOffline(true);
    await checkin.confirmCheckin();
    await expect(page.getByText(/queued|pending/i)).toBeVisible({ timeout: 5000 });

    // Reconnect
    await context.setOffline(false);

    // The OfflineQueueIndicator should eventually reflect synced/cleared state
    await expect(
      page
        .getByText(/synced|completed|sync/i)
        .or(page.getByTestId('offline-indicator').filter({ hasText: '' }))
    ).toBeVisible({ timeout: 10_000 });
  });

  test('check-in works offline with previously cached family data', async ({
    page,
    context,
  }) => {
    const checkin = new CheckinPage(page);

    // Cache data while online
    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();
    await expect(page.getByText(/who.*checking in/i)).toBeVisible({ timeout: 5000 });

    // Go offline and restart the flow from the search step
    await context.setOffline(true);
    await checkin.goto();

    // Should still be able to search (from React Query / ServiceWorker cache)
    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();

    // Family members should render from cache
    await expect(page.getByText(/who.*checking in/i)).toBeVisible({ timeout: 5000 });

    await context.setOffline(false);
  });
});

// ===========================================================================
// 4. Supervisor Mode
// ===========================================================================

test.describe('Supervisor Mode', () => {
  test.beforeEach(async ({ page }) => {
    await setupCheckinMocks(page);
    await setupSupervisorMocks(page);
    const checkin = new CheckinPage(page);
    await checkin.goto();
  });

  test('@smoke authenticate with PIN → supervisor panel opens', async ({ page }) => {
    await authenticateSupervisor(page);

    await expect(page.getByText('Supervisor Mode')).toBeVisible();
    await expect(page.getByText(/test supervisor/i)).toBeVisible();
  });

  test('supervisor panel shows current check-in roster', async ({ page }) => {
    await authenticateSupervisor(page);

    // Reprint Labels tab is the default; it shows "Current Check-Ins"
    await expect(page.getByText('Current Check-Ins')).toBeVisible();
    await expect(page.getByText(testData.people.johnnySmith.fullName)).toBeVisible();
  });

  test('supervisor can reprint labels from current check-in list', async ({ page }) => {
    await setupCheckinMocks(page, { checkinResult: singleCheckinResult });
    await authenticateSupervisor(page);

    // The reprint button for Johnny should exist in the roster
    const reprintButton = page.getByRole('button', { name: /reprint/i }).first();
    await expect(reprintButton).toBeVisible();
    await reprintButton.click();

    // No error thrown and UI does not crash (supervisor reprint API was mocked to 200)
    await expect(page.getByText('Supervisor Mode')).toBeVisible();
  });

  test('exit supervisor mode returns to kiosk search screen', async ({ page }) => {
    await authenticateSupervisor(page);

    await page.getByRole('button', { name: /exit supervisor/i }).click();

    await expect(page.getByText('Supervisor Mode')).not.toBeVisible();
    // Kiosk header supervisor button visible again
    await expect(page.getByRole('button', { name: /supervisor/i })).toBeVisible();
  });

  test('invalid PIN shows error', async ({ page }) => {
    await page.getByRole('button', { name: /supervisor/i }).click();

    // Enter wrong PIN (0000)
    for (let i = 0; i < 4; i++) {
      await page.getByRole('button', { name: '0' }).click();
    }
    await page.getByRole('button', { name: 'Submit' }).click();

    await expect(page.getByText(/invalid pin/i)).toBeVisible();
    // Supervisor mode should NOT activate
    await expect(page.getByText('Supervisor Mode')).not.toBeVisible();
  });

  test('supervisor checkout tab — security code entry visible', async ({ page }) => {
    await authenticateSupervisor(page);

    await page.getByRole('button', { name: 'Checkout' }).click();
    await expect(page.getByText(/security code/i)).toBeVisible();
  });

  test('supervisor page-parent tab — visible after tab switch', async ({ page }) => {
    await authenticateSupervisor(page);

    await page.getByRole('button', { name: 'Page Parent' }).click();
    await expect(page.getByText(/page parent/i)).toBeVisible();
  });
});

// ===========================================================================
// 5. Performance
// ===========================================================================

test.describe('Performance', () => {
  test('@perf online check-in completes in < 500ms (test env)', async ({ page }) => {
    await setupCheckinMocks(page);
    const checkin = new CheckinPage(page);
    await checkin.goto();

    // Warm up — navigate and wait for the page to settle
    await page.waitForLoadState('networkidle');

    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();
    await expect(page.getByText(/who.*checking in/i)).toBeVisible({ timeout: 5000 });

    await checkin.selectMember(0);

    // Measure from confirm click to confirmation screen
    const start = Date.now();
    await checkin.confirmCheckin();
    await checkin.expectConfirmation();
    const elapsed = Date.now() - start;

    // 500ms target in test env (backend is mocked so network latency is near zero)
    expect(elapsed).toBeLessThan(500);
  });

  test('@perf page load — search form interactive in < 2500ms', async ({ page }) => {
    await setupCheckinMocks(page);

    const start = Date.now();
    await page.goto('/checkin');
    // Phone input must be ready to type before we call it interactive
    await expect(page.getByRole('button', { name: /search by phone/i })).toBeVisible();
    const elapsed = Date.now() - start;

    expect(elapsed).toBeLessThan(2500);
  });

  test('@perf confirm button responds to click in < 200ms (INP)', async ({ page }) => {
    await setupCheckinMocks(page);
    const checkin = new CheckinPage(page);
    await checkin.goto();
    await page.waitForLoadState('networkidle');

    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();
    await expect(page.getByText(/who.*checking in/i)).toBeVisible({ timeout: 5000 });
    await checkin.selectMember(0);

    // Measure the time from click to the button entering its loading state,
    // which confirms the interaction was handled within the INP budget.
    const start = Date.now();
    await checkin.confirmCheckin();
    // The button enters a loading/disabled state almost immediately on click
    // (before the API responds). Confirm the UI responded quickly.
    await expect(checkin.confirmButton).toBeDisabled({ timeout: 200 });
    const elapsed = Date.now() - start;

    expect(elapsed).toBeLessThan(200);
  });
});

// ===========================================================================
// 6. Kiosk Touch Mode
// ===========================================================================

test.describe('Kiosk Touch Mode', () => {
  test.use({ viewport: { width: 1024, height: 768 }, hasTouch: true });

  test.beforeEach(async ({ page }) => {
    await setupCheckinMocks(page);
    const checkin = new CheckinPage(page);
    await checkin.goto();
  });

  test('confirm/check-in button meets minimum 48px touch target', async ({ page }) => {
    const checkin = new CheckinPage(page);

    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();
    await expect(page.getByText(/who.*checking in/i)).toBeVisible({ timeout: 5000 });
    await checkin.selectMember(0);

    const box = await checkin.confirmButton.boundingBox();
    expect(box?.height).toBeGreaterThanOrEqual(48);
  });

  test('done button meets minimum 48px touch target on confirmation', async ({ page }) => {
    const checkin = new CheckinPage(page);

    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();
    await expect(page.getByText(/who.*checking in/i)).toBeVisible({ timeout: 5000 });
    await checkin.selectMember(0);
    await checkin.confirmCheckin();
    await checkin.expectConfirmation();

    const box = await checkin.doneButton.boundingBox();
    expect(box?.height).toBeGreaterThanOrEqual(48);
  });

  test('@smoke touch-based happy path completes successfully', async ({ page }) => {
    const checkin = new CheckinPage(page);

    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();
    await expect(page.getByText(/who.*checking in/i)).toBeVisible({ timeout: 5000 });

    // Simulate tap on first member card
    const members = await checkin.familyMemberCards.all();
    if (members[0]) {
      await members[0].tap();
    }

    await checkin.confirmButton.tap();
    await checkin.expectConfirmation();

    await expect(page.getByText('Security Code')).toBeVisible();
  });
});
