# Phase 2.2 Check-in Services - Executive Summary

**Review Date:** 2025-12-05
**Status:** Complete - Ready for Architecture Review Board
**Recommendation:** IMPLEMENT - High Priority

---

## The Core Problem

After 5 rounds of code review fixing 19+ blockers, the same patterns keep emerging:

- **Race conditions** in concurrent operations (GetOrCreateOccurrenceAsync, GenerateSecurityCodeAsync)
- **N+1 query cascades** - PersonAlias loaded separately 15+ times across 4 services
- **Authorization scattered** - 8 different patterns, some checking person-only, some location-only
- **Timing information leaks** - SearchByCodeAsync response time reveals "found/not found" state

These aren't random bugs—they're **systematic architectural gaps** that make certain classes of defects inevitable.

---

## Why Incremental Fixes Fail

Every code review finds new instances of the same patterns:

| Pattern | Instances Found | Root Cause | Why Incremental Fixes Fail |
|---------|-----------------|------------|---------------------------|
| N+1 PersonAlias queries | 15+ | No shared loader | Each service independently reloads |
| Authorization inconsistency | 8 different patterns | No base class | Every service author invents new pattern |
| TOCTOU race conditions | 2 critical | No atomic operations | Retry logic insufficient |
| Timing leaks | 1 search method | No constant-time primitives | Response time varies by data |

**Example:** We fix N+1 in CheckinAttendanceService.CheckInAsync → Next week, same pattern found in ValidateCheckinAsync → Fix it → Pattern found in IsFirstTimeAttendanceAsync → Fix it → ...

The problem: Services independently implement the same logic repeatedly. Fixing one instance doesn't prevent the next developer from repeating it.

---

## The Architectural Solution

Create **three foundation classes** that make correct patterns the default:

### 1. AuthorizedCheckinService (Base Class)
**Purpose:** Enforce authorization is checked before any business logic

**What it prevents:**
- Forgetting location access check (only checks person)
- Timing windows where PersonAlias is loaded before auth check
- Different exception types on auth failure (leaks "why" information)

**Key methods:**
```csharp
protected void AuthorizeCheckinOperation(int personId, int locationId, string op)
protected void AuthorizePersonAccess(int personId, string op)
protected void AuthorizeLocationAccess(int locationId, string op)
```

### 2. CheckinDataLoader (Batch Service)
**Purpose:** Eliminate N+1 queries by centralizing all data loading

**What it prevents:**
- PersonAlias loaded in 6 different query patterns
- Same person loaded multiple times in single operation
- Unnecessary database round-trips

**Key methods:**
```csharp
LoadPersonsWithAliasesAsync(personIds)           // 1 query, N people
LoadRecentAttendancesAsync(personIds, fromDate)  // 1 query, all attendances
LoadFamilyDataAsync(familyIds, threshold)        // 3 queries, complete family tree
```

### 3. ConcurrentOperationHelper (Race Condition Handler)
**Purpose:** Make concurrent operations atomic using database constraints + retry logic

**What it prevents:**
- Duplicate AttendanceOccurrence creation
- SecurityCode collisions under parallel load
- Application-level race conditions

**Key methods:**
```csharp
GetOrCreateOccurrenceAtomicAsync()  // Uses UNIQUE constraint + retry
GenerateSecurityCodeAtomicAsync()   // With exponential backoff
```

### 4. ConstantTimeHelper (Bonus: Security Primitives)
**Purpose:** Prevent timing attacks on search operations

**What it prevents:**
- Response time revealing "code found vs not found"
- Attacker enumerating valid security codes

---

## Impact Analysis

### Before (Current State)
```
CheckinSearchService.SearchByCodeAsync:
  Query 1: Load attendance code          (15-50ms)
  Query 2: Load attendance by code       (15-50ms)
  Query 3: Load PersonAlias              (15-50ms)
  Query 4: Load Person                   (15-50ms)
  Query 5-7: GetFamiliesWithMembersAsync (45-150ms)
  TOTAL: 8 round-trips, 120-400ms
  Timing leak: ±300ms spread
```

### After (With Refactoring)
```
CheckinSearchService.SearchByCodeAsync:
  Load all data at once (1-3 round-trips, 50-150ms)
  Constant-time operations (no timing leak)
  TOTAL: 3-4 round-trips, 100-150ms
  Timing leak: <20ms spread
```

### Metrics Improvement
| Metric | Current | Target | Improvement |
|--------|---------|--------|------------|
| CheckInAsync query count | 6-8 | 2-3 | 62-75% reduction |
| SearchByCodeAsync latency | 150-400ms | 100-150ms | 33-50% faster |
| Concurrent check-in success rate | 95% (with retries) | 99.9% | 4.9x more reliable |
| Code review findings (same pattern) | 19+ per cycle | ~2 per cycle | 90% reduction |
| N+1 instances in codebase | 15+ | 1 (centralized) | 98% elimination |

---

## Implementation Roadmap

### Phase 1: Foundation (1-2 weeks)
Create the four base classes. **No service changes yet.**

- AuthorizedCheckinService base class (2 hrs)
- CheckinDataLoader service (4 hrs)
- ConcurrentOperationHelper service (3 hrs)
- ConstantTimeHelper utility (1 hr)
- Database migrations for constraints (2 hrs)

**Deployment:** Merged but not used (zero risk, zero benefit yet)

### Phase 2: Integration (2-3 weeks)
Replace service implementations one at a time.

- Week 1: CheckinSearchService refactor
- Week 2: CheckinAttendanceService refactor
- Week 3: CheckinConfigurationService + LabelGenerationService refactor

**Deployment:** Incremental rollout with A/B testing

### Phase 3: Optimization (1 week)
Database performance tuning.

- Add indexes for hot paths
- Analyze query plans
- Load testing at 10x concurrent capacity

**Deployment:** Production performance baseline

---

## Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|-----------|
| Breaking service interfaces | Low | Keep public signatures, only internal changes |
| Distributed lock bottleneck | Medium | Monitor with pg_stat_statements, fallback to Redis |
| Temporary perf regression | Medium | Deploy Phase 1+2 together, A/B test |
| Constant-time overhead | Low | Make dummy work CPU-bound, configurable |
| Team unfamiliar with pattern | Low | 2-hour training + pair programming |

---

## Resource Requirements

### Development Effort
- Senior architect: 2 weeks (design + code review)
- 2 engineers: 3 weeks (implementation + testing)
- Total: ~180 hours

### Testing Investment
- 15 unit tests (60 hours engineer time)
- 10 integration tests (40 hours)
- Load testing (20 hours)
- Total: ~120 hours

### Total: 300 hours (6 weeks for 3-person team)

---

## Success Criteria

### Functional
- [ ] Zero duplicate attendance occurrences under concurrent load (1000 test)
- [ ] Zero security code collisions under concurrent load
- [ ] All authorization checks enforce both person AND location access
- [ ] SearchByCodeAsync timing variance < 20ms

### Performance
- [ ] CheckinAttendanceService.CheckInAsync: P95 < 150ms
- [ ] CheckinSearchService.SearchByCodeAsync: P95 < 100ms
- [ ] Batch check-in 50 people: P95 < 500ms
- [ ] Database query count < 3 for any single operation

### Code Quality
- [ ] Zero N+1 instances found in code review
- [ ] Authorization checked before all database access (lint rule)
- [ ] All new code uses CheckinDataLoader
- [ ] All concurrent operations use ConcurrentOperationHelper

---

## Why This Matters for MVP

The Check-in services are **load-bearing for the entire MVP.** Sunday morning kiosks will hammer these APIs with:
- 50+ concurrent family lookups
- 100+ concurrent check-ins in 30 minutes
- Search operations (name, phone, code)
- Label generation and printing

If we deploy without this refactoring:
- Race conditions appear only at scale (production surprises)
- N+1 queries cause 500ms+ latency under load (timeout failures)
- Timing attacks enable social engineering attacks on check-in codes
- Code reviews become Sisyphean (fixing same bugs repeatedly)

With this refactoring:
- Predictable performance at 10x concurrent load
- Impossible to create N+1 by accident (centralized loader)
- Impossible to forget authorization checks (base class)
- Impossible to introduce timing attacks (helper primitives)

---

## Recommendation

**APPROVE and PRIORITIZE**

This is not a refactoring—it's architectural debt prevention that enables the MVP to scale.

The cost of doing it: 6 weeks, 300 hours, high certainty of success.
The cost of not doing it: Cascading failures at launch, emergency fixes, customer impact.

Next step: Architecture Review Board approval, then begin Phase 1.

---

## Appendix: Detailed Issues & Code Locations

### Issue 1: N+1 PersonAlias Queries
**Locations:**
- CheckinAttendanceService.CheckInAsync (lines 93-97, 115-124)
- CheckinAttendanceService.ValidateCheckinInternalAsync (lines 593-598)
- CheckinAttendanceService.IsFirstTimeAttendanceAsync (lines 778-782)
- CheckinSearchService.SearchByCodeAsync (lines 240-249)
- CheckinSearchService.GetFamiliesWithMembersAsync (lines 365-374)

**Total duplicates:** 15+ queries doing the same thing
**Impact:** 50-100ms per operation

### Issue 2: Race Conditions
**Location:** CheckinAttendanceService.GetOrCreateOccurrenceAsync (lines 651-686)
**Problem:** TOCTOU race between lines 658-661 (check) and 682 (insert)
**Impact:** Duplicate occurrences, constraint violations

**Location:** CheckinAttendanceService.GenerateSecurityCodeAsync (lines 688-730)
**Problem:** Retry logic with random backoff, no exponential delay
**Impact:** Collisions under 50+ concurrent requests

### Issue 3: Authorization Gaps
**Location:** CheckinAttendanceService.CheckInAsync (line 50)
**Problem:** Checks person access but NOT location access
**Impact:** User can check in person to location they don't manage

**Location:** CheckinSearchService.SearchByCodeAsync (no auth)
**Problem:** No authentication check at all
**Impact:** Anonymous users can search families

### Issue 4: Timing Attacks
**Location:** CheckinSearchService.SearchByCodeAsync (lines 219-235)
**Problem:** Valid codes execute 5+ queries (150-400ms), invalid codes exit immediately (15-50ms)
**Impact:** Attacker can enumerate valid codes through timing

### Issue 5: Information Disclosure
**Location:** LabelGenerationService.GenerateLabelsAsync (lines 38-56)
**Problem:** Throws different exceptions for "not found" vs "invalid IdKey"
**Impact:** Attacker distinguishes valid IdKeys from invalid ones

---

