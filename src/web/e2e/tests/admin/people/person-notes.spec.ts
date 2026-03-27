/**
 * E2E Smoke Tests: Person Notes Section
 * Branch: feature/issue-486-notes-interaction-log
 *
 * Verifies that the NotesSection component renders on the person detail page.
 * Uses API mocking to avoid auth setup and ensure deterministic state.
 *
 * What is tested:
 *   - Person detail page loads with Notes section present
 *   - Empty-state message "No notes yet" renders when no notes exist
 *   - "Add Note" button is visible in the Notes section header
 *   - Notes section renders note cards when notes data is returned
 */

import { test, expect, type Page } from '@playwright/test';

// IdKey for person ID 1 (little-endian Base64 URL-safe encoding)
const PERSON_ID_KEY = 'AQAAAA';
const PERSON_DETAIL_URL = `/admin/people/${PERSON_ID_KEY}`;
const API_BASE = 'http://localhost:5000/api/v1';

// ---------------------------------------------------------------------------
// Minimal mock response payloads with correct shape for each API consumer
// ---------------------------------------------------------------------------

// getPersonById: get<{ data: PersonDetailDto }> → component uses response.data
const MOCK_PERSON = {
  data: {
    idKey: PERSON_ID_KEY,
    guid: '33333333-3333-3333-3333-333333333333',
    firstName: 'John',
    nickName: 'John',
    middleName: null,
    lastName: 'Smith',
    fullName: 'John Smith',
    birthDate: '1985-06-15',
    age: 40,
    gender: 'Male',
    email: 'john.smith@example.com',
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

// getPersonFamily: get<{ data: PersonFamilyResponse }> → returns response.data
// Outer envelope: { data: PersonFamilyResponse }
const MOCK_FAMILY = {
  data: {
    family: { idKey: 'AQAAAA', name: 'Smith', memberCount: 1 },
    members: [],
  },
};

// getPersonGroups: get<PagedResult<PersonGroupMembershipDto>> → returns result directly (no .data)
const MOCK_GROUPS_EMPTY = {
  data: [],
  meta: { page: 1, pageSize: 25, totalCount: 0, totalPages: 0 },
};

// getPersonNotes: get<{ data: PagedResult<NoteDto> }> → returns response.data
// Outer envelope: { data: PagedResult }
const makeNotesPayload = (notes: object[]) => ({
  data: {
    data: notes,
    meta: { page: 1, pageSize: 25, totalCount: notes.length, totalPages: notes.length > 0 ? 1 : 0 },
  },
});

const MOCK_NOTES_EMPTY = makeNotesPayload([]);

const MOCK_NOTE = {
  idKey: 'AQAAAA',
  noteTypeName: 'General',
  noteTypeValueIdKey: 'general',
  text: 'Attended the newcomers lunch on Sunday.',
  isPrivate: false,
  isAlert: false,
  noteDateTime: '2026-03-20T14:30:00.000Z',
  authorPersonName: 'Jane Doe',
};

const MOCK_NOTES_WITH_DATA = makeNotesPayload([MOCK_NOTE]);

// getPersonAttendance: get<{ data: AttendanceSummaryDto[] }> → returns response.data
// Outer envelope: { data: AttendanceSummaryDto[] }
const MOCK_ATTENDANCE = { data: [] };

// getPersonGivingSummary: get<{ data: PersonGivingSummaryDto }> → returns response.data
// Outer envelope: { data: PersonGivingSummaryDto }
const MOCK_GIVING_SUMMARY = {
  data: {
    yearToDateTotal: 0,
    lastContributionDate: null,
    recentContributions: [],
  },
};

// getCommunicationPreferences: get<{ data: CommunicationPreferenceDto[] }> → returns response.data
// Return empty array wrapped correctly
const MOCK_COMMUNICATION_PREFS = { data: [] };

// Auth refresh — called by AuthContext on mount. Shape: { accessToken, refreshToken, expiresAt }
// The access token just needs to look like a JWT (header.payload.sig) so extractRolesFromJwt
// can parse it without throwing. Payload is a valid base64 JSON object.
//
// Computed offline: base64url({ alg: "HS256", typ: "JWT" }) = eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9
// Payload: base64url({ sub: "1", name: "John Smith", idKey: "AQAAAA", exp: 9999999999 })
//          = eyJzdWIiOiIxIiwibmFtZSI6IkpvaG4gU21pdGgiLCJpZEtleSI6IkFRQUFBQSIsImV4cCI6OTk5OTk5OTk5OX0
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

// ---------------------------------------------------------------------------
// Setup helpers
// ---------------------------------------------------------------------------

/**
 * Inject tokens into localStorage before the SPA loads so the API client
 * includes an Authorization header and AuthContext finds a token on mount.
 */
async function injectAuthToken(page: Page) {
  await page.addInitScript((token: string) => {
    localStorage.setItem('koinon_access_token', token);
    localStorage.setItem('koinon_refresh_token', 'dummy-refresh-token');
  }, FAKE_ACCESS_TOKEN);
}

/**
 * Register all route handlers needed by PersonDetailPage.
 * Pass the desired notes payload (empty or with data) per test.
 */
async function mockPersonDetailRoutes(
  page: Page,
  notesPayload: ReturnType<typeof makeNotesPayload>,
) {
  // Auth refresh — must succeed so AuthContext marks session as authenticated
  await page.route(`${API_BASE}/auth/refresh`, (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_REFRESH_RESPONSE),
    }),
  );

  // Person detail
  await page.route(`${API_BASE}/people/${PERSON_ID_KEY}`, (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_PERSON),
    }),
  );

  // Family — must be before groups glob to avoid ambiguity
  await page.route(`${API_BASE}/people/${PERSON_ID_KEY}/family`, (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_FAMILY),
    }),
  );

  // Groups (with optional query params)
  await page.route(`${API_BASE}/people/${PERSON_ID_KEY}/groups`, (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_GROUPS_EMPTY),
    }),
  );

  // Notes (with page/pageSize query params)
  await page.route(`**/people/${PERSON_ID_KEY}/notes*`, (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(notesPayload),
    }),
  );

  // Communication preferences
  await page.route(`${API_BASE}/people/${PERSON_ID_KEY}/communication-preferences`, (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_COMMUNICATION_PREFS),
    }),
  );

  // Attendance history
  await page.route(`**/people/${PERSON_ID_KEY}/attendance*`, (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_ATTENDANCE),
    }),
  );

  // Giving summary
  await page.route(`${API_BASE}/people/${PERSON_ID_KEY}/giving-summary`, (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_GIVING_SUMMARY),
    }),
  );
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

test.describe('Person Notes Section', () => {
  test('renders Notes heading and empty state when no notes exist', async ({ page }) => {
    await injectAuthToken(page);
    await mockPersonDetailRoutes(page, MOCK_NOTES_EMPTY);

    await page.goto(PERSON_DETAIL_URL);

    // Wait for person name — confirms the detail page rendered successfully
    await expect(page.getByRole('heading', { name: 'John Smith' })).toBeVisible({
      timeout: 10000,
    });

    // Notes section heading must be present
    await expect(page.getByRole('heading', { name: /^Notes/i, level: 2 })).toBeVisible();

    // Empty state message
    await expect(page.getByText('No notes yet')).toBeVisible();

    // Add Note button
    await expect(page.getByRole('button', { name: /Add Note/i })).toBeVisible();
  });

  test('renders note cards when notes are present', async ({ page }) => {
    await injectAuthToken(page);
    await mockPersonDetailRoutes(page, MOCK_NOTES_WITH_DATA);

    await page.goto(PERSON_DETAIL_URL);

    await expect(page.getByRole('heading', { name: 'John Smith' })).toBeVisible({
      timeout: 10000,
    });

    // Notes section heading (count badge appended with span, so partial match)
    await expect(page.getByRole('heading', { name: /Notes/i, level: 2 })).toBeVisible();

    // Note card content
    await expect(page.getByText('Attended the newcomers lunch on Sunday.')).toBeVisible();

    // Note type badge
    await expect(page.getByText('General')).toBeVisible();

    // Author attribution
    await expect(page.getByText('Jane Doe')).toBeVisible();
  });

  test('Add Note button opens the inline note form', async ({ page }) => {
    await injectAuthToken(page);
    await mockPersonDetailRoutes(page, MOCK_NOTES_EMPTY);

    await page.goto(PERSON_DETAIL_URL);

    await expect(page.getByRole('heading', { name: 'John Smith' })).toBeVisible({
      timeout: 10000,
    });

    // Click Add Note
    await page.getByRole('button', { name: /Add Note/i }).click();

    // The inline add form should appear with its own heading
    await expect(page.getByRole('heading', { name: /Add Note/i })).toBeVisible();

    // Note type dropdown — scoped to the add-note form to avoid strict mode ambiguity
    await expect(page.locator('form').getByRole('combobox')).toBeVisible();

    // Cancel button returns to empty state
    await page.getByRole('button', { name: /Cancel/i }).click();
    await expect(page.getByText('No notes yet')).toBeVisible();
  });
});
