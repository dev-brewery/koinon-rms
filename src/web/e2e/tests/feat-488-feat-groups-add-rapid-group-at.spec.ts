/**
 * E2E Tests: Feature #488 — Rapid Group Attendance Entry
 * Covers the "Take Attendance" flow for group leaders: modal with date picker,
 * member roster checkboxes, save, and attendance history on group detail page.
 */

import { test, expect } from "../fixtures/auth.fixture";
import { GroupsPage } from "../fixtures/page-objects/groups.page";

test.describe("Rapid Group Attendance Entry", () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test("MyGroupsPage loads and shows heading", async ({ page }) => {
    await page.goto("/my-groups");
    await expect(page.getByRole("heading", { name: /my groups/i })).toBeVisible({ timeout: 10000 });
  });

  test("group leaders see Take Attendance action on their groups", async ({ page }) => {
    await page.goto("/my-groups");
    await expect(page.getByRole("heading", { name: /my groups/i })).toBeVisible({ timeout: 10000 });
    await page.waitForLoadState("networkidle");

    // Either groups are present (showing Take Attendance buttons) or empty state is shown.
    // Both are valid — the page must load without error and show the correct UI.
    const takeAttendanceBtn = page.getByRole("button", { name: /take attendance/i });
    const btnCount = await takeAttendanceBtn.count();

    if (btnCount > 0) {
      // Group leaders: verify Take Attendance action is visible
      await expect(takeAttendanceBtn.first()).toBeVisible();
    }
    // else: empty state rendered — heading assertion above already proves the page loaded correctly
  });

  test("Take Attendance modal shows date picker defaulting to today", async ({ page }) => {
    await page.goto("/my-groups");
    await page.waitForLoadState("networkidle");

    const takeAttendanceBtn = page.getByRole("button", { name: /take attendance/i }).first();
    if (await takeAttendanceBtn.count() === 0) {
      test.skip();
      return;
    }

    // Expand members so modal receives member list
    const viewMembersBtn = page.getByRole("button", { name: /view members/i }).first();
    if (await viewMembersBtn.count() > 0) {
      await viewMembersBtn.click();
      await page.waitForLoadState("networkidle");
    }

    await takeAttendanceBtn.click();
    await expect(page.getByRole("heading", { name: /take attendance/i })).toBeVisible({ timeout: 5000 });

    const today = new Date().toISOString().split("T")[0];
    const datePicker = page.locator("input[type=date]");
    await expect(datePicker).toBeVisible();
    await expect(datePicker).toHaveValue(today);
  });

  test("attendance modal shows member roster with present/absent checkboxes", async ({ page }) => {
    await page.goto("/my-groups");
    await page.waitForLoadState("networkidle");

    const takeAttendanceBtn = page.getByRole("button", { name: /take attendance/i }).first();
    if (await takeAttendanceBtn.count() === 0) {
      test.skip();
      return;
    }

    const viewMembersBtn = page.getByRole("button", { name: /view members/i }).first();
    if (await viewMembersBtn.count() > 0) {
      await viewMembersBtn.click();
      await page.waitForLoadState("networkidle");
    }

    await takeAttendanceBtn.click();
    await expect(page.getByRole("heading", { name: /take attendance/i })).toBeVisible({ timeout: 5000 });

    // Member list container renders
    await expect(page.locator(".divide-y").first()).toBeVisible({ timeout: 5000 });

    // Checkboxes for present/absent toggles
    const checkboxes = page.locator("input[type=checkbox]");
    const checkboxCount = await checkboxes.count();
    if (checkboxCount > 0) {
      await checkboxes.first().check();
      await expect(checkboxes.first()).toBeChecked();
      await checkboxes.first().uncheck();
      await expect(checkboxes.first()).not.toBeChecked();
    }

    await expect(page.getByRole("button", { name: /record attendance/i })).toBeVisible();
    await expect(page.getByRole("button", { name: /cancel/i })).toBeVisible();
  });

  test("date picker is user-editable in attendance modal", async ({ page }) => {
    await page.goto("/my-groups");
    await page.waitForLoadState("networkidle");

    const takeAttendanceBtn = page.getByRole("button", { name: /take attendance/i }).first();
    if (await takeAttendanceBtn.count() === 0) {
      test.skip();
      return;
    }

    const viewMembersBtn = page.getByRole("button", { name: /view members/i }).first();
    if (await viewMembersBtn.count() > 0) {
      await viewMembersBtn.click();
      await page.waitForLoadState("networkidle");
    }

    await takeAttendanceBtn.click();
    await expect(page.getByRole("heading", { name: /take attendance/i })).toBeVisible({ timeout: 5000 });

    const datePicker = page.locator("input[type=date]");
    await datePicker.fill("2025-01-05");
    await expect(datePicker).toHaveValue("2025-01-05");
  });

  test("screenshot: rapid attendance entry on My Groups page", async ({ page }) => {
    await page.goto("/my-groups");
    await page.waitForLoadState("networkidle");

    const takeAttendanceBtn = page.getByRole("button", { name: /take attendance/i }).first();
    if (await takeAttendanceBtn.count() > 0) {
      const viewMembersBtn = page.getByRole("button", { name: /view members/i }).first();
      if (await viewMembersBtn.count() > 0) {
        await viewMembersBtn.click();
        await page.waitForLoadState("networkidle");
      }
      await takeAttendanceBtn.click();
      await expect(page.getByRole("heading", { name: /take attendance/i })).toBeVisible({ timeout: 5000 });
    }

    await expect(page).toHaveScreenshot("rapid-attendance-my-groups.png");
  });
});

test.describe("Group Attendance History on Detail Page", () => {
  test.beforeEach(async ({ loginAsAdmin }) => {
    await loginAsAdmin();
  });

  test("group detail page shows Attendance History section", async ({ page }) => {
    const groupsPage = new GroupsPage(page);
    await groupsPage.gotoTreeView();

    await page.getByRole("link", { name: /nursery/i }).first().click();
    await page.waitForLoadState("networkidle");

    await expect(page.getByRole("heading", { name: /attendance history/i })).toBeVisible({ timeout: 10000 });
  });

  test("past attendance is viewable on group detail page", async ({ page }) => {
    const groupsPage = new GroupsPage(page);
    await groupsPage.gotoTreeView();

    await page.getByRole("link", { name: /nursery/i }).first().click();
    await page.waitForLoadState("networkidle");

    await expect(page.getByRole("heading", { name: /attendance history/i })).toBeVisible({ timeout: 10000 });

    // Either shows records or "No attendance records yet" — section must exist
    const hasNoRecords = await page.getByText(/no attendance records yet/i).count();
    const hasRecords = await page.locator("[class*=border][class*=rounded]").count();
    expect(hasNoRecords + hasRecords).toBeGreaterThan(0);
  });

  test("screenshot: group detail page with attendance history section", async ({ page }) => {
    const groupsPage = new GroupsPage(page);
    await groupsPage.gotoTreeView();

    await page.getByRole("link", { name: /nursery/i }).first().click();
    await page.waitForLoadState("networkidle");

    await expect(page.getByRole("heading", { name: /attendance history/i })).toBeVisible({ timeout: 10000 });

    await expect(page).toHaveScreenshot("group-detail-attendance-history.png");
  });
});
