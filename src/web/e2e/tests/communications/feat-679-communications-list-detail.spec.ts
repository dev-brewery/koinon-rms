/**
 * E2E Tests: Issue #679 (Wave 3) — Communications list + detail interactions
 *
 * Covers gaps left by feat-490 (email) and feat-491 (SMS):
 *  - Applying the status filter actually changes the page state (not just
 *    that options are present).
 *  - List row click-through is exercised via a mocked communication so the
 *    test passes regardless of seeder state.
 *  - Detail-page Reschedule modal opens and closes (no reschedule sent).
 *  - Detail page renders message body, metadata sidebar, and recipients heading.
 *
 * Strategy: use Playwright route interception to serve a synthetic communication
 * to the detail page. This avoids creating a real communication (which would
 * exercise the outbound-send path) and keeps the spec deterministic even on a
 * fresh database where no communications exist.
 */

import { test, expect, type Page, type Route } from '../../fixtures/auth.fixture';

// ---------------------------------------------------------------------------
// Fixture: a scheduled communication detail response
// ---------------------------------------------------------------------------

const MOCK_SCHEDULED_ID_KEY = 'E2E679A';
const MOCK_SENT_ID_KEY = 'E2E679B';

function buildScheduledCommunication(idKey: string) {
  const futureIso = new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString();
  return {
    data: {
      idKey,
      communicationType: 'Email',
      status: 'Scheduled',
      subject: 'E2E Wave 3 scheduled communication',
      body: '<p>Hello {{FirstName}}, this is an E2E fixture message.</p>',
      fromName: 'E2E Wave 3',
      fromEmail: 'e2e-wave3@example.com',
      replyToEmail: 'noreply@example.com',
      note: 'E2E fixture note for wave 3 list+detail coverage.',
      scheduledDateTime: futureIso,
      sentDateTime: null,
      createdDateTime: new Date(Date.now() - 3600_000).toISOString(),
      modifiedDateTime: null,
      recipientCount: 2,
      deliveredCount: 0,
      failedCount: 0,
      openedCount: 0,
      recipients: [
        {
          idKey: `${idKey}R1`,
          personIdKey: 'P1',
          fullName: 'Alice Example',
          email: 'alice@example.com',
          phone: null,
          status: 'Pending',
          sentDateTime: null,
          deliveredDateTime: null,
          failedDateTime: null,
          failureReason: null,
          openedDateTime: null,
        },
        {
          idKey: `${idKey}R2`,
          personIdKey: 'P2',
          fullName: 'Bob Example',
          email: 'bob@example.com',
          phone: null,
          status: 'Pending',
          sentDateTime: null,
          deliveredDateTime: null,
          failedDateTime: null,
          failureReason: null,
          openedDateTime: null,
        },
      ],
    },
  };
}

function buildSentCommunication(idKey: string) {
  const pastIso = new Date(Date.now() - 3600_000).toISOString();
  return {
    data: {
      idKey,
      communicationType: 'Email',
      status: 'Sent',
      subject: 'E2E Wave 3 sent communication',
      body: '<p>Delivered fixture body.</p>',
      fromName: 'E2E Wave 3',
      fromEmail: 'e2e-wave3@example.com',
      replyToEmail: null,
      note: null,
      scheduledDateTime: null,
      sentDateTime: pastIso,
      createdDateTime: pastIso,
      modifiedDateTime: null,
      recipientCount: 1,
      deliveredCount: 1,
      failedCount: 0,
      openedCount: 0,
      recipients: [
        {
          idKey: `${idKey}R1`,
          personIdKey: 'P1',
          fullName: 'Carol Example',
          email: 'carol@example.com',
          phone: null,
          status: 'Delivered',
          sentDateTime: pastIso,
          deliveredDateTime: pastIso,
          failedDateTime: null,
          failureReason: null,
          openedDateTime: null,
        },
      ],
    },
  };
}

function buildListResponse(summary: {
  idKey: string;
  subject: string;
  status: string;
  communicationType: string;
  recipientCount: number;
  deliveredCount: number;
  failedCount: number;
  scheduledDateTime?: string | null;
  sentDateTime?: string | null;
}) {
  const now = new Date().toISOString();
  return {
    data: [
      {
        idKey: summary.idKey,
        communicationType: summary.communicationType,
        status: summary.status,
        subject: summary.subject,
        recipientCount: summary.recipientCount,
        deliveredCount: summary.deliveredCount,
        failedCount: summary.failedCount,
        openedCount: 0,
        scheduledDateTime: summary.scheduledDateTime ?? null,
        sentDateTime: summary.sentDateTime ?? null,
        createdDateTime: now,
        modifiedDateTime: null,
      },
    ],
    meta: { page: 1, pageSize: 50, totalCount: 1, totalPages: 1 },
  };
}

// ---------------------------------------------------------------------------
// Mocking helpers
// ---------------------------------------------------------------------------

/** Route-intercept the detail endpoint to return our fixture. */
async function mockDetailEndpoint(
  page: Page,
  idKey: string,
  payload: ReturnType<typeof buildScheduledCommunication>
): Promise<void> {
  await page.route(`**/api/v1/communications/${idKey}`, async (route: Route) => {
    if (route.request().method() !== 'GET') {
      await route.fallback();
      return;
    }
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(payload),
    });
  });
}

/** Route-intercept the list endpoint so we always have at least one visible row. */
async function mockListEndpoint(
  page: Page,
  payload: ReturnType<typeof buildListResponse>
): Promise<void> {
  await page.route('**/api/v1/communications?**', async (route: Route) => {
    if (route.request().method() !== 'GET') {
      await route.fallback();
      return;
    }
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(payload),
    });
  });
  // Also catch the no-query variant
  await page.route('**/api/v1/communications', async (route: Route) => {
    if (route.request().method() !== 'GET') {
      await route.fallback();
      return;
    }
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(payload),
    });
  });
}

// ---------------------------------------------------------------------------
// Communications List — filter application + click-through
// ---------------------------------------------------------------------------

test.describe('Communications List — filter + click-through (#679)', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('changing the status filter updates the select value via React state', async ({
    page,
  }) => {
    await page.goto('/admin/communications');
    await expect(page.getByRole('heading', { name: 'Communications' })).toBeVisible({
      timeout: 10000,
    });

    const filter = page.locator('#status-filter');
    await filter.selectOption('Sent');
    await expect(filter).toHaveValue('Sent');

    await filter.selectOption('Scheduled');
    await expect(filter).toHaveValue('Scheduled');

    // Switching back to All (empty value) is also valid
    await filter.selectOption('');
    await expect(filter).toHaveValue('');
  });

  test('clicking a communication row navigates to the detail page (mocked)', async ({ page }) => {
    // Seed the list with a synthetic sent communication
    await mockListEndpoint(
      page,
      buildListResponse({
        idKey: MOCK_SENT_ID_KEY,
        subject: 'E2E Wave 3 sent communication',
        status: 'Sent',
        communicationType: 'Email',
        recipientCount: 1,
        deliveredCount: 1,
        failedCount: 0,
        sentDateTime: new Date().toISOString(),
      })
    );
    await mockDetailEndpoint(page, MOCK_SENT_ID_KEY, buildSentCommunication(MOCK_SENT_ID_KEY));

    await page.goto('/admin/communications');
    const rowLink = page.locator(`a[href="/admin/communications/${MOCK_SENT_ID_KEY}"]`);
    await expect(rowLink).toBeVisible({ timeout: 10000 });
    await rowLink.click();

    await expect(page).toHaveURL(new RegExp(`/admin/communications/${MOCK_SENT_ID_KEY}$`));
    await expect(
      page.getByRole('heading', { name: 'E2E Wave 3 sent communication' })
    ).toBeVisible({ timeout: 10000 });
  });
});

// ---------------------------------------------------------------------------
// Communication Detail — rendering sections and reschedule modal
// ---------------------------------------------------------------------------

test.describe('Communication Detail — sections + reschedule modal (#679)', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('detail page renders subject heading, message body, and metadata sidebar', async ({
    page,
  }) => {
    await mockDetailEndpoint(
      page,
      MOCK_SENT_ID_KEY,
      buildSentCommunication(MOCK_SENT_ID_KEY)
    );

    await page.goto(`/admin/communications/${MOCK_SENT_ID_KEY}`);

    // Subject appears as the main heading
    await expect(
      page.getByRole('heading', { name: 'E2E Wave 3 sent communication' })
    ).toBeVisible({ timeout: 10000 });
    // Message section renders the body content
    await expect(page.getByRole('heading', { name: 'Message' })).toBeVisible();
    await expect(page.getByText('Delivered fixture body.')).toBeVisible();
    // Metadata sidebar shows From Name and From Email
    await expect(page.getByRole('heading', { name: 'Metadata' })).toBeVisible();
    await expect(page.getByText('E2E Wave 3', { exact: true })).toBeVisible();
    await expect(page.getByText('e2e-wave3@example.com')).toBeVisible();
  });

  test('detail page renders the recipients heading with the recipient count', async ({
    page,
  }) => {
    await mockDetailEndpoint(
      page,
      MOCK_SENT_ID_KEY,
      buildSentCommunication(MOCK_SENT_ID_KEY)
    );

    await page.goto(`/admin/communications/${MOCK_SENT_ID_KEY}`);
    await expect(page.getByRole('heading', { name: /^Recipients \(1\)$/ })).toBeVisible({
      timeout: 10000,
    });
  });

  test('scheduled communication shows scheduled banner and action buttons', async ({ page }) => {
    await mockDetailEndpoint(
      page,
      MOCK_SCHEDULED_ID_KEY,
      buildScheduledCommunication(MOCK_SCHEDULED_ID_KEY)
    );

    await page.goto(`/admin/communications/${MOCK_SCHEDULED_ID_KEY}`);
    await expect(page.getByText(/Scheduled to send:/)).toBeVisible({ timeout: 10000 });
    await expect(page.getByRole('button', { name: 'Cancel Schedule' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Reschedule' })).toBeVisible();
  });

  test('Reschedule button opens a modal with a datetime input and closes on Cancel', async ({
    page,
  }) => {
    await mockDetailEndpoint(
      page,
      MOCK_SCHEDULED_ID_KEY,
      buildScheduledCommunication(MOCK_SCHEDULED_ID_KEY)
    );

    await page.goto(`/admin/communications/${MOCK_SCHEDULED_ID_KEY}`);
    await page.getByRole('button', { name: 'Reschedule' }).click();

    // Modal content
    await expect(page.getByRole('heading', { name: 'Reschedule Communication' })).toBeVisible({
      timeout: 5000,
    });
    await expect(page.locator('#rescheduleDateTime')).toBeVisible();
    // Save disabled until a date is entered
    const save = page.getByRole('button', { name: 'Reschedule', exact: true }).last();
    await expect(save).toBeDisabled();

    // Cancel closes the modal
    await page.getByRole('button', { name: 'Cancel', exact: true }).last().click();
    await expect(page.getByRole('heading', { name: 'Reschedule Communication' })).toHaveCount(0, {
      timeout: 3000,
    });
  });

  test('detail page shows the back-navigation link to the communications list', async ({
    page,
  }) => {
    await mockDetailEndpoint(
      page,
      MOCK_SENT_ID_KEY,
      buildSentCommunication(MOCK_SENT_ID_KEY)
    );

    await page.goto(`/admin/communications/${MOCK_SENT_ID_KEY}`);
    const backLink = page.getByRole('link', { name: 'Back to communications' });
    await expect(backLink).toBeVisible({ timeout: 10000 });
    await backLink.click();
    await expect(page).toHaveURL(/\/admin\/communications$/);
    await expect(page.getByRole('heading', { name: 'Communications' })).toBeVisible();
  });

  test('detail page shows a friendly error state when the API returns 404', async ({ page }) => {
    // Return 404 for a synthetic idKey
    await page.route('**/api/v1/communications/NOTFOUND679', async (route) => {
      await route.fulfill({
        status: 404,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Not found' }),
      });
    });

    await page.goto('/admin/communications/NOTFOUND679');
    await expect(
      page.getByRole('heading', { name: 'Communication Not Found' })
    ).toBeVisible({ timeout: 10000 });
    await expect(page.getByRole('link', { name: 'Back to Communications' })).toBeVisible();
  });
});
