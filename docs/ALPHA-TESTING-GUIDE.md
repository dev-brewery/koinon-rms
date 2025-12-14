# Koinon RMS Alpha - Testing Guide

**Version:** 0.1.0-alpha
**Last Updated:** December 2025

This guide outlines the critical user journeys and testing procedures for alpha testers.

## Testing Overview

### Goals

1. Validate check-in workflow under real-world conditions
2. Identify usability issues
3. Verify performance targets are met
4. Document edge cases and unexpected behaviors

### Test Environment

Before testing, ensure you have:
- Deployed the application per [ALPHA-DEPLOYMENT.md](./ALPHA-DEPLOYMENT.md)
- Access to test credentials
- A tablet or desktop browser

### Test Data

The alpha includes pre-seeded test data:

**Families:**
- Smith Family (5 members)
- Johnson Family (3 members)
- Williams Family (4 members)
- Brown Family (2 members)

**Phone Numbers for Testing:**
- 555-123-4567 (Smith Family)
- 555-234-5678 (Johnson Family)
- 555-345-6789 (Williams Family)

**Groups/Schedules:**
- Kids Ministry (ages 0-12)
- Youth Group (ages 13-17)
- Adult Service
- Sunday 9:00 AM, 10:30 AM, 12:00 PM schedules

## Critical Test Cases

### TC-001: Basic Check-in Flow

**Priority:** Critical
**Estimated Time:** 5 minutes

**Steps:**
1. Navigate to `/checkin`
2. Enter phone number `555-123-4567`
3. Click Search
4. Verify Smith family members appear
5. Select one or more members
6. Click "Check In"
7. Verify success message appears

**Expected Results:**
- Family search completes in <200ms
- Members display with photos and names
- Check-in confirms successfully
- Success message shows names and room assignments

---

### TC-002: Check-in with Name Search

**Priority:** High
**Estimated Time:** 3 minutes

**Steps:**
1. Navigate to `/checkin`
2. Enter family name "Johnson"
3. Click Search
4. Verify Johnson family appears
5. Complete check-in

**Expected Results:**
- Name search works similarly to phone search
- Results appear within 200ms
- Correct family is returned

---

### TC-003: Offline Check-in

**Priority:** Critical
**Estimated Time:** 10 minutes

**Steps:**
1. Navigate to `/checkin`
2. Perform a normal search to cache family data
3. Open browser DevTools (F12)
4. Go to Network tab
5. Enable "Offline" mode (or disconnect WiFi)
6. Search for the same family
7. Complete check-in
8. Verify "Queued" or "Offline" indicator appears
9. Re-enable network
10. Wait for sync or refresh page
11. Verify check-in appears in admin console

**Expected Results:**
- Offline search completes in <50ms
- Clear indicator that device is offline
- Check-in queues successfully
- Sync occurs when online resumes

---

### TC-004: Admin Dashboard

**Priority:** High
**Estimated Time:** 5 minutes

**Steps:**
1. Log in at `/login` with admin credentials
2. Navigate to `/admin`
3. Verify dashboard loads
4. Check statistics display (total people, families, check-ins)
5. Verify quick action buttons work
6. Check upcoming schedules section

**Expected Results:**
- Dashboard loads in <1000ms
- Statistics reflect current data
- Navigation to all sections works

---

### TC-005: People Management

**Priority:** Medium
**Estimated Time:** 10 minutes

**Steps:**
1. Navigate to `/admin/people`
2. Verify list loads with pagination
3. Search for a person by name
4. Click on a person to view details
5. Navigate back to list
6. Filter by status (Active/Inactive)

**Expected Results:**
- List loads with proper pagination
- Search filters results correctly
- Person detail view shows all info
- Filters work as expected

---

### TC-006: Family Management

**Priority:** Medium
**Estimated Time:** 10 minutes

**Steps:**
1. Navigate to `/admin/families`
2. Verify family list loads
3. Click on a family to view members
4. Check family member relationships display
5. Navigate back to list

**Expected Results:**
- Family list shows all families
- Family detail shows all members
- Relationships are clear

---

### TC-007: Schedule Management

**Priority:** High
**Estimated Time:** 10 minutes

**Steps:**
1. Navigate to `/admin/schedules`
2. View existing schedules
3. Check schedule details
4. Verify group assignments
5. Check schedule timing

**Expected Results:**
- Schedules display with times and groups
- Details are accurate
- Can navigate to associated groups

---

### TC-008: Room Roster

**Priority:** High
**Estimated Time:** 5 minutes

**Steps:**
1. Perform several check-ins
2. Navigate to `/admin/roster`
3. Verify checked-in people appear
4. Check room assignments are correct
5. Verify count matches check-ins

**Expected Results:**
- Roster shows all checked-in attendees
- Room assignments match check-in selections
- Counts are accurate

---

### TC-009: Error Handling

**Priority:** Medium
**Estimated Time:** 5 minutes

**Steps:**
1. Try searching with invalid phone format
2. Try checking in without selecting anyone
3. Try accessing a non-existent page
4. Simulate network error during check-in

**Expected Results:**
- Validation errors show clear messages
- Invalid actions are prevented
- 404 pages show helpful information
- Network errors allow retry

---

### TC-010: Mobile/Tablet Experience

**Priority:** High
**Estimated Time:** 10 minutes

**Steps:**
1. Access check-in page on tablet
2. Verify touch targets are appropriately sized
3. Test on-screen keyboard interactions
4. Verify responsive layout adjustments
5. Test in landscape and portrait

**Expected Results:**
- Touch targets are minimum 44x44px
- Keyboard doesn't obscure inputs
- Layout adapts to screen size
- Both orientations work

---

## Performance Testing

### Measuring Check-in Speed

**Using Browser DevTools:**

1. Open DevTools (F12)
2. Go to Network tab
3. Filter by "Fetch/XHR"
4. Perform check-in operation
5. Note API response times
6. Target: <200ms for search, <200ms for check-in

**Using Performance Tab:**

1. Open DevTools
2. Go to Performance tab
3. Click Record
4. Perform operations
5. Stop recording
6. Analyze timeline for long tasks

### Offline Performance

1. Enable offline mode in DevTools
2. Perform search
3. Note response time in console
4. Target: <50ms for cached lookups

## Bug Reporting

### What to Report

- **Always report:**
  - Data loss
  - Security concerns
  - Crashes or blank screens
  - Performance issues (>1s delays)

- **Please report:**
  - Confusing UI/UX
  - Missing features that should be included
  - Inconsistent behavior

### How to Report

Create a GitHub Issue with:

```markdown
## Summary
[One sentence description]

## Steps to Reproduce
1. [Step 1]
2. [Step 2]
3. [Step 3]

## Expected Behavior
[What should happen]

## Actual Behavior
[What actually happened]

## Environment
- Browser: [Chrome 120, Safari 17, etc.]
- Device: [iPad Pro, Android tablet, Desktop]
- OS: [macOS, Windows, iOS, Android]

## Screenshots/Video
[Attach if helpful]

## Additional Context
[Any other relevant information]
```

## Testing Schedule

| Week | Focus Area |
|------|------------|
| 1 | Check-in flow (TC-001 to TC-003) |
| 2 | Admin console (TC-004 to TC-008) |
| 3 | Edge cases and performance (TC-009, TC-010) |
| 4 | Regression testing |

## Feedback Sessions

We'll schedule weekly feedback calls to:
- Review discovered issues
- Prioritize fixes
- Discuss feature requests
- Plan for beta release

Contact the development team to schedule your session.

## Thank You!

Your participation in alpha testing is invaluable. The feedback you provide directly shapes the product and ensures we're building something that truly serves your needs.
