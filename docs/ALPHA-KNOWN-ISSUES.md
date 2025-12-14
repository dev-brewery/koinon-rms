# Koinon RMS Alpha - Known Issues

**Last Updated:** December 2025

This document lists known issues in the alpha release along with workarounds and expected fix timelines.

## Critical Issues

*No critical issues at this time.*

## High Priority Issues

### ALPHA-001: Offline sync may fail silently after extended offline period

**Symptoms:** Check-ins performed while offline for extended periods (>24 hours) may not sync when connectivity is restored.

**Workaround:**
1. Refresh the browser after connectivity is restored
2. Re-perform any check-ins that don't appear in the admin console
3. Clear browser cache if sync continues to fail

**Expected Fix:** Beta release

---

### ALPHA-002: Session timeout doesn't show warning

**Symptoms:** User sessions expire after inactivity without warning, requiring re-login.

**Workaround:**
- Refresh the page periodically during long idle periods
- If logged out, simply log back in - no data is lost

**Expected Fix:** Beta release

---

## Medium Priority Issues

### ALPHA-003: Search requires minimum 3 characters

**Symptoms:** Family/person search doesn't return results until 3+ characters are entered.

**Workaround:** Enter at least 3 characters (e.g., phone area code or first 3 letters of name).

**Expected Fix:** v1.0 (configurable minimum)

---

### ALPHA-004: Dashboard statistics may show stale data

**Symptoms:** Dashboard statistics (total people, families, etc.) may not update immediately after changes.

**Workaround:** Refresh the page to see updated statistics.

**Expected Fix:** Beta release (real-time updates)

---

### ALPHA-005: Mobile keyboard may cover input fields

**Symptoms:** On some tablets, the on-screen keyboard may partially cover input fields.

**Workaround:**
- Scroll the page to bring the input field into view
- Use landscape orientation for better layout

**Expected Fix:** Beta release

---

## Low Priority Issues

### ALPHA-006: Empty state illustrations not implemented

**Symptoms:** Empty list views show text-only empty states without illustrations.

**Impact:** Cosmetic only - functionality not affected.

**Expected Fix:** Beta release

---

### ALPHA-007: Browser back button may cause unexpected navigation

**Symptoms:** Using the browser back button in the admin console may navigate to unexpected pages.

**Workaround:** Use the in-app navigation links instead of browser back button.

**Expected Fix:** Beta release

---

### ALPHA-008: Print label preview not available

**Symptoms:** Label printing feature shows a placeholder - actual print not yet implemented.

**Workaround:** Record check-ins manually until printing is available.

**Expected Fix:** Beta release (requires hardware testing)

---

## Configuration Limitations

### Data Seeding

The alpha includes pre-seeded test data:
- Sample families and people
- Sample groups and schedules
- Test user accounts

This data is for testing purposes only. Production deployment will start with clean data or data migration.

### Environment Variables

Some configuration options are not yet exposed via environment variables:
- Session timeout duration (fixed at 30 minutes)
- Search minimum characters (fixed at 3)
- Cache TTL values (fixed at defaults)

These will be configurable in future releases.

## Reporting New Issues

To report a new issue:

1. Check this document to see if the issue is already known
2. Search existing GitHub Issues
3. If not found, create a new GitHub Issue with:
   - Clear description of the problem
   - Steps to reproduce
   - Expected vs actual behavior
   - Browser and device information
   - Screenshots if applicable

## Issue Status Legend

| Status | Meaning |
|--------|---------|
| Critical | System unusable, data loss risk |
| High | Major functionality impaired |
| Medium | Feature works with limitations |
| Low | Cosmetic or minor inconvenience |
