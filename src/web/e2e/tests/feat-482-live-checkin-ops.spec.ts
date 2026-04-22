/**
 * E2E Tests: Feature #482 — Live Check-in Operations Dashboard
 *
 * Covers all 5 functional ACs from the 2026-04-20 scope-lock:
 *   1. Real-time room headcounts (5s polling)
 *   2. Room open/close toggle
 *   3. Capacity warnings (green / yellow / red / grey pill)
 *   4. Search within checked-in attendees
 *   5. Summary stats card (total / present / checked-out)
 *
 * The page is at /admin/checkin/operations and polls
 * /api/v1/checkin-operations/dashboard every 5s. We wait for those responses
 * deterministically with page.waitForResponse rather than page.waitForTimeout.
 */

import { test, expect } from '../fixtures/auth.fixture';

const DASHBOARD_URL_RE = /\/checkin-operations\/dashboard(\?|$)/;

test.describe('Live Check-in Operations Dashboard (#482)', () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test('dashboard page loads with page + summary + room list testids', async ({ page }) => {
    const firstLoad = page.waitForResponse(
      (res) => DASHBOARD_URL_RE.test(res.url()) && res.status() === 200,
      { timeout: 15000 }
    );
    await page.goto('/admin/checkin/operations');
    await firstLoad;

    await expect(page.getByTestId('checkin-ops-page')).toBeVisible({ timeout: 10000 });
    await expect(page.getByTestId('checkin-ops-summary')).toBeVisible();
    await expect(page.getByTestId('checkin-ops-room-list')).toBeVisible();
  });

  test('AC5: summary card shows total / present / checked-out counts', async ({ page }) => {
    const firstLoad = page.waitForResponse(
      (res) => DASHBOARD_URL_RE.test(res.url()) && res.status() === 200,
      { timeout: 15000 }
    );
    await page.goto('/admin/checkin/operations');
    await firstLoad;

    await expect(page.getByTestId('checkin-ops-summary-total')).toBeVisible();
    await expect(page.getByTestId('checkin-ops-summary-present')).toBeVisible();
    await expect(page.getByTestId('checkin-ops-summary-checkedout')).toBeVisible();

    // Values are numeric strings.
    const totalText = (await page.getByTestId('checkin-ops-summary-total').textContent()) ?? '';
    expect(totalText.trim()).toMatch(/^\d+$/);
  });

  test('AC1: dashboard polls every 5s (second response arrives without navigation)', async ({ page }) => {
    const firstLoad = page.waitForResponse(
      (res) => DASHBOARD_URL_RE.test(res.url()) && res.status() === 200,
      { timeout: 15000 }
    );
    await page.goto('/admin/checkin/operations');
    await firstLoad;
    await expect(page.getByTestId('checkin-ops-page')).toBeVisible();

    // The next poll should arrive within ~6s (5s interval + latency margin).
    const secondPoll = await page.waitForResponse(
      (res) => DASHBOARD_URL_RE.test(res.url()) && res.status() === 200,
      { timeout: 10000 }
    );
    expect(secondPoll.ok()).toBe(true);
  });

  test('AC3: capacity pill renders with a valid color for each room card (if any)', async ({ page }) => {
    const firstLoad = page.waitForResponse(
      (res) => DASHBOARD_URL_RE.test(res.url()) && res.status() === 200,
      { timeout: 15000 }
    );
    await page.goto('/admin/checkin/operations');
    await firstLoad;

    const roomCards = page.getByTestId('checkin-ops-room-card');
    const cardCount = await roomCards.count();
    if (cardCount === 0) {
      // No rooms seeded with capacity — empty state is acceptable per spec.
      await expect(page.getByTestId('checkin-ops-room-list')).toBeVisible();
      return;
    }

    const firstPill = roomCards.first().getByTestId('checkin-ops-capacity-pill');
    await expect(firstPill).toBeVisible();
    const color = await firstPill.getAttribute('data-color');
    expect(['green', 'yellow', 'red', 'grey']).toContain(color ?? '');
  });

  test('AC2: clicking the room toggle flips is-open and triggers a toggle + refresh', async ({ page }) => {
    const firstLoad = page.waitForResponse(
      (res) => DASHBOARD_URL_RE.test(res.url()) && res.status() === 200,
      { timeout: 15000 }
    );
    await page.goto('/admin/checkin/operations');
    await firstLoad;

    const roomCards = page.getByTestId('checkin-ops-room-card');
    const cardCount = await roomCards.count();
    if (cardCount === 0) {
      test.skip();
      return;
    }

    const firstCard = roomCards.first();
    const initialIsOpen = await firstCard.getAttribute('data-is-open');
    const toggleBtn = firstCard.getByTestId('checkin-ops-room-toggle');
    await expect(toggleBtn).toBeVisible();

    const togglePost = page.waitForResponse(
      (res) => /\/checkin-operations\/rooms\/.+\/toggle$/.test(res.url()) && res.request().method() === 'POST',
      { timeout: 15000 }
    );
    const refreshAfterToggle = page.waitForResponse(
      (res) => DASHBOARD_URL_RE.test(res.url()) && res.status() === 200,
      { timeout: 15000 }
    );

    await toggleBtn.click();
    const toggleResponse = await togglePost;
    expect(toggleResponse.ok()).toBe(true);
    await refreshAfterToggle;

    // data-is-open on the same card (by locationIdKey) should flip.
    const locationIdKey = await firstCard.getAttribute('data-location-idkey');
    expect(locationIdKey).toBeTruthy();
    const sameCard = page.locator(
      `[data-testid="checkin-ops-room-card"][data-location-idkey="${locationIdKey}"]`
    );
    await expect
      .poll(async () => await sameCard.getAttribute('data-is-open'), { timeout: 15000 })
      .not.toBe(initialIsOpen);

    // Toggle back so subsequent runs start from the same state.
    const togglePost2 = page.waitForResponse(
      (res) => /\/checkin-operations\/rooms\/.+\/toggle$/.test(res.url()) && res.request().method() === 'POST',
      { timeout: 15000 }
    );
    await sameCard.getByTestId('checkin-ops-room-toggle').click();
    await togglePost2;
  });

  test('AC4: attendee search filters the rows client-side', async ({ page }) => {
    const firstLoad = page.waitForResponse(
      (res) => DASHBOARD_URL_RE.test(res.url()) && res.status() === 200,
      { timeout: 15000 }
    );
    await page.goto('/admin/checkin/operations');
    await firstLoad;

    const searchInput = page.getByTestId('checkin-ops-search');
    await expect(searchInput).toBeVisible();

    const rows = page.getByTestId('checkin-ops-attendee-row');
    const initialCount = await rows.count();

    // Type something extremely unlikely to match any real attendee.
    await searchInput.fill('zzzzzzznomatch_9x8y');

    // Either no rows remain, or the empty-state is shown.
    await expect
      .poll(async () => {
        const c = await rows.count();
        const empty = await page.getByTestId('checkin-ops-attendee-empty').isVisible().catch(() => false);
        return c === 0 || empty;
      }, { timeout: 5000 })
      .toBe(true);

    // Clearing restores rows.
    await searchInput.fill('');
    await expect
      .poll(async () => await rows.count(), { timeout: 5000 })
      .toBe(initialCount);
  });
});
