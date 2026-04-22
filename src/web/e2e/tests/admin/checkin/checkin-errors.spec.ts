/**
 * E2E Tests: Check-in Error Scenarios
 * Tests error handling for no family, duplicates, etc.
 *
 * All tests use API mocks so they run reliably without live backend data.
 */

import { test, expect, type Page } from '@playwright/test';
import { CheckinPage } from '../../../fixtures/page-objects/checkin.page';
import { testData } from '../../../fixtures/test-data';

// ---------------------------------------------------------------------------
// Shared mock data
// ---------------------------------------------------------------------------

const SMITH_FAMILY_ID = 'fam_smith_abc123';
const PERSON_JOHNNY_ID = 'per_johnny_111';

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

const checkinSuccessResult = {
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

/** Base mocks: configuration endpoint to prevent page from hanging */
async function setupBaseMocks(page: Page) {
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
}

test.describe('Check-in Error Scenarios', () => {
  test.beforeEach(async ({ page }) => {
    await setupBaseMocks(page);
    const checkin = new CheckinPage(page);
    await checkin.goto();
  });

  test('should show error for non-existent phone number', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Mock empty search result for non-existent phone
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
    await expect(
      page.getByText(/no family found|not found|doesn't exist/i)
    ).toBeVisible();

    // Phone input should still be visible to try again
    await expect(checkin.phoneInput).toBeVisible();
    await expect(checkin.phoneInput).toHaveValue('0000000000');
  });

  test('should validate phone number format', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Test invalid formats — PhoneSearch validates 10+ digits client-side
    const invalidNumbers = ['123', '12345', 'abcdefghij', '555-123-456'];

    for (const invalid of invalidNumbers) {
      await checkin.enterPhone(invalid);
      await checkin.submitPhone();

      // Should show validation error
      await expect(
        page.getByText(/invalid phone|10 digits|valid number/i)
      ).toBeVisible();

      // Clear for next test
      await checkin.phoneInput.clear();
    }
  });

  test('should handle duplicate check-in attempt', async ({ page }) => {
    const checkin = new CheckinPage(page);
    let checkinAttempt = 0;

    // Full mocks with duplicate detection on second check-in
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

    await page.route('**/api/v1/checkin/attendance', (route) => {
      checkinAttempt++;
      if (checkinAttempt === 1) {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(checkinSuccessResult),
        });
      } else {
        route.fulfill({
          status: 409,
          contentType: 'application/json',
          body: JSON.stringify({
            error: {
              code: 'ALREADY_CHECKED_IN',
              message: 'Johnny Smith was already checked in at 8:55 AM',
            },
          }),
        });
      }
    });

    await page.route('**/api/v1/checkin/labels/**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ data: [] }),
      })
    );

    // First check-in
    await checkin.searchByPhone('5551234567');
    await checkin.selectMember(0);
    await checkin.confirmCheckin();
    await expect(checkin.successMessage).toBeVisible();

    // Attempt duplicate check-in
    await checkin.goto(); // Reset
    await checkin.searchByPhone('5551234567');
    await checkin.selectMember(0); // Same member
    await checkin.confirmCheckin();

    // Should show already checked in error
    await expect(
      page.getByText(/already checked in|duplicate|recently checked/i)
    ).toBeVisible();

    // Should show time of previous check-in
    await expect(page.getByText(/checked in at|last check-in/i)).toBeVisible();
  });

  test('should handle network timeout', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Intercept and hang API response to trigger app timeout logic
    await page.route('**/api/v1/families/search*', async (_route) => {
      // Never respond - forces app to handle timeout internally
      // App should have timeout logic (e.g., AbortController with 5s timeout)
    });

    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();

    // Should show timeout error from app's timeout handler
    await expect(
      page.getByText(/timeout|taking too long|try again/i)
    ).toBeVisible({ timeout: 15000 });

    // Should offer retry
    await expect(
      page.getByRole('button', { name: /retry|try again/i })
    ).toBeVisible();
  });

  test('should handle server error (500)', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Mock server error
    await page.route('**/api/v1/families/search*', (route) =>
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Internal Server Error' }),
      })
    );

    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();

    // Should show friendly error message
    await expect(
      page.getByText(/something went wrong|server error|try again later/i)
    ).toBeVisible();

    // Should not expose technical details
    await expect(page.getByText(/500|internal server/i)).not.toBeVisible();
  });

  test('should handle API validation error (400)', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Mock validation error
    await page.route('**/api/v1/families/search*', (route) =>
      route.fulfill({
        status: 400,
        contentType: 'application/json',
        body: JSON.stringify({
          error: 'Validation failed',
          details: { phone: 'Invalid phone format' },
        }),
      })
    );

    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();

    // Should show validation error
    await expect(
      page.getByText(/invalid phone format|validation failed/i)
    ).toBeVisible();
  });

  test('should handle empty family (no members)', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Mock family with no members
    await page.route('**/api/v1/families/search*', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          data: [
            {
              idKey: 'fam_empty_001',
              name: 'Empty Family',
              members: [],
            },
          ],
        }),
      })
    );

    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();

    // Should show no members message
    await expect(
      page.getByText(/no family members|no one to check in/i)
    ).toBeVisible();
  });

  test('should handle check-in API failure', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Allow search to succeed
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

    await checkin.searchByPhone('5551234567');
    await checkin.selectMember(0);

    // Mock check-in failure
    await page.route('**/api/v1/checkin/attendance', (route) =>
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Check-in failed' }),
      })
    );

    await checkin.confirmCheckin();

    // Should show error
    await expect(
      page.getByText(/check-in failed|couldn't check in/i)
    ).toBeVisible();

    // Member should remain selected to retry
    const selectedCard = checkin.familyMemberCards.first();
    await expect(selectedCard).toHaveAttribute('data-selected', 'true');
  });

  test('should handle malformed API response', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Mock invalid JSON
    await page.route('**/api/v1/families/search*', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: 'invalid json{',
      })
    );

    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();

    // Should show error
    await expect(
      page.getByText(/something went wrong|error loading/i)
    ).toBeVisible();
  });

  test('should handle member ineligible for check-in', async ({ page }) => {
    const checkin = new CheckinPage(page);

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

    await checkin.searchByPhone('5551234567');

    // Mock member with ineligible status
    await page.route('**/api/v1/checkin/attendance', (route) =>
      route.fulfill({
        status: 409,
        contentType: 'application/json',
        body: JSON.stringify({
          error: 'Member is not eligible',
          reason: 'Too young for this service',
        }),
      })
    );

    await checkin.selectMember(0);
    await checkin.confirmCheckin();

    // Should show eligibility error
    await expect(
      page.getByText(/not eligible|too young/i)
    ).toBeVisible();
  });

  test('should clear error on new search', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Mock: empty for 0000000000, success for 5551234567
    await page.route('**/api/v1/families/search*', (route) => {
      const url = route.request().url();
      if (url.includes('0000000000')) {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ data: [] }),
        });
      } else {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(smithSearchResponse),
        });
      }
    });

    await page.route('**/api/v1/checkin/opportunities/**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(opportunitiesResponse),
      })
    );

    // Trigger error
    await checkin.enterPhone('0000000000');
    await checkin.submitPhone();
    await expect(page.getByText(/not found/i)).toBeVisible();

    // Clear and search again
    await checkin.phoneInput.clear();
    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();

    // Error should be gone
    await expect(page.getByText(/not found/i)).not.toBeVisible();

    // Results should be visible
    await expect(checkin.familyMemberCards.first()).toBeVisible();
  });

  test('should retry failed request', async ({ page }) => {
    const checkin = new CheckinPage(page);

    let attemptCount = 0;

    // Fail first attempt, succeed on second
    await page.route('**/api/v1/families/search*', (route) => {
      attemptCount++;
      if (attemptCount === 1) {
        route.abort('failed');
      } else {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(smithSearchResponse),
        });
      }
    });

    await page.route('**/api/v1/checkin/opportunities/**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(opportunitiesResponse),
      })
    );

    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();

    // Should show error
    await expect(page.getByText(/error|failed/i)).toBeVisible();

    // Click retry
    await page.getByRole('button', { name: /retry|try again/i }).click();

    // Should succeed
    await expect(checkin.familyMemberCards.first()).toBeVisible();
    expect(attemptCount).toBe(2);
  });

  test('@smoke should recover from all error types', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Set up conditional mocks
    await page.route('**/api/v1/families/search*', (route) => {
      const url = route.request().url();
      if (url.includes('0000000000')) {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ data: [] }),
        });
      } else {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(smithSearchResponse),
        });
      }
    });

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
        body: JSON.stringify(checkinSuccessResult),
      })
    );

    await page.route('**/api/v1/checkin/labels/**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ data: [] }),
      })
    );

    // Error 1: Invalid phone (client-side validation)
    await checkin.enterPhone('123');
    await checkin.submitPhone();
    await expect(page.getByText(/invalid/i)).toBeVisible();

    // Recover
    await checkin.phoneInput.clear();

    // Error 2: Not found
    await checkin.enterPhone('0000000000');
    await checkin.submitPhone();
    await expect(page.getByText(/not found/i)).toBeVisible();

    // Recover and succeed
    await checkin.phoneInput.clear();
    await checkin.searchByPhone('5551234567');
    await expect(checkin.familyMemberCards.first()).toBeVisible();

    // Complete check-in successfully
    await checkin.selectMember(0);
    await checkin.confirmCheckin();
    await expect(checkin.successMessage).toBeVisible();
  });
});

test.describe('Check-in Error Accessibility', () => {
  test.beforeEach(async ({ page }) => {
    await setupBaseMocks(page);
  });

  test('should announce errors to screen readers', async ({ page }) => {
    const checkin = new CheckinPage(page);

    await page.route('**/api/v1/families/search*', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ data: [] }),
      })
    );

    await checkin.goto();
    await checkin.enterPhone('0000000000');
    await checkin.submitPhone();

    // Error should be in aria-live region
    const errorMessage = page.getByText(/not found/i);
    await expect(errorMessage).toBeVisible();

    // Check for aria-live or role=alert containing the error text
    const liveRegion = page.locator('[role="alert"]:has-text("not found"), [aria-live]:has-text("not found")').first();
    await expect(liveRegion).toBeVisible();
  });

  test('should focus error message or input on error', async ({ page }) => {
    const checkin = new CheckinPage(page);

    await checkin.goto();
    await checkin.enterPhone('123');
    await checkin.submitPhone();

    // Phone input should be focused to correct error
    await expect(checkin.phoneInput).toBeFocused();
  });

  test('should have proper ARIA labels on error elements', async ({ page }) => {
    const checkin = new CheckinPage(page);

    await page.route('**/api/v1/families/search*', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ data: [] }),
      })
    );

    await checkin.goto();
    await checkin.enterPhone('0000000000');
    await checkin.submitPhone();

    // Error message should be associated with input
    const errorId = await page
      .getByText(/not found/i)
      .getAttribute('id');
    if (errorId) {
      await expect(checkin.phoneInput).toHaveAttribute(
        'aria-describedby',
        new RegExp(errorId)
      );
    }
  });
});

test.describe('Check-in Error Metrics', () => {
  test.beforeEach(async ({ page }) => {
    await setupBaseMocks(page);
  });

  test('should log error for monitoring', async ({ page }) => {
    const checkin = new CheckinPage(page);
    const errors: string[] = [];

    // Capture console errors
    page.on('console', (msg) => {
      if (msg.type() === 'error') {
        errors.push(msg.text());
      }
    });

    // Trigger error
    await page.route('**/api/v1/families/search*', (route) =>
      route.abort('failed')
    );

    await checkin.goto();
    await checkin.enterPhone('5551234567');
    await checkin.submitPhone();

    // Should have logged error (for monitoring/telemetry)
    // Note: This assumes error logging is implemented
    expect(errors.length).toBeGreaterThan(0);
  });

  test('should track error metrics', async ({ page }) => {
    const checkin = new CheckinPage(page);

    await checkin.goto();

    // Check if analytics/monitoring is tracking errors
    const metricsTracked = await page.evaluate(() => {
      return typeof (window as { trackError?: () => void }).trackError === 'function';
    });

    if (metricsTracked) {
      // Mock error
      await page.route('**/api/v1/families/search*', (route) =>
        route.abort('failed')
      );

      await checkin.enterPhone('5551234567');
      await checkin.submitPhone();

      // Verify tracking called
      const tracked = await page.evaluate(() => {
        return (window as { errorTrackingCalled?: boolean }).errorTrackingCalled || false;
      });

      expect(tracked).toBeTruthy();
    }
  });
});
