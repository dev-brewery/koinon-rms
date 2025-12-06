# Koinon RMS: Check-in UX Improvements Plan

## Consultant Feedback Summary

8 critical gaps identified that will cause friction in production check-in operations.

---

## Priority 0 - Ship Blockers

### 1. Idle Timeout (Privacy Protection)

**Problem:** No automatic reset when families walk away - next user sees previous family's data.

**Solution:**
- Add 60-second idle timeout on check-in screens
- Show 10-second warning countdown before reset
- Touch anywhere to cancel timeout
- Immediate reset on "Done" button

**Implementation:**
```
Files to modify:
- src/web/src/components/checkin/KioskLayout.tsx - Add idle detection
- src/web/src/hooks/useIdleTimeout.ts - New hook for idle tracking
- src/web/src/pages/CheckinPage.tsx - Reset state on timeout
```

**Acceptance Criteria:**
- [ ] Screen resets after 60 seconds of inactivity
- [ ] Warning dialog appears at 50 seconds
- [ ] Any touch cancels timeout
- [ ] Configurable timeout duration

---

### 2. Label Printing Integration

**Problem:** No way to print security labels - core check-in requirement.

**Solution:**
- Browser-based printing to networked Dymo/Zebra printers
- QR code on label with attendance ID
- Parent/child matching security codes

**Implementation:**
```
Files to create:
- src/web/src/services/printing/LabelPrinter.ts - Print service
- src/web/src/services/printing/ZplTemplate.ts - ZPL label templates
- src/web/src/components/checkin/PrintButton.tsx - Print UI

Backend:
- Already have GET /api/v1/checkin/labels/{id} endpoint
```

**Acceptance Criteria:**
- [ ] Labels print automatically after check-in
- [ ] Manual reprint available
- [ ] Works with Dymo LabelWriter and Zebra printers
- [ ] Includes security code, name, room, and allergies

---

## Priority 1 - Week 1 Fixes

### 3. QR/Barcode Scanning

**Problem:** Slow repeat check-ins - families must search every week.

**Solution:**
- Family QR code card that bypasses search
- Scan goes directly to member selection
- Option to print QR card on first visit

**Implementation:**
```
Files to create:
- src/web/src/components/checkin/QrScanner.tsx - Camera scanner
- src/web/src/hooks/useQrScanner.ts - Scanner hook
- src/web/src/pages/CheckinPage.tsx - Add scan mode

Backend addition:
- Family.CheckinCode field for quick lookup
```

**Acceptance Criteria:**
- [ ] Camera-based QR scanning
- [ ] Scanned code goes directly to family
- [ ] Generate family QR card for printing
- [ ] Works without network (cached family data)

---

### 4. Supervisor Mode

**Problem:** No way for staff to reprint labels or override issues.

**Solution:**
- PIN-protected supervisor mode
- Access to label reprint
- View current attendance
- Override check-in restrictions

**Implementation:**
```
Files to create:
- src/web/src/components/checkin/SupervisorMode.tsx - Admin panel
- src/web/src/components/checkin/PinEntry.tsx - PIN input
- src/web/src/hooks/useSupervisorMode.ts - State management
```

**Acceptance Criteria:**
- [ ] 4-digit PIN to enter supervisor mode
- [ ] Reprint labels for any attendance
- [ ] View current room attendance
- [ ] Clear timeout badge on supervisor actions

---

### 5. Multi-Opportunity Selection

**Problem:** Person can only select one group/location - can't attend Sunday School AND Worship.

**Solution:**
- Checkbox-based multi-select for each person
- Show all eligible opportunities
- Generate separate labels for each

**Implementation:**
```
Files to modify:
- src/web/src/components/checkin/FamilyMemberList.tsx - Multi-select UI
- src/web/src/pages/CheckinPage.tsx - Handle multiple selections
- API already supports batch attendance recording
```

**Acceptance Criteria:**
- [ ] Multiple checkboxes per person (not radio buttons)
- [ ] Clear indication of all selected activities
- [ ] Separate label per activity
- [ ] Show time conflicts if applicable

---

## Priority 2 - Near-term

### 6. Checkout Flow

**Problem:** No way to record when children leave - safety/audit gap.

**Solution:**
- Checkout mode in supervisor panel
- Scan or search for child
- Verify parent security code
- Record departure time

**Implementation:**
```
Files to create:
- src/web/src/components/checkin/CheckoutFlow.tsx - Checkout UI
- src/web/src/components/checkin/SecurityCodeVerify.tsx - Code check

Backend:
- Already have POST /api/v1/checkin/checkout/{id} endpoint
```

**Acceptance Criteria:**
- [ ] Security code verification required
- [ ] Timestamp recorded for audit
- [ ] Parent notification option
- [ ] Alerts for unmatched codes

---

### 7. Special Needs/Allergy Indicators

**Problem:** No visibility into medical/special needs during check-in.

**Solution:**
- Display allergy badges on member cards
- Special needs notes visible
- Print allergies on labels
- Alert icons for critical info

**Implementation:**
```
Files to modify:
- src/web/src/components/checkin/FamilyMemberList.tsx - Add badges
- src/Koinon.Application/DTOs/CheckinSearchResultDto.cs - Add fields

Backend:
- Add Allergies, SpecialNeeds fields to CheckinFamilyMemberDto
```

**Acceptance Criteria:**
- [ ] Allergy icons on member cards
- [ ] Special needs notes displayed
- [ ] Allergies printed on labels
- [ ] Critical allergies highlighted

---

### 8. Inline New Family Registration

**Problem:** New families can't check in without separate registration process.

**Solution:**
- "New Family" button on search screen
- Quick registration form (names, phone, basic info)
- Immediate check-in after registration
- Full profile completion later

**Implementation:**
```
Files to create:
- src/web/src/components/checkin/NewFamilyForm.tsx - Registration
- src/web/src/components/checkin/QuickAddChild.tsx - Add children

Backend:
- Use existing POST /api/v1/people and POST /api/v1/families
```

**Acceptance Criteria:**
- [ ] Minimal required fields (name, phone)
- [ ] Add multiple children quickly
- [ ] Immediate check-in after creation
- [ ] Flag for follow-up data collection

---

## Existing Outstanding Items

### Technical Debt #4: SQLite Tests
- Switch UnitOfWorkTransactionTests from InMemory to SQLite
- InMemory provider doesn't support transactions

### Product Requirement #5: Label Printing Desktop App
- Separate Windows desktop application
- Runs local web server for printer access
- Connects to Dymo/Zebra drivers
- Part of P0 label printing solution

---

## Implementation Order

```
Week 1 (P0 - Ship Blockers):
├── Day 1-2: Idle timeout
└── Day 3-5: Label printing integration

Week 2 (P1 - Critical UX):
├── Day 1-2: Multi-opportunity selection
├── Day 3-4: Supervisor mode
└── Day 5: QR scanning (basic)

Week 3 (P2 - Enhancements):
├── Day 1-2: Checkout flow
├── Day 3: Special needs/allergies
└── Day 4-5: Inline registration

Week 4 (Polish):
├── Technical debt cleanup
├── Integration testing
└── Performance optimization
```

---

## Success Metrics

| Feature | Target |
|---------|--------|
| Check-in time (repeat family) | <30 seconds |
| Check-in time (new family) | <2 minutes |
| Label print time | <3 seconds |
| Idle timeout accuracy | 100% reset |
| QR scan success rate | >95% |
