using Koinon.Application.Interfaces;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Koinon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.TestDataSeeder;

/// <summary>
/// Seeds deterministic test data into the database for E2E and integration testing.
/// Uses fixed GUIDs for easy reference in tests.
/// </summary>
public class DataSeeder
{
    private readonly KoinonDbContext _context;
    private readonly ILogger<DataSeeder> _logger;
    private readonly IAuthService _authService;

    // Fixed GUIDs for deterministic data
    private static readonly Guid _smithFamilyGuid = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _johnsonFamilyGuid = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid _johnSmithGuid = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid _janeSmithGuid = new("44444444-4444-4444-4444-444444444444");
    private static readonly Guid _johnnySmithGuid = new("55555555-5555-5555-5555-555555555555");
    private static readonly Guid _jennySmithGuid = new("66666666-6666-6666-6666-666666666666");
    private static readonly Guid _bobJohnsonGuid = new("77777777-7777-7777-7777-777777777777");
    private static readonly Guid _barbaraJohnsonGuid = new("88888888-8888-8888-8888-888888888888");
    private static readonly Guid _billyJohnsonGuid = new("99999999-9999-9999-9999-999999999999");
    private static readonly Guid _nurseryGroupGuid = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _preschoolGroupGuid = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid _elementaryGroupGuid = new("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid _sunday9amScheduleGuid = new("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid _sunday11amScheduleGuid = new("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid _wednesday7pmScheduleGuid = new("ffffffff-ffff-ffff-ffff-ffffffffffff");
    private static readonly Guid _familyGroupTypeGuid = new("10101010-1010-1010-1010-101010101010");
    private static readonly Guid _checkinGroupTypeGuid = new("20202020-2020-2020-2020-202020202020");
    private static readonly Guid _generalGroupTypeGuid = new("60606060-6060-6060-6060-606060606060");
    private static readonly Guid _adultRoleGuid = new("30303030-3030-3030-3030-303030303030");
    private static readonly Guid _childRoleGuid = new("40404040-4040-4040-4040-404040404040");
    private static readonly Guid _memberRoleGuid = new("50505050-5050-5050-5050-505050505050");
    private static readonly Guid _adminSecurityRoleGuid = new("70707070-7070-7070-7070-707070707070");

    public DataSeeder(KoinonDbContext context, ILogger<DataSeeder> logger, IAuthService authService)
    {
        _context = context;
        _logger = logger;
        _authService = authService;
    }

    /// <summary>
    /// Reset database by truncating all tables and resetting sequences.
    /// </summary>
    public async Task ResetDatabaseAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Truncating tables...");

        // Disable triggers to avoid foreign key violations
        await _context.Database.ExecuteSqlRawAsync("SET session_replication_role = 'replica';", cancellationToken);

        // Get all table names (excluding EF migrations table)
        var tables = await _context.Database
            .SqlQueryRaw<string>(
                @"SELECT tablename
                  FROM pg_tables
                  WHERE schemaname = 'public'
                  AND tablename != '__EFMigrationsHistory'")
            .ToListAsync(cancellationToken);

        // Truncate each table
        foreach (var table in tables)
        {
            _logger.LogDebug("Truncating table: {Table}", table);
            // Table names cannot be parameterized in SQL - must use raw SQL.
            // Table names come from pg_tables (system catalog), so this is safe.
            var truncateSql = string.Format("TRUNCATE TABLE \"{0}\" RESTART IDENTITY CASCADE;", table);
#pragma warning disable EF1002 // Table names from pg_tables are safe system catalog values
            await _context.Database.ExecuteSqlRawAsync(truncateSql, cancellationToken);
#pragma warning restore EF1002
        }

        // Re-enable triggers
        await _context.Database.ExecuteSqlRawAsync("SET session_replication_role = 'origin';", cancellationToken);

        _logger.LogInformation("Truncated {Count} tables", tables.Count);
    }

    /// <summary>
    /// Seed all test data.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Seed group types and roles
        _logger.LogInformation("Seeding group types and roles...");
        var (familyGroupType, checkinGroupType, adultRole, childRole, memberRole) = await SeedGroupTypesAsync(now, cancellationToken);

        // Seed schedules
        _logger.LogInformation("Seeding schedules...");
        var (sunday9am, sunday11am, wednesday7pm) = await SeedSchedulesAsync(now, cancellationToken);

        // Seed families and people
        _logger.LogInformation("Seeding families and people...");
        var people = await SeedFamiliesAsync(adultRole.Id, childRole.Id, now, cancellationToken);

        // Seed check-in groups
        _logger.LogInformation("Seeding check-in groups...");
        await SeedCheckinGroupsAsync(checkinGroupType.Id, memberRole.Id, sunday9am.Id, now, cancellationToken);

        // Final save for all check-in groups
        await _context.SaveChangesAsync(cancellationToken);

        // Seed security roles and assign Admin to John Smith
        _logger.LogInformation("Seeding security roles...");
        await SeedSecurityRolesAsync(people, now, cancellationToken);

        _logger.LogInformation("✅ Successfully seeded all test data");
    }

    private async Task<(GroupType familyGroupType, GroupType checkinGroupType, GroupTypeRole adultRole, GroupTypeRole childRole, GroupTypeRole memberRole)> SeedGroupTypesAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        // Family group type
        var familyGroupType = new GroupType
        {
            Guid = _familyGroupTypeGuid,
            Name = "Family",
            Description = "Family group type for organizing people into family units",
            GroupTerm = "Family",
            GroupMemberTerm = "Family Member",
            IsSystem = true,
            ShowInGroupList = false,
            ShowInNavigation = false,
            TakesAttendance = false,
            CreatedDateTime = now
        };

        // Check-in group type
        var checkinGroupType = new GroupType
        {
            Guid = _checkinGroupTypeGuid,
            Name = "Check-in Group",
            Description = "Groups used for check-in areas",
            GroupTerm = "Group",
            GroupMemberTerm = "Member",
            IsSystem = true,
            ShowInGroupList = true,
            ShowInNavigation = true,
            TakesAttendance = true,
            CreatedDateTime = now
        };

        // General group type (for generic groups created in E2E tests)
        var generalGroupType = new GroupType
        {
            Guid = _generalGroupTypeGuid,
            Name = "General",
            Description = "General purpose groups",
            GroupTerm = "Group",
            GroupMemberTerm = "Member",
            IsSystem = false,
            ShowInGroupList = true,
            ShowInNavigation = true,
            TakesAttendance = false,
            CreatedDateTime = now
        };

        _context.GroupTypes.Add(familyGroupType);
        _context.GroupTypes.Add(checkinGroupType);
        _context.GroupTypes.Add(generalGroupType);
        // Save group types first so we have their IDs for roles
        await _context.SaveChangesAsync(cancellationToken);

        // Roles for family group type
        var adultRole = new GroupTypeRole
        {
            Guid = _adultRoleGuid,
            GroupTypeId = familyGroupType.Id,
            Name = "Adult",
            Description = "Adult family member",
            IsSystem = true,
            IsLeader = true,
            CanView = true,
            CanEdit = true,
            CanManageMembers = true,
            Order = 0,
            CreatedDateTime = now
        };

        var childRole = new GroupTypeRole
        {
            Guid = _childRoleGuid,
            GroupTypeId = familyGroupType.Id,
            Name = "Child",
            Description = "Child family member",
            IsSystem = true,
            IsLeader = false,
            CanView = true,
            CanEdit = false,
            CanManageMembers = false,
            Order = 1,
            CreatedDateTime = now
        };

        // Role for check-in groups
        var memberRole = new GroupTypeRole
        {
            Guid = _memberRoleGuid,
            GroupTypeId = checkinGroupType.Id,
            Name = "Member",
            Description = "Group member",
            IsSystem = true,
            IsLeader = false,
            CanView = true,
            CanEdit = false,
            CanManageMembers = false,
            Order = 0,
            CreatedDateTime = now
        };

        _context.GroupTypeRoles.Add(adultRole);
        _context.GroupTypeRoles.Add(childRole);
        _context.GroupTypeRoles.Add(memberRole);
        // Save roles so we have their IDs for group members
        await _context.SaveChangesAsync(cancellationToken);

        return (familyGroupType, checkinGroupType, adultRole, childRole, memberRole);
    }

    private async Task<(Schedule sunday9am, Schedule sunday11am, Schedule wednesday7pm)> SeedSchedulesAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        var sunday9am = new Schedule
        {
            Guid = _sunday9amScheduleGuid,
            Name = "Sunday 9:00 AM",
            Description = "Sunday morning service at 9:00 AM",
            WeeklyDayOfWeek = DayOfWeek.Sunday,
            WeeklyTimeOfDay = new TimeSpan(9, 0, 0),
            CheckInStartOffsetMinutes = 60,
            CheckInEndOffsetMinutes = 30,
            IsActive = true,
            IsPublic = true,
            Order = 0,
            CreatedDateTime = now
        };

        var sunday11am = new Schedule
        {
            Guid = _sunday11amScheduleGuid,
            Name = "Sunday 11:00 AM",
            Description = "Sunday morning service at 11:00 AM",
            WeeklyDayOfWeek = DayOfWeek.Sunday,
            WeeklyTimeOfDay = new TimeSpan(11, 0, 0),
            CheckInStartOffsetMinutes = 60,
            CheckInEndOffsetMinutes = 30,
            IsActive = true,
            IsPublic = true,
            Order = 1,
            CreatedDateTime = now
        };

        var wednesday7pm = new Schedule
        {
            Guid = _wednesday7pmScheduleGuid,
            Name = "Wednesday 7:00 PM",
            Description = "Wednesday evening service at 7:00 PM",
            WeeklyDayOfWeek = DayOfWeek.Wednesday,
            WeeklyTimeOfDay = new TimeSpan(19, 0, 0),
            CheckInStartOffsetMinutes = 30,
            CheckInEndOffsetMinutes = 15,
            IsActive = true,
            IsPublic = true,
            Order = 2,
            CreatedDateTime = now
        };

        _context.Schedules.Add(sunday9am);
        _context.Schedules.Add(sunday11am);
        _context.Schedules.Add(wednesday7pm);
        // Save schedules so we have their IDs for groups
        await _context.SaveChangesAsync(cancellationToken);

        return (sunday9am, sunday11am, wednesday7pm);
    }

    private async Task<List<Person>> SeedFamiliesAsync(
        int adultRoleId, int childRoleId, DateTime now, CancellationToken cancellationToken = default)
    {
        var people = new List<Person>();

        // Smith family (using Family entity, not Group)
        var smithFamily = new Family
        {
            Guid = _smithFamilyGuid,
            Name = "Smith Family",
            IsActive = true,
            CreatedDateTime = now
        };
        _context.Families.Add(smithFamily);
        // Save family so we have its ID for members
        await _context.SaveChangesAsync(cancellationToken);

        // John Smith (Adult) - Admin user for E2E tests
        var johnSmith = new Person
        {
            Guid = _johnSmithGuid,
            FirstName = "John",
            LastName = "Smith",
            NickName = "John",
            Gender = Gender.Male,
            BirthYear = 1985,
            BirthMonth = 6,
            BirthDay = 15,
            Email = "john.smith@example.com",
            IsEmailActive = true,
            PasswordHash = await _authService.HashPasswordAsync("admin123"),
            CreatedDateTime = now
        };
        people.Add(johnSmith);

        // Jane Smith (Adult)
        var janeSmith = new Person
        {
            Guid = _janeSmithGuid,
            FirstName = "Jane",
            LastName = "Smith",
            NickName = "Jane",
            Gender = Gender.Female,
            BirthYear = 1987,
            BirthMonth = 8,
            BirthDay = 22,
            Email = "jane.smith@example.com",
            IsEmailActive = true,
            CreatedDateTime = now
        };
        people.Add(janeSmith);

        // Johnny Smith (Child, age 6)
        var currentYear = DateTime.UtcNow.Year;
        var johnnySmith = new Person
        {
            Guid = _johnnySmithGuid,
            FirstName = "Johnny",
            LastName = "Smith",
            NickName = "Johnny",
            Gender = Gender.Male,
            BirthYear = currentYear - 6,
            BirthMonth = 3,
            BirthDay = 10,
            CreatedDateTime = now
        };
        people.Add(johnnySmith);

        // Jenny Smith (Child, age 4)
        var jennySmith = new Person
        {
            Guid = _jennySmithGuid,
            FirstName = "Jenny",
            LastName = "Smith",
            NickName = "Jenny",
            Gender = Gender.Female,
            BirthYear = currentYear - 4,
            BirthMonth = 11,
            BirthDay = 5,
            Allergies = "Peanuts",
            HasCriticalAllergies = true,
            CreatedDateTime = now
        };
        people.Add(jennySmith);

        // Add all Smith family people
        _context.People.AddRange(johnSmith, janeSmith, johnnySmith, jennySmith);

        // Johnson family (using Family entity, not Group)
        var johnsonFamily = new Family
        {
            Guid = _johnsonFamilyGuid,
            Name = "Johnson Family",
            IsActive = true,
            CreatedDateTime = now
        };
        _context.Families.Add(johnsonFamily);
        // Save Johnson family so we have its ID for members
        await _context.SaveChangesAsync(cancellationToken);

        // Bob Johnson (Adult)
        var bobJohnson = new Person
        {
            Guid = _bobJohnsonGuid,
            FirstName = "Bob",
            LastName = "Johnson",
            NickName = "Bob",
            Gender = Gender.Male,
            BirthYear = 1980,
            BirthMonth = 2,
            BirthDay = 14,
            Email = "bob.johnson@example.com",
            IsEmailActive = true,
            CreatedDateTime = now
        };
        people.Add(bobJohnson);

        // Barbara Johnson (Adult)
        var barbaraJohnson = new Person
        {
            Guid = _barbaraJohnsonGuid,
            FirstName = "Barbara",
            LastName = "Johnson",
            NickName = "Barbara",
            Gender = Gender.Female,
            BirthYear = 1982,
            BirthMonth = 9,
            BirthDay = 30,
            Email = "barbara.johnson@example.com",
            IsEmailActive = true,
            CreatedDateTime = now
        };
        people.Add(barbaraJohnson);

        // Billy Johnson (Child, age 5)
        var billyJohnson = new Person
        {
            Guid = _billyJohnsonGuid,
            FirstName = "Billy",
            LastName = "Johnson",
            NickName = "Billy",
            Gender = Gender.Male,
            BirthYear = currentYear - 5,
            BirthMonth = 7,
            BirthDay = 18,
            CreatedDateTime = now
        };
        people.Add(billyJohnson);

        // Add all Johnson family people
        _context.People.AddRange(bobJohnson, barbaraJohnson, billyJohnson);
        // Save all people so we have their IDs for family members
        await _context.SaveChangesAsync(cancellationToken);

        // Batch add all family members (using FamilyMember entity, not GroupMember)
        var familyMembers = new List<FamilyMember>
        {
            new() { FamilyId = smithFamily.Id, PersonId = johnSmith.Id, FamilyRoleId = adultRoleId, IsPrimary = true, DateAdded = now, CreatedDateTime = now },
            new() { FamilyId = smithFamily.Id, PersonId = janeSmith.Id, FamilyRoleId = adultRoleId, IsPrimary = true, DateAdded = now, CreatedDateTime = now },
            new() { FamilyId = smithFamily.Id, PersonId = johnnySmith.Id, FamilyRoleId = childRoleId, IsPrimary = true, DateAdded = now, CreatedDateTime = now },
            new() { FamilyId = smithFamily.Id, PersonId = jennySmith.Id, FamilyRoleId = childRoleId, IsPrimary = true, DateAdded = now, CreatedDateTime = now },
            new() { FamilyId = johnsonFamily.Id, PersonId = bobJohnson.Id, FamilyRoleId = adultRoleId, IsPrimary = true, DateAdded = now, CreatedDateTime = now },
            new() { FamilyId = johnsonFamily.Id, PersonId = barbaraJohnson.Id, FamilyRoleId = adultRoleId, IsPrimary = true, DateAdded = now, CreatedDateTime = now },
            new() { FamilyId = johnsonFamily.Id, PersonId = billyJohnson.Id, FamilyRoleId = childRoleId, IsPrimary = true, DateAdded = now, CreatedDateTime = now }
        };
        _context.FamilyMembers.AddRange(familyMembers);
        // Save all family members
        await _context.SaveChangesAsync(cancellationToken);

        return people;
    }

    private async Task SeedSecurityRolesAsync(List<Person> people, DateTime now, CancellationToken cancellationToken = default)
    {
        var adminRole = new SecurityRole
        {
            Guid = _adminSecurityRoleGuid,
            Name = "Admin",
            Description = "Full administrative access",
            IsSystemRole = true,
            IsActive = true,
            CreatedDateTime = now
        };

        _context.SecurityRoles.Add(adminRole);
        await _context.SaveChangesAsync(cancellationToken);

        // Assign Admin role to John Smith (first person, the E2E test admin user)
        var johnSmith = people.First(p => p.Email == "john.smith@example.com");
        var adminAssignment = new PersonSecurityRole
        {
            PersonId = johnSmith.Id,
            SecurityRoleId = adminRole.Id,
            CreatedDateTime = now
        };

        _context.PersonSecurityRoles.Add(adminAssignment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private Task SeedCheckinGroupsAsync(int checkinGroupTypeId, int memberRoleId, int scheduleId, DateTime now, CancellationToken cancellationToken = default)
    {
        // Nursery group (ages 0-2)
        var nursery = new Group
        {
            Guid = _nurseryGroupGuid,
            GroupTypeId = checkinGroupTypeId,
            Name = "Nursery",
            Description = "Nursery for infants and toddlers",
            IsSystem = false,
            IsActive = true,
            GroupCapacity = 15,
            MinAgeMonths = 0,
            MaxAgeMonths = 24,
            ScheduleId = scheduleId,
            CreatedDateTime = now
        };

        // Preschool group (ages 3-5)
        var preschool = new Group
        {
            Guid = _preschoolGroupGuid,
            GroupTypeId = checkinGroupTypeId,
            Name = "Preschool",
            Description = "Preschool ministry for ages 3-5",
            IsSystem = false,
            IsActive = true,
            GroupCapacity = 20,
            MinAgeMonths = 36,
            MaxAgeMonths = 71,
            ScheduleId = scheduleId,
            CreatedDateTime = now
        };

        // Elementary group (grades K-5)
        var elementary = new Group
        {
            Guid = _elementaryGroupGuid,
            GroupTypeId = checkinGroupTypeId,
            Name = "Elementary",
            Description = "Elementary ministry for grades K-5",
            IsSystem = false,
            IsActive = true,
            GroupCapacity = 30,
            MinGrade = 0,  // Kindergarten
            MaxGrade = 5,  // 5th grade
            ScheduleId = scheduleId,
            CreatedDateTime = now
        };

        // Batch add all check-in groups
        _context.Groups.AddRange(nursery, preschool, elementary);
        // Note: SaveChangesAsync is called in SeedAsync() after this method returns
        return Task.CompletedTask;
    }
}
