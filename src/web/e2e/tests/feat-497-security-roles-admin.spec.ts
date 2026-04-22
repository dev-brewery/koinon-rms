/**
 * E2E: feat(admin) #497 — Security role and permissions management UI
 *
 * Coverage:
 *   - AC1: Admin can view and create security roles
 *   - AC2: Admin can assign claims (permissions) to a role
 *   - AC3: Admin can add and remove people from a role
 *   - AC4: Changes take effect on the next request — no restart needed.
 *     This is proved by asserting that AFTER the test adds a DENY for
 *     financial:view to john.smith (via a fresh role), a second HTTP call
 *     to a financial:view-protected endpoint immediately returns 403.
 *     DENY-takes-precedence means the DB mutation alone is sufficient.
 */

import { expect, test } from '@playwright/test';
import type { Page, APIRequestContext } from '@playwright/test';

/**
 * Logs in via the UI without relying on the shared loginAsAdmin fixture,
 * which is coupled to an older dashboard route.
 */
async function loginAsAdmin(page: Page): Promise<void> {
  await page.goto('/login');
  await page.getByLabel('Email').fill('john.smith@example.com');
  await page.getByLabel('Password').fill('admin123');
  await page.getByRole('button', { name: 'Sign In' }).click();
  // After login the app redirects away from /login; tolerate either
  // "/dashboard" (legacy) or "/" (current default redirect).
  await expect(page).not.toHaveURL(/\/login$/, { timeout: 10_000 });
}

const API_BASE = 'http://localhost:5000/api/v1';

function uniqueSuffix(): string {
  return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 6)}`;
}

async function getAccessToken(request: APIRequestContext): Promise<string> {
  const response = await request.post(`${API_BASE}/auth/login`, {
    data: { email: 'john.smith@example.com', password: 'admin123' },
  });
  expect(response.ok()).toBeTruthy();
  const body = await response.json();
  return body.data.accessToken as string;
}

async function loginAsJohn(
  request: APIRequestContext,
): Promise<{ token: string; personIdKey: string }> {
  const response = await request.post(`${API_BASE}/auth/login`, {
    data: { email: 'john.smith@example.com', password: 'admin123' },
  });
  expect(response.ok()).toBeTruthy();
  const body = await response.json();
  return {
    token: body.data.accessToken as string,
    personIdKey: body.data.user.idKey as string,
  };
}

async function apiCreateRole(
  request: APIRequestContext,
  token: string,
  name: string,
): Promise<string> {
  const response = await request.post(`${API_BASE}/admin/security-roles`, {
    headers: { Authorization: `Bearer ${token}` },
    data: { name, description: 'E2E test role', isActive: true },
  });
  expect(response.ok()).toBeTruthy();
  const body = await response.json();
  return body.data.idKey as string;
}

async function apiDeleteRole(
  request: APIRequestContext,
  token: string,
  idKey: string,
): Promise<void> {
  await request.delete(`${API_BASE}/admin/security-roles/${idKey}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}

async function openSecurityRoleDetail(page: Page, roleName: string): Promise<void> {
  await page.getByTestId('security-roles-list').waitFor();
  const row = page.locator(
    `[data-testid="security-role-row"][data-role-name="${roleName}"]`,
  );
  await row.click();
  await page.getByTestId('security-role-detail').waitFor();
}

// Run serially because the tests share the seeded john.smith account and the
// refresh-token rotation behaviour can otherwise invalidate another worker's
// token mid-test.
test.describe.serial('feat(#497) Security role and permissions management UI', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test('AC1: admin can view and create a new security role', async ({ page }) => {
    const suffix = uniqueSuffix();
    const roleName = `E2E Volunteer ${suffix}`;

    await page.goto('/admin/settings/security-roles');
    await expect(page.getByTestId('security-roles-page')).toBeVisible();
    await expect(page.getByTestId('security-roles-list')).toBeVisible();
    // Let background queries settle so concurrent token refreshes do not
    // race the create POST.
    await page.waitForLoadState('networkidle');

    // Create role
    await page.getByTestId('security-roles-create').click();
    await expect(page.getByTestId('security-role-create-modal')).toBeVisible();
    await page.getByTestId('security-role-create-name').fill(roleName);
    await page
      .getByTestId('security-role-create-description')
      .fill('Created by E2E test');

    // Submit and wait for the POST round-trip so we fail fast on auth errors.
    const createResponse = page.waitForResponse(
      (response) =>
        response.url().endsWith('/api/v1/admin/security-roles') &&
        response.request().method() === 'POST',
    );
    await page.getByTestId('security-role-create-submit').click();
    const response = await createResponse;
    expect(response.status()).toBe(201);

    // New role should appear in the list
    await expect(
      page.locator(`[data-testid="security-role-row"][data-role-name="${roleName}"]`),
    ).toBeVisible();

    // Cleanup via API
    const token = await getAccessToken(page.request);
    const rolesResponse = await page.request.get(`${API_BASE}/admin/security-roles`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    const body = await rolesResponse.json();
    const created = body.data.find((r: { name: string }) => r.name === roleName);
    if (created) {
      await apiDeleteRole(page.request, token, created.idKey);
    }
  });

  test('AC2: admin can assign a claim to a role', async ({ page }) => {
    const suffix = uniqueSuffix();
    const roleName = `E2E Claim Role ${suffix}`;
    const token = await getAccessToken(page.request);
    const roleIdKey = await apiCreateRole(page.request, token, roleName);

    try {
      await page.goto('/admin/settings/security-roles');
      await openSecurityRoleDetail(page, roleName);

      // Pick the first available claim and assign as Allow
      const picker = page.getByTestId('security-role-claim-picker');
      await picker.waitFor();
      const optionValue = await picker
        .locator('option:not([value=""])')
        .first()
        .getAttribute('value');
      expect(optionValue).toBeTruthy();
      await picker.selectOption(optionValue!);
      await page.getByTestId('security-role-claim-decision').selectOption('A');
      await page.getByTestId('security-role-claim-assign').click();

      // One claim row should now exist
      await expect(page.getByTestId('security-role-claim-row')).toHaveCount(1);

      // Verify via API that the claim association exists server-side (no cache)
      const detail = await page.request.get(
        `${API_BASE}/admin/security-roles/${roleIdKey}`,
        { headers: { Authorization: `Bearer ${token}` } },
      );
      const detailBody = await detail.json();
      expect(detailBody.data.claims.length).toBe(1);
      expect(detailBody.data.claims[0].allowOrDeny).toBe('A');
    } finally {
      await apiDeleteRole(page.request, token, roleIdKey);
    }
  });

  test('AC3: admin can add and remove a person from a role', async ({ page }) => {
    const suffix = uniqueSuffix();
    const roleName = `E2E Member Role ${suffix}`;
    const token = await getAccessToken(page.request);
    const roleIdKey = await apiCreateRole(page.request, token, roleName);

    try {
      await page.goto('/admin/settings/security-roles');
      await openSecurityRoleDetail(page, roleName);

      // Search for "Jane Smith" (seeded)
      const picker = page.getByTestId('security-role-member-picker');
      await picker.fill('Jane');
      await page.getByTestId('security-role-member-search').click();

      // Click Add on the first result
      await page.getByTestId('security-role-member-result').first().waitFor();
      await page
        .getByTestId('security-role-member-result')
        .first()
        .getByTestId('security-role-member-add')
        .click();

      // Member should appear in the members list
      await expect(page.getByTestId('security-role-member-row')).toHaveCount(1);

      // Remove member
      await page
        .getByTestId('security-role-member-row')
        .first()
        .getByTestId('security-role-member-remove')
        .click();
      await expect(page.getByTestId('security-role-member-row')).toHaveCount(0);

      // Server confirms removal (no cache)
      const detail = await page.request.get(
        `${API_BASE}/admin/security-roles/${roleIdKey}`,
        { headers: { Authorization: `Bearer ${token}` } },
      );
      const detailBody = await detail.json();
      expect(detailBody.data.members.length).toBe(0);
    } finally {
      await apiDeleteRole(page.request, token, roleIdKey);
    }
  });

  test('AC4: claim changes take effect on the next request (no restart needed)', async ({
    request,
  }) => {
    const suffix = uniqueSuffix();
    const roleName = `E2E Deny Role ${suffix}`;
    const { token, personIdKey } = await loginAsJohn(request);

    // Baseline: john.smith already has Financial Admin → GET /giving/funds works.
    const before = await request.get(`${API_BASE}/giving/funds`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(before.status()).toBe(200);

    // Create a new role, give it a DENY on financial:view,
    // and add john.smith to it. DENY-takes-precedence means he should
    // IMMEDIATELY lose access without any restart or cache bust.
    const roleIdKey = await apiCreateRole(request, token, roleName);

    try {
      // Find financial:view claim idKey
      const claimsResponse = await request.get(
        `${API_BASE}/admin/security-claims`,
        { headers: { Authorization: `Bearer ${token}` } },
      );
      const claimsBody = await claimsResponse.json();
      const financialView = claimsBody.data.find(
        (c: { claimType: string; claimValue: string }) =>
          c.claimType === 'financial' && c.claimValue === 'view',
      );
      expect(financialView).toBeTruthy();

      // Assign DENY and add john.smith via the admin endpoints
      const assignResponse = await request.post(
        `${API_BASE}/admin/security-roles/${roleIdKey}/claims`,
        {
          headers: { Authorization: `Bearer ${token}` },
          data: { claimIdKey: financialView.idKey, allowOrDeny: 'D' },
        },
      );
      expect(assignResponse.ok()).toBeTruthy();

      const memberResponse = await request.post(
        `${API_BASE}/admin/security-roles/${roleIdKey}/members`,
        {
          headers: { Authorization: `Bearer ${token}` },
          data: { personIdKey },
        },
      );
      expect(memberResponse.ok()).toBeTruthy();

      // Immediately call the claim-protected endpoint — should be 403.
      const afterDeny = await request.get(`${API_BASE}/giving/funds`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      expect(afterDeny.status()).toBe(403);

      // Remove john.smith from the deny role — access restored immediately.
      const removeResponse = await request.delete(
        `${API_BASE}/admin/security-roles/${roleIdKey}/members/${personIdKey}`,
        { headers: { Authorization: `Bearer ${token}` } },
      );
      expect(removeResponse.ok()).toBeTruthy();

      const afterRemove = await request.get(`${API_BASE}/giving/funds`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      expect(afterRemove.status()).toBe(200);
    } finally {
      await apiDeleteRole(request, token, roleIdKey);
    }
  });

  test('system roles cannot be deleted', async ({ page }) => {
    await page.goto('/admin/settings/security-roles');
    await page.getByTestId('security-roles-list').waitFor();

    const systemRow = page.locator(
      '[data-testid="security-role-row"][data-role-name="Admin"]',
    );
    const deleteButton = systemRow.getByTestId('security-role-delete');
    await expect(deleteButton).toBeDisabled();
  });
});
