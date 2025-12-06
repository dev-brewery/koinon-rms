# Quick Reference: Check-in Service Patterns

**For developers implementing Phase 2.2 refactoring**

---

## Pattern 1: Authorization

### WRONG ❌
```csharp
public async Task<AttendanceDto> CheckInAsync(int personId, int locationId)
{
    // Only checks person, not location
    if (!userContext.CanAccessPerson(personId))
        return new AttendanceDto(Success: false);

    // Continues even if location check fails
    // LocationId never verified!
}
```

### RIGHT ✓
```csharp
public async Task<CheckinResultDto> CheckInAsync(
    int personId,
    int locationId,
    CancellationToken ct = default)
{
    try
    {
        // Check BOTH person AND location before any database access
        AuthorizeCheckinOperation(personId, locationId, nameof(CheckInAsync));

        // Safe to proceed - authorization guaranteed
        var person = await context.People.FindAsync(personId, ct);
        var location = await context.Groups.FindAsync(locationId, ct);

        // ... create attendance ...
    }
    catch (UnauthorizedAccessException ex)
    {
        logger.LogWarning("Authorization denied for check-in");
        return new CheckinResultDto(
            Success: false,
            ErrorMessage: GenericAuthorizationDeniedMessage());
    }
}
```

**Rule:** Always call `Authorize*` methods at the START of public methods.

---

## Pattern 2: Batch Data Loading

### WRONG ❌ (N+1 queries)
```csharp
public async Task<List<PersonDto>> GetPeopleAsync(int[] personIds, CancellationToken ct)
{
    var results = new List<PersonDto>();

    foreach (var id in personIds)
    {
        // Query 1, 2, 3... N queries! ❌
        var person = await context.People.FindAsync(id, ct);

        // Query N+1, N+2... more N+1 queries! ❌
        var alias = await context.PersonAliases
            .FirstOrDefaultAsync(pa => pa.PersonId == id && pa.AliasPersonId == null, ct);

        results.Add(new PersonDto(person, alias));
    }

    return results;
}
```

### RIGHT ✓ (1-2 queries)
```csharp
public async Task<List<PersonDto>> GetPeopleAsync(int[] personIds, CancellationToken ct)
{
    // Load all data in 1 query instead of N+1 queries
    var peopleWithAliases = await dataLoader.LoadPersonsWithAliasesAsync(
        personIds, ct);

    // Now use pre-loaded data (O(1) dictionary lookups)
    var results = new List<PersonDto>();

    foreach (var id in personIds)
    {
        if (peopleWithAliases.TryGetValue(id, out var data))
        {
            results.Add(new PersonDto(data.Person, data.PrimaryAlias));
        }
    }

    return results;
}
```

**Rule:** Never load data inside a loop. Use `CheckinDataLoader` before the loop.

---

## Pattern 3: Race Conditions

### WRONG ❌ (TOCTOU race)
```csharp
private async Task<AttendanceOccurrence> GetOrCreateOccurrenceAsync(
    int groupId, DateOnly date, CancellationToken ct)
{
    // ❌ Race condition: Check
    var existing = await context.AttendanceOccurrences
        .FirstOrDefaultAsync(o => o.GroupId == groupId && o.OccurrenceDate == date, ct);

    if (existing != null)
        return existing;

    // ❌ Race condition: Create (another thread might have created between check and insert)
    var newOccurrence = new AttendanceOccurrence
    {
        GroupId = groupId,
        OccurrenceDate = date
    };

    context.AttendanceOccurrences.Add(newOccurrence);
    await context.SaveChangesAsync(ct);

    return newOccurrence;
}
```

### RIGHT ✓ (Atomic operation)
```csharp
private async Task<AttendanceOccurrence> GetOrCreateOccurrenceAsync(
    int groupId, DateOnly date, CancellationToken ct)
{
    // Atomic operation: Always try INSERT first, let DB handle uniqueness
    // Database constraint + retry logic prevents race conditions
    var occurrence = await concurrencyHelper.GetOrCreateOccurrenceAtomicAsync(
        groupId,
        scheduleId: null,
        date,
        ct);

    return occurrence;
}
```

**Rule:** Never check-then-create. Use `ConcurrentOperationHelper` for atomic operations.

---

## Pattern 4: Timing Attacks

### WRONG ❌ (Timing leak)
```csharp
public async Task<FamilyDto?> SearchByCodeAsync(string code, CancellationToken ct)
{
    var code = code.Trim().ToUpperInvariant();
    var today = DateTime.UtcNow.Date;

    // Valid code: 8 queries (300ms)
    // Invalid code: 1 query (20ms)
    // Attacker measures timing to enumerate valid codes! ❌

    var attendanceCode = await context.AttendanceCodes
        .FirstOrDefaultAsync(ac => ac.Code == code && ac.IssueDateTime.Date == today, ct);

    if (attendanceCode != null)
    {
        // 7 more queries...
        return await LoadFamilyByCodeAsync(attendanceCode, ct);
    }

    return null;
}
```

### RIGHT ✓ (Constant-time)
```csharp
public async Task<FamilyDto?> SearchByCodeAsync(string code, CancellationToken ct)
{
    // Use constant-time search: Found and not-found take similar time
    return await ConstantTimeHelper.SearchWithConstantTiming(
        searchOperation: async () => {
            // Actual search
            var attendanceCode = await context.AttendanceCodes
                .FirstOrDefaultAsync(ac => ac.Code == code && ac.IssueDateTime.Date == today, ct);

            if (attendanceCode != null)
            {
                return await LoadFamilyByCodeAsync(attendanceCode, ct);
            }

            return null;
        },
        busyWorkOperation: async () => {
            // Dummy work if not found (consumes time)
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(code));
            await Task.Delay(50, ct);
        }
    );
}
```

**Rule:** For security-sensitive searches, use `ConstantTimeHelper.SearchWithConstantTiming`.

---

## Pattern 5: Information Disclosure

### WRONG ❌ (Different exceptions)
```csharp
public async Task<LabelSetDto> GenerateLabelsAsync(string attendanceIdKey, CancellationToken ct)
{
    if (!IdKeyHelper.TryDecode(attendanceIdKey, out var attendanceId))
    {
        // Exception type 1: Reveals key is invalid
        throw new ArgumentException($"Invalid IdKey: {attendanceIdKey}");
    }

    var attendance = await context.Attendances.FindAsync(attendanceId, ct);

    if (attendance == null)
    {
        // Exception type 2: Reveals key is valid but not found
        throw new InvalidOperationException($"Attendance not found: {attendanceIdKey}");
    }
}
```

An attacker can tell:
- "ArgumentException" = IdKey format invalid
- "InvalidOperationException" = IdKey valid but no record

### RIGHT ✓ (Same exception)
```csharp
public async Task<LabelSetDto> GenerateLabelsAsync(string attendanceIdKey, CancellationToken ct)
{
    try
    {
        AuthorizeAuthentication(nameof(GenerateLabelsAsync));

        if (!IdKeyHelper.TryDecode(attendanceIdKey, out var attendanceId))
        {
            // Same exception regardless of reason
            throw new UnauthorizedAccessException();
        }

        var attendance = await context.Attendances.FindAsync(attendanceId, ct);

        if (attendance == null)
        {
            // Same exception (attacker can't distinguish)
            throw new UnauthorizedAccessException();
        }

        // Verify authorization
        if (!userContext.CanAccessPerson(attendance.PersonAlias!.PersonId))
        {
            throw new UnauthorizedAccessException();
        }

        // ... generate labels ...
    }
    catch (UnauthorizedAccessException)
    {
        // Single generic response
        return new LabelSetDto(Success: false, Message: "Not authorized");
    }
}
```

**Rule:** Use same exception type for all authorization failures (no information leaks).

---

## Checklist: Service Refactoring

When refactoring a service, check:

- [ ] All public methods call `Authorize*` at the START
- [ ] No loops that load data (use `CheckinDataLoader` before loop)
- [ ] All concurrent operations use `ConcurrentOperationHelper`
- [ ] No "check then create" patterns (use atomic operations)
- [ ] Security searches use `ConstantTimeHelper`
- [ ] Same exception type for all authorization failures
- [ ] No PersonAlias loaded outside of batch loading
- [ ] All queries can be executed in <3 round-trips
- [ ] Database query count is predictable (not N+1)
- [ ] Timing differences between success/failure are <20ms

---

## Common Mistakes

### Mistake 1: Forget Location Check
```csharp
// ❌ WRONG
if (!userContext.CanAccessPerson(personId))
    return error;
// locationId never checked!

// ✓ RIGHT
AuthorizeCheckinOperation(personId, locationId, operation);
```

### Mistake 2: Load PersonAlias After Authorization
```csharp
// ❌ WRONG
var attendance = context.Attendances.Find(id);
if (attendance.PersonAlias?.PersonId != null &&
    !userContext.CanAccessPerson(attendance.PersonAlias.PersonId))
    return error;

// ✓ RIGHT
var attendance = context.Attendances.Find(id);
AuthorizePersonAccess(attendance.PersonAliasId.Value, operation);
```

### Mistake 3: Call DataLoader Inside Service
```csharp
// ❌ WRONG
foreach (var personId in personIds)
{
    var person = context.People.FindAsync(personId); // N+1!
}

// ✓ RIGHT
var people = await dataLoader.LoadPersonsWithAliasesAsync(personIds, ct);
foreach (var personId in personIds)
{
    var person = people[personId]; // O(1)
}
```

### Mistake 4: Synchronous Auth Checks
```csharp
// ❌ WRONG (if CanAccessPerson is async)
if (!await userContext.CanAccessPerson(personId))
    return error;

// ✓ RIGHT
AuthorizePersonAccess(personId, operation); // Handles async if needed
```

---

## Performance Targets

After refactoring, these are the targets:

| Operation | Target | How to Verify |
|-----------|--------|---------------|
| CheckInAsync | <150ms P95 | APM dashboard |
| SearchByCodeAsync | <100ms P95 | Load test |
| Batch check-in (50 people) | <500ms P95 | Load test |
| Query count | <3 per operation | Stopwatch in unit test |
| Database round-trips | <3 | EF Core logging |

---

## Testing Your Changes

### Unit Test Template
```csharp
[Fact]
public async Task CheckInAsync_WithValidInput_CallsAuthorizeCheckinOperation()
{
    // Arrange
    var service = new CheckinAttendanceService(
        context: _mockContext,
        userContext: _mockUserContext,
        dataLoader: _mockDataLoader,
        concurrency: _mockConcurrency,
        logger: _mockLogger);

    // Act
    var result = await service.CheckInAsync(request, ct);

    // Assert
    _mockUserContext.Verify(x => x.CanAccessPerson(personId), Times.Once);
    _mockUserContext.Verify(x => x.CanAccessLocation(locationId), Times.Once);
}
```

### Integration Test Template
```csharp
[Fact]
public async Task ConcurrentCheckIns_NoRaceConditions()
{
    // Arrange: 50 concurrent check-ins
    var tasks = Enumerable.Range(1, 50)
        .Select(i => _service.CheckInAsync(
            new CheckinRequestDto(
                PersonIdKey: IdKeyHelper.Encode(i),
                LocationIdKey: IdKeyHelper.Encode(1),
                OccurrenceDate: DateOnly.FromDateTime(DateTime.UtcNow)),
            CancellationToken.None))
        .ToList();

    // Act
    var results = await Task.WhenAll(tasks);

    // Assert
    var occurrences = await _context.AttendanceOccurrences.ToListAsync();
    Assert.Single(occurrences); // Only one occurrence created
    Assert.All(results, r => Assert.True(r.Success));
}
```

---

## Where to Get Help

**If you need to understand:**
- Authorization patterns → Read: ARCHITECTURAL_REVIEW_EXECUTIVE_SUMMARY.md (Authorization Strategy section)
- Concurrency patterns → Read: ARCHITECTURAL_REVIEW_PHASE2.2.md (Concurrency Strategy section)
- N+1 elimination → Read: ARCHITECTURAL_REVIEW_PHASE2.2.md (N+1 Query Patterns section)
- Timing attacks → Read: ARCHITECTURAL_REVIEW_PHASE2.2.md (Timing Attacks section)

**If you need to implement:**
- New base class → Copy from: IMPLEMENTATION_GUIDE_BASE_CLASSES.md
- Refactor existing service → Use checklist above
- Create tests → Use test templates above

---

## Review Checklist Before Commit

- [ ] All authorization checks use base class methods
- [ ] No N+1 queries (verified with query count)
- [ ] All concurrent operations use helper
- [ ] All security searches use constant-time helper
- [ ] Same exception for all auth failures
- [ ] Tests pass (unit + integration)
- [ ] Performance targets met (latency + query count)
- [ ] Code review passes (patterns verified)

---

**Last Updated:** 2025-12-05
**Version:** 1.0
**Status:** Ready for Phase 2 implementation

