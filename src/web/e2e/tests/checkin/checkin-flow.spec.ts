import { test, expect, type Page } from '@playwright/test';
import { CheckinPage } from '../../fixtures/page-objects/checkin.page';
import { testData } from '../../fixtures/test-data';

// ---------------------------------------------------------------------------
// Mock data & helpers
// ---------------------------------------------------------------------------

const SMITH_FAMILY_ID = 'fam_smith_abc123';
const PERSON_JOHNNY_ID = 'per_johnny_111';
const PERSON_JENNY_ID = 'per_jenny_222';

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

const opportunitiesResponse = {
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
            groupIdKey: 'grp_elementary_bbb',
            groupName: testData.groups.elementary.name,
            locations: [
              {
                locationIdKey: 'loc_room201_yyy',
                locationName: 'Room 201',
                currentCount: 5,
                schedules: [
                  {
                    scheduleIdKey: 'sch_sun9am_zzz',
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

const checkinResult = {
  results: [
    {
      success: true,
      attendanceIdKey: 'att_johnny_001',
      person: {
        idKey: PERSON_JOHNNY_ID,
        fullName: testData.people.johnnySmith.fullName,
        firstName: testData.people.johnnySmith.firstName,
        lastName: testData.people.johnnySmith.lastName,
      },
      location: { idKey: 'loc_room201_yyy', name: 'Room 201', fullPath: 'Main > Room 201' },
      securityCode: 'ABC123',
      checkInTime: '2026-03-29T08:55:00Z',
    },
  ],
  successCount: 1,
  failureCount: 0,
  allSucceeded: true,
};

/**
 * Set up API mocks so checkin tests work without a live backend.
 * Mirrors setupCheckinMocks from checkin-complete-flow.spec.ts.
 */
async function setupMocks(page: Page) {
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
    })
  );

  await page.route('**/api/v1/families/search*', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(smithSearchResponse),
    })
  );

  await page.route('**/api/v1/checkin/opportunities/**', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(opportunitiesResponse),
    })
  );

  await page.route('**/api/v1/checkin/attendance', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(checkinResult),
    })
  );

  await page.route('**/api/v1/checkin/labels/**', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ data: [] }),
    })
  );
}

test.describe('Check-in Flow', () => {
  test.beforeEach(async ({ page }) => {
    await setupMocks(page);
    const checkin = new CheckinPage(page);
    await checkin.goto();
  });

  test('should display phone input on load', async ({ page }) => {
    const checkin = new CheckinPage(page);

    await expect(checkin.phoneInput).toBeVisible();
    await expect(checkin.phoneInput).toBeFocused();
  });

  test('should find family by phone number', async ({ page }) => {
    const checkin = new CheckinPage(page);

    await checkin.searchByPhone('5551234567');
    await expect(checkin.familyMemberCards.first()).toBeVisible();
  });

  test('should display multiple family members', async ({ page }) => {
    const checkin = new CheckinPage(page);

    await checkin.searchByPhone('5551234567');

    const memberCount = await checkin.familyMemberCards.count();
    expect(memberCount).toBeGreaterThan(0);
  });

  test('should allow selecting a family member', async ({ page }) => {
    const checkin = new CheckinPage(page);

    await checkin.searchByPhone('5551234567');
    await checkin.selectMember(0);

    // Member should show selected state
    await expect(checkin.familyMemberCards.first()).toHaveAttribute('data-selected', 'true');
  });

  test('@smoke should complete full check-in flow', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Search for family
    await checkin.searchByPhone('5551234567');

    // Select first member
    await checkin.selectMember(0);

    // Confirm check-in
    await checkin.confirmCheckin();

    // Verify success
    await checkin.expectSuccess();
  });

  test('should handle idle timeout warning', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Dev mode warning fires at 8s of inactivity. Wait for it with enough margin.
    await expect(checkin.idleWarningModal).toBeVisible({ timeout: 15000 });

    // Dismiss and verify it closes
    await checkin.dismissIdleWarning();
  });

  test('should show error for non-existent phone', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Override search mock to return empty results
    await page.route('**/api/v1/families/search*', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ data: [] }),
      })
    );

    await checkin.enterPhone('0000000000');
    await checkin.submitPhone();

    // Should show no results message
    await expect(page.getByText(/no family found|not found/i)).toBeVisible();
  });

  test('should validate phone number format', async ({ page }) => {
    const checkin = new CheckinPage(page);

    await checkin.enterPhone('123'); // Too short
    await checkin.submitPhone();

    // Should show validation error (PhoneSearch validates 10+ digits client-side)
    await expect(page.getByText(/valid phone|10 digits/i)).toBeVisible();
  });
});

test.describe('Check-in Kiosk Mode', () => {
  test.use({ viewport: { width: 1024, height: 768 }, hasTouch: true });

  test('should have touch-friendly buttons', async ({ page }) => {
    await setupMocks(page);
    const checkin = new CheckinPage(page);
    await checkin.goto();

    // Type something to hide search mode toggles, then clear — ensures only submit button matches
    await checkin.enterPhone('5551234567');

    // Check button has minimum touch target size (48px)
    const buttonBox = await checkin.searchButton.boundingBox();
    expect(buttonBox?.height).toBeGreaterThanOrEqual(48);
    expect(buttonBox?.width).toBeGreaterThanOrEqual(48);
  });
});
