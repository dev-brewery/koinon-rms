using Koinon.Domain.Entities;
using Koinon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Koinon.Api.Services;

/// <summary>
/// Seeds test data required for E2E tests in development mode.
/// Ensures deterministic test fixtures exist so Playwright tests can
/// exercise the check-in kiosk flow against the real backend.
///
/// Data here MUST match src/web/e2e/fixtures/test-data.ts.
/// All methods are idempotent — safe to call on every startup.
/// </summary>
public static class DevelopmentDataSeeder
{
    // Well-known GUIDs from test-data.ts
    private static readonly Guid _nurseryGuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _preschoolGuid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid _elementaryGuid = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    // "Always Open" schedule for dev — has no WeeklyDayOfWeek/WeeklyTimeOfDay
    // so IsScheduleCheckinActive() returns true unconditionally.
    private static readonly Guid _alwaysOpenScheduleGuid = Guid.Parse("aa000002-0000-0000-0000-000000000001");

    // Deterministic GUIDs for room locations (seeder-owned)
    private static readonly Guid _room101Guid = Guid.Parse("aa000001-0000-0000-0000-000000000001");
    private static readonly Guid _room201Guid = Guid.Parse("aa000001-0000-0000-0000-000000000002");
    private static readonly Guid _room301Guid = Guid.Parse("aa000001-0000-0000-0000-000000000003");

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KoinonDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<KoinonDbContext>>();

        await SeedPersonAliasesAsync(db, logger);
        await SeedPhoneNumbersAsync(db, logger);
        await SeedAlwaysOpenScheduleAsync(db, logger);
        await SeedRoomLocationsAsync(db, logger);
        await SeedGroupSchedulesAsync(db, logger);
        await UpdateGroupScheduleRefsAsync(db, logger);
    }

    /// <summary>
    /// Ensures every Person has a primary PersonAlias record (AliasPersonId = null).
    /// The attendance service requires PersonAlias to create attendance records.
    /// </summary>
    private static async Task SeedPersonAliasesAsync(KoinonDbContext db, ILogger logger)
    {
        // Find all people who don't have a primary alias (AliasPersonId IS NULL)
        var peopleWithoutAlias = await db.People.AsNoTracking()
            .Where(p => !db.PersonAliases.Any(pa => pa.PersonId == p.Id && pa.AliasPersonId == null))
            .Select(p => new { p.Id, p.Guid })
            .ToListAsync();

        if (peopleWithoutAlias.Count == 0)
        {
            return;
        }

        foreach (var person in peopleWithoutAlias)
        {
            db.PersonAliases.Add(new PersonAlias
            {
                PersonId = person.Id,
                AliasPersonId = null,
                Guid = Guid.NewGuid(),
                CreatedDateTime = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Development seeder: added {Count} primary person aliases", peopleWithoutAlias.Count);
    }

    private static async Task SeedPhoneNumbersAsync(KoinonDbContext db, ILogger logger)
    {
        var seedPhones = new[]
        {
            new { PersonGuid = Guid.Parse("33333333-3333-3333-3333-333333333333"), Number = "(555) 123-4567", Normalized = "5551234567" }, // John Smith
            new { PersonGuid = Guid.Parse("44444444-4444-4444-4444-444444444444"), Number = "(555) 123-4567", Normalized = "5551234567" }, // Jane Smith
            new { PersonGuid = Guid.Parse("77777777-7777-7777-7777-777777777777"), Number = "(555) 987-6543", Normalized = "5559876543" }, // Bob Johnson
            new { PersonGuid = Guid.Parse("88888888-8888-8888-8888-888888888888"), Number = "(555) 987-6543", Normalized = "5559876543" }, // Barbara Johnson
        };

        var seeded = 0;
        foreach (var phone in seedPhones)
        {
            var person = await db.People.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Guid == phone.PersonGuid);
            if (person == null)
            {
                continue;
            }

            var exists = await db.PhoneNumbers.AsNoTracking()
                .AnyAsync(pn => pn.PersonId == person.Id && pn.NumberNormalized == phone.Normalized);
            if (exists)
            {
                continue;
            }

            db.PhoneNumbers.Add(new PhoneNumber
            {
                PersonId = person.Id,
                Number = phone.Number,
                NumberNormalized = phone.Normalized,
                CountryCode = "1",
                IsMessagingEnabled = true,
                IsUnlisted = false,
                Guid = Guid.NewGuid(),
                CreatedDateTime = DateTime.UtcNow,
            });
            seeded++;
        }

        if (seeded > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Development seeder: added {Count} phone numbers for test families", seeded);
        }
    }

    /// <summary>
    /// Seeds an "Always Open" schedule without WeeklyDayOfWeek/WeeklyTimeOfDay.
    /// The attendance service treats schedules with no weekly config as always open.
    /// </summary>
    private static async Task SeedAlwaysOpenScheduleAsync(KoinonDbContext db, ILogger logger)
    {
        var exists = await db.Schedules.AsNoTracking().AnyAsync(s => s.Guid == _alwaysOpenScheduleGuid);
        if (exists)
        {
            return;
        }

        db.Schedules.Add(new Schedule
        {
            Name = "Always Open (Dev)",
            IsActive = true,
            // No WeeklyDayOfWeek or WeeklyTimeOfDay → always open
            CheckInStartOffsetMinutes = null,
            CheckInEndOffsetMinutes = null,
            Guid = _alwaysOpenScheduleGuid,
            CreatedDateTime = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Development seeder: added 'Always Open (Dev)' schedule");
    }

    /// <summary>
    /// Seeds room locations (Room 101, 201, 301) for check-in groups.
    /// </summary>
    private static async Task SeedRoomLocationsAsync(KoinonDbContext db, ILogger logger)
    {
        var rooms = new[]
        {
            new { Guid = _room101Guid, Name = "Room 101", SoftThreshold = 15, FirmThreshold = 20 },
            new { Guid = _room201Guid, Name = "Room 201", SoftThreshold = 20, FirmThreshold = 25 },
            new { Guid = _room301Guid, Name = "Room 301", SoftThreshold = 30, FirmThreshold = 35 },
        };

        var seeded = 0;
        foreach (var room in rooms)
        {
            var exists = await db.Locations.AsNoTracking().AnyAsync(l => l.Guid == room.Guid);
            if (exists)
            {
                continue;
            }

            db.Locations.Add(new Location
            {
                Name = room.Name,
                IsActive = true,
                SoftRoomThreshold = room.SoftThreshold,
                FirmRoomThreshold = room.FirmThreshold,
                Guid = room.Guid,
                CreatedDateTime = DateTime.UtcNow,
            });
            seeded++;
        }

        if (seeded > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Development seeder: added {Count} room locations", seeded);
        }
    }

    /// <summary>
    /// Seeds group_schedule entries linking check-in groups to schedules and rooms.
    /// This is what makes the opportunities endpoint return available options.
    ///
    /// Nursery    → Room 101 @ Sunday 9am
    /// Preschool  → Room 201 @ Sunday 9am
    /// Elementary → Room 301 @ Sunday 9am
    /// </summary>
    private static async Task SeedGroupSchedulesAsync(KoinonDbContext db, ILogger logger)
    {
        // Look up entities by well-known GUIDs
        var nursery = await db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Guid == _nurseryGuid);
        var preschool = await db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Guid == _preschoolGuid);
        var elementary = await db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Guid == _elementaryGuid);
        var alwaysOpen = await db.Schedules.AsNoTracking().FirstOrDefaultAsync(s => s.Guid == _alwaysOpenScheduleGuid);
        var room101 = await db.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.Guid == _room101Guid);
        var room201 = await db.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.Guid == _room201Guid);
        var room301 = await db.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.Guid == _room301Guid);

        if (nursery == null || preschool == null || elementary == null ||
            alwaysOpen == null || room101 == null || room201 == null || room301 == null)
        {
            logger.LogWarning("Development seeder: skipping group_schedule seeding — prerequisite data missing");
            return;
        }

        var schedules = new[]
        {
            new { GroupId = nursery.Id, ScheduleId = alwaysOpen.Id, LocationId = room101.Id },
            new { GroupId = preschool.Id, ScheduleId = alwaysOpen.Id, LocationId = room201.Id },
            new { GroupId = elementary.Id, ScheduleId = alwaysOpen.Id, LocationId = room301.Id },
        };

        var seeded = 0;
        foreach (var gs in schedules)
        {
            var exists = await db.GroupSchedules.AsNoTracking()
                .AnyAsync(x => x.GroupId == gs.GroupId && x.ScheduleId == gs.ScheduleId && x.LocationId == gs.LocationId);
            if (exists)
            {
                continue;
            }

            db.GroupSchedules.Add(new GroupSchedule
            {
                GroupId = gs.GroupId,
                ScheduleId = gs.ScheduleId,
                LocationId = gs.LocationId,
                Order = 0,
                Guid = Guid.NewGuid(),
                CreatedDateTime = DateTime.UtcNow,
            });
            seeded++;
        }

        if (seeded > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Development seeder: added {Count} group schedules", seeded);
        }

        // Remove any non-always-open group_schedule entries for these groups
        // so the opportunities endpoint only returns the always-open schedule.
        var groupIds = new[] { nursery.Id, preschool.Id, elementary.Id };
        foreach (var groupId in groupIds)
        {
            var removed = await db.Database.ExecuteSqlInterpolatedAsync(
                $@"DELETE FROM group_schedule
                   WHERE group_id = {groupId} AND schedule_id != {alwaysOpen.Id}");
            if (removed > 0)
            {
                logger.LogInformation("Development seeder: removed {Count} non-always-open schedules for group {GroupId}", removed, groupId);
            }
        }
    }

    /// <summary>
    /// Updates check-in groups (Nursery, Preschool, Elementary) to reference
    /// the "Always Open" schedule so check-in validation passes at any time.
    /// Group.ScheduleId controls the schedule window validation in CheckinAttendanceService.
    /// </summary>
    private static async Task UpdateGroupScheduleRefsAsync(KoinonDbContext db, ILogger logger)
    {
        var alwaysOpen = await db.Schedules.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Guid == _alwaysOpenScheduleGuid);
        if (alwaysOpen == null)
        {
            return;
        }

        // Use ExecuteSqlInterpolated per-group to avoid EF Core change-tracker issues.
        // Previous seeder methods loaded groups with AsNoTracking, and EF Core's
        // identity resolution can interfere with subsequent tracked queries.
        var groupGuids = new[] { _nurseryGuid, _preschoolGuid, _elementaryGuid };
        var totalUpdated = 0;

        foreach (var guid in groupGuids)
        {
            var updated = await db.Database.ExecuteSqlInterpolatedAsync(
                $@"UPDATE ""group"" SET schedule_id = {alwaysOpen.Id}
                   WHERE guid = {guid} AND (schedule_id IS NULL OR schedule_id != {alwaysOpen.Id})");
            totalUpdated += updated;
        }

        if (totalUpdated > 0)
        {
            logger.LogInformation("Development seeder: updated {Count} groups to use always-open schedule", totalUpdated);
        }
    }
}
