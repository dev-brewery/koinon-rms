/**
 * E2E Tests: Authorized Pickup Management (#683)
 *
 * Verifies that the Authorized Pickups + Pickup History sections on a person's
 * detail page render, and that the add/edit/remove/history flows work.
 *
 * Uses API route mocking (same pattern as person-notes.spec.ts) so the tests
 * are deterministic and do not require a running backend.
 *
 * Coverage:
 *   - Authorized Pickups + Pickup History sections render on the detail page
 *   - Empty state renders when no pickups exist
 *   - Add pickup via dialog → appears in list
 *   - Edit pickup → changes persist
 *   - Remove pickup → disappears from list
 *   - Pickup History panel renders (empty state OK)
 */
import { test, expect, type Page } from '@playwright/test';

// IdKey for person ID 1 (little-endian Base64 URL-safe encoding)
const PERSON_ID_KEY = 'AQAAAA';
const PICKUP_ID_KEY_1 = 'AgAAAA';
const PICKUP_ID_KEY_2 = 'AwAAAA';
const PERSON_DETAIL_URL = `/admin/people/${PERSON_ID_KEY}`;

// ---------------------------------------------------------------------------
// Auth helpers
// ---------------------------------------------------------------------------

const FAKE_ACCESS_TOKEN =
  'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9' +
  '.eyJzdWIiOiIxIiwibmFtZSI6IkpvaG4gU21pdGgiLCJpZEtleSI6IkFRQUFBQSIsImV4cCI6OTk5OTk5OTk5OX0' +
  '.dummy-signature';

const MOCK_REFRESH_RESPONSE = {
  data: {
    accessToken: FAKE_ACCESS_TOKEN,
    refreshToken: 'dummy-refresh-token',
    expiresAt: '2099-01-01T00:00:00.000Z',
  },
};

async function injectAuthToken(page: Page) {
  await page.addInitScript((token: string) => {
    localStorage.setItem('koinon_access_token', token);
    localStorage.setItem('koinon_refresh_token', 'dummy-refresh-token');
  }, FAKE_ACCESS_TOKEN);
}

// ---------------------------------------------------------------------------
// Page data mocks
// ---------------------------------------------------------------------------

const MOCK_PERSON = {
  data: {
    idKey: PERSON_ID_KEY,
    guid: '33333333-3333-3333-3333-333333333333',
    firstName: 'Emma',
    nickName: 'Emma',
    middleName: null,
    lastName: 'Smith',
    fullName: 'Emma Smith',
    birthDate: '2018-06-15',
    age: 7,
    gender: 'Female',
    email: null,
    isEmailActive: true,
    emailPreference: 'EmailAllowed',
    phoneNumbers: [],
    recordStatus: null,
    connectionStatus: null,
    title: null,
    suffix: null,
    maritalStatus: null,
    anniversaryDate: null,
    isDeceased: false,
    primaryFamily: { idKey: 'AQAAAA', name: 'Smith', memberCount: 4 },
    primaryCampus: null,
    photoId: null,
    photoUrl: null,
    createdDateTime: '2026-03-26T21:57:12.326Z',
    modifiedDateTime: null,
  },
};

const MOCK_FAMILY = {
  data: {
    family: { idKey: 'AQAAAA', name: 'Smith', memberCount: 1 },
    members: [],
  },
};

const MOCK_GROUPS_EMPTY = {
  data: [],
  meta: { page: 1, pageSize: 25, totalCount: 0, totalPages: 0 },
};

const MOCK_NOTES_EMPTY = { data: [] };

const MOCK_ATTENDANCE = { data: [] };
const MOCK_GIVING_SUMMARY = {
  data: {
    yearToDateTotal: 0,
    lastContributionDate: null,
    recentContributions: [],
  },
};
const MOCK_COMMUNICATION_PREFS = { data: [] };

// Authorized pickup payloads — note the { data: ... } envelope matches backend.
function makePickup(idKey: string, name: string, relationship = 0, authorizationLevel = 0) {
  return {
    idKey,
    childIdKey: PERSON_ID_KEY,
    childName: 'Emma Smith',
    authorizedPersonIdKey: undefined,
    authorizedPersonName: undefined,
    name,
    phoneNumber: '(555) 123-4567',
    relationship,
    authorizationLevel,
    photoUrl: undefined,
    isActive: true,
  };
}

const INITIAL_PICKUP = makePickup(PICKUP_ID_KEY_1, 'Sarah Smith', 0, 0);

// ---------------------------------------------------------------------------
// Stateful pickup list — lets tests exercise add/edit/remove against an
// in-memory store of pickups. Reset per test via resetPickupStore().
// ---------------------------------------------------------------------------

type Pickup = ReturnType<typeof makePickup>;
let pickupStore: Pickup[] = [];

function resetPickupStore(initial: Pickup[] = []) {
  pickupStore = [...initial];
}

async function mockPersonDetailRoutes(page: Page) {
  // Auth refresh
  await page.route('**/api/v1/auth/refresh', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_REFRESH_RESPONSE),
    }),
  );

  // Notifications (avoid unmocked 500s polluting the console)
  await page.route('**/api/v1/notifications/**', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ data: { count: 0 } }),
    }),
  );

  // Family — must be declared BEFORE the plain person route, because routes
  // register newest-first and we need the more specific pattern to match first.
  await page.route(`**/api/v1/people/${PERSON_ID_KEY}/family`, (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_FAMILY),
    }),
  );

  // Groups
  await page.route(`**/api/v1/people/${PERSON_ID_KEY}/groups*`, (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_GROUPS_EMPTY),
    }),
  );

  // Notes
  await page.route(`**/api/v1/people/${PERSON_ID_KEY}/notes*`, (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_NOTES_EMPTY),
    }),
  );

  // Communication preferences
  await page.route(
    `**/api/v1/people/${PERSON_ID_KEY}/communication-preferences`,
    (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(MOCK_COMMUNICATION_PREFS),
      }),
  );

  // Attendance history
  await page.route(`**/api/v1/people/${PERSON_ID_KEY}/attendance*`, (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_ATTENDANCE),
    }),
  );

  // Giving summary
  await page.route(
    `**/api/v1/people/${PERSON_ID_KEY}/giving-summary`,
    (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(MOCK_GIVING_SUMMARY),
      }),
  );

  // Pickup history (empty)
  await page.route(
    `**/api/v1/people/${PERSON_ID_KEY}/pickup-history*`,
    (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ data: [] }),
      }),
  );

  // ---- Authorized pickup routes (stateful) ----
  // List / create
  await page.route(
    `**/api/v1/people/${PERSON_ID_KEY}/authorized-pickups`,
    async (route) => {
      const request = route.request();
      if (request.method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ data: pickupStore }),
        });
        return;
      }

      if (request.method() === 'POST') {
        const body = JSON.parse(request.postData() || '{}');
        const newPickup = makePickup(
          PICKUP_ID_KEY_2,
          body.name ?? 'Unnamed',
          body.relationship ?? 0,
          body.authorizationLevel ?? 0,
        );
        newPickup.phoneNumber = body.phoneNumber ?? newPickup.phoneNumber;
        pickupStore = [...pickupStore, newPickup];
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({ data: newPickup }),
        });
        return;
      }

      await route.fallback();
    },
  );

  // Update / Delete by pickupIdKey
  await page.route(
    '**/api/v1/authorized-pickups/*',
    async (route) => {
      const request = route.request();
      const url = new URL(request.url());
      const pickupIdKey = url.pathname.split('/').pop() ?? '';

      if (request.method() === 'PUT') {
        const body = JSON.parse(request.postData() || '{}');
        pickupStore = pickupStore.map((p) =>
          p.idKey === pickupIdKey
            ? {
                ...p,
                relationship: body.relationship ?? p.relationship,
                authorizationLevel:
                  body.authorizationLevel ?? p.authorizationLevel,
              }
            : p,
        );
        const updated = pickupStore.find((p) => p.idKey === pickupIdKey);
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ data: updated }),
        });
        return;
      }

      if (request.method() === 'DELETE') {
        pickupStore = pickupStore.filter((p) => p.idKey !== pickupIdKey);
        await route.fulfill({
          status: 204,
          body: '',
        });
        return;
      }

      await route.fallback();
    },
  );

  // Person detail — MUST be last so more specific patterns above win.
  await page.route(`**/api/v1/people/${PERSON_ID_KEY}`, (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_PERSON),
    }),
  );
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

test.describe('Authorized Pickup (#683)', () => {
  test('renders Authorized Pickups and Pickup History sections with empty states', async ({
    page,
  }) => {
    resetPickupStore([]);
    await injectAuthToken(page);
    await mockPersonDetailRoutes(page);

    await page.goto(PERSON_DETAIL_URL);

    // Page loaded
    await expect(
      page.getByRole('heading', { name: 'Emma Smith', level: 1 }),
    ).toBeVisible({ timeout: 10000 });

    // Both sections exist with their test ids
    await expect(page.getByTestId('authorized-pickups-section')).toBeVisible();
    await expect(page.getByTestId('pickup-history-section')).toBeVisible();

    // Authorized Pickups empty state
    await expect(
      page.getByRole('heading', { name: /Authorized Pickups for Emma Smith/i }),
    ).toBeVisible();
    await expect(page.getByText('No authorized pickups')).toBeVisible();
    await expect(
      page.getByRole('button', { name: 'Add Authorized Pickup' }),
    ).toBeVisible();

    // Pickup History empty state
    await expect(
      page.getByRole('heading', { name: /Pickup History for Emma Smith/i }),
    ).toBeVisible();
    await expect(page.getByText('No pickup history')).toBeVisible();
  });

  test('adds a new authorized pickup via the dialog', async ({ page }) => {
    resetPickupStore([]);
    await injectAuthToken(page);
    await mockPersonDetailRoutes(page);

    await page.goto(PERSON_DETAIL_URL);
    await expect(
      page.getByRole('heading', { name: 'Emma Smith', level: 1 }),
    ).toBeVisible({ timeout: 10000 });

    // Open dialog
    await page.getByRole('button', { name: 'Add Authorized Pickup' }).click();
    await expect(
      page.getByRole('heading', { name: 'Add Authorized Pickup' }),
    ).toBeVisible();

    // Fill form
    await page.getByLabel(/^Name/).fill('Sarah Smith');
    await page.getByLabel(/Phone Number/).fill('(555) 123-4567');
    await page.getByLabel('Relationship').selectOption('0'); // Parent

    // Wait for the POST round-trip and the refetch, then the new entry appears.
    const createRequest = page.waitForResponse(
      (res) =>
        res.url().includes(`/people/${PERSON_ID_KEY}/authorized-pickups`) &&
        res.request().method() === 'POST' &&
        res.status() === 201,
    );
    await page.getByRole('button', { name: /^Add$/ }).click();
    await createRequest;

    // New pickup shows in list
    await expect(page.getByText('Sarah Smith')).toBeVisible({ timeout: 5000 });
    await expect(page.getByText('Always Authorized')).toBeVisible();
    await expect(page.getByText('No authorized pickups')).toHaveCount(0);
  });

  test('edits an existing pickup and persists changes', async ({ page }) => {
    // Start with a pickup already in the store (authorization = Always/0)
    resetPickupStore([INITIAL_PICKUP]);
    await injectAuthToken(page);
    await mockPersonDetailRoutes(page);

    await page.goto(PERSON_DETAIL_URL);
    await expect(
      page.getByRole('heading', { name: 'Emma Smith', level: 1 }),
    ).toBeVisible({ timeout: 10000 });

    // The initial pickup renders
    await expect(page.getByText('Sarah Smith')).toBeVisible();
    await expect(page.getByText('Always Authorized')).toBeVisible();

    // Click Edit
    await page.getByRole('button', { name: 'Edit' }).click();
    await expect(
      page.getByRole('heading', { name: 'Edit Authorized Pickup' }),
    ).toBeVisible();

    // Change authorization to "Emergency Only" via radio
    await page.getByRole('radio', { name: /Emergency Only/i }).check();

    const updateRequest = page.waitForResponse(
      (res) =>
        res.url().includes(`/authorized-pickups/${PICKUP_ID_KEY_1}`) &&
        res.request().method() === 'PUT' &&
        res.status() === 200,
    );
    await page.getByRole('button', { name: /^Update$/ }).click();
    await updateRequest;

    // List now reflects the new authorization level
    await expect(page.getByText('Emergency Only')).toBeVisible({ timeout: 5000 });
    await expect(page.getByText('Always Authorized')).toHaveCount(0);
  });

  test('removes a pickup from the list', async ({ page }) => {
    resetPickupStore([INITIAL_PICKUP]);
    await injectAuthToken(page);
    await mockPersonDetailRoutes(page);

    // Auto-accept the native confirm() dialog
    page.on('dialog', (dialog) => dialog.accept());

    await page.goto(PERSON_DETAIL_URL);
    await expect(
      page.getByRole('heading', { name: 'Emma Smith', level: 1 }),
    ).toBeVisible({ timeout: 10000 });

    // Pickup renders
    await expect(page.getByText('Sarah Smith')).toBeVisible();

    // Click Remove
    const deleteRequest = page.waitForResponse(
      (res) =>
        res.url().includes(`/authorized-pickups/${PICKUP_ID_KEY_1}`) &&
        res.request().method() === 'DELETE' &&
        res.status() === 204,
    );
    await page.getByRole('button', { name: /^Remove$/ }).click();
    await deleteRequest;

    // List is empty again
    await expect(page.getByText('Sarah Smith')).toHaveCount(0);
    await expect(page.getByText('No authorized pickups')).toBeVisible({
      timeout: 5000,
    });
  });
});
