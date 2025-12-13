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
    private readonly ILogger _logger;

    // Fixed GUIDs for deterministic data
    private static readonly Guid SmithFamilyGuid = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid JohnsonFamilyGuid = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid JohnSmithGuid = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid JaneSmithGuid = new("44444444-4444-4444-4444-444444444444");
    private static readonly Guid JohnnySmithGuid = new("55555555-5555-5555-5555-555555555555");
    private static readonly Guid JennySmithGuid = new("66666666-6666-6666-6666-666666666666");
    private static readonly Guid BobJohnsonGuid = new("77777777-7777-7777-7777-777777777777");
    private static readonly Guid BarbaraJohnsonGuid = new("88888888-8888-8888-8888-888888888888");
    private static readonly Guid BillyJohnsonGuid = new("99999999-9999-9999-9999-999999999999");
    private static readonly Guid NurseryGroupGuid = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid PreschoolGroupGuid = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ElementaryGroupGuid = new("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Sunday9amScheduleGuid = new("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid Sunday11amScheduleGuid = new("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid Wednesday7pmScheduleGuid = new("ffffffff-ffff-ffff-ffff-ffffffffffff");
    private static readonly Guid FamilyGroupTypeGuid = new("10101010-1010-1010-1010-101010101010");
    private static readonly Guid CheckinGroupTypeGuid = new("20202020-2020-2020-2020-202020202020");
    private static readonly Guid AdultRoleGuid = new("30303030-3030-3030-3030-303030303030");
    private static readonly Guid ChildRoleGuid = new("40404040-4040-4040-4040-404040404040");
    private static readonly Guid MemberRoleGuid = new("50505050-5050-5050-5050-505050505050");

    public DataSeeder(KoinonDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
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
            // Table names from pg_tables are safe, but use FormattableString to satisfy EF analyzer
            FormattableString sql = $"TRUNCATE TABLE \"{table}\" RESTART IDENTITY CASCADE;";
            await _context.Database.ExecuteSqlAsync(sql, cancellationToken);
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
        var (smithFamily, johnsonFamily, people) = await SeedFamiliesAsync(familyGroupType.Id, adultRole.Id, childRole.Id, now, cancellationToken);

        // Seed check-in groups
        _logger.LogInformation("Seeding check-in groups...");
        await SeedCheckinGroupsAsync(checkinGroupType.Id, memberRole.Id, sunday9am.Id, now, cancellationToken);

        // Final save for all check-in groups
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("✅ Successfully seeded all test data");
    }

    private async Task<(GroupType familyGroupType, GroupType checkinGroupType, GroupTypeRole adultRole, GroupTypeRole childRole, GroupTypeRole memberRole)> SeedGroupTypesAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        // Family group type
        var familyGroupType = new GroupType
        {
            Guid = FamilyGroupTypeGuid,
            Name = "Family",
            Description = "Family group type for organizing people into family units",
            GroupTerm = "Family",
            GroupMemberTerm = "Family Member",
            IsSystem = true,
            IsFamilyGroupType = true,
            ShowInGroupList = false,
            ShowInNavigation = false,
            TakesAttendance = false,
            CreatedDateTime = now
        };

        // Check-in group type
        var checkinGroupType = new GroupType
        {
            Guid = CheckinGroupTypeGuid,
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

        _context.GroupTypes.Add(familyGroupType);
        _context.GroupTypes.Add(checkinGroupType);
        // Save group types first so we have their IDs for roles
        await _context.SaveChangesAsync(cancellationToken);

        // Roles for family group type
        var adultRole = new GroupTypeRole
        {
            Guid = AdultRoleGuid,
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
            Guid = ChildRoleGuid,
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
            Guid = MemberRoleGuid,
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
            Guid = Sunday9amScheduleGuid,
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
            Guid = Sunday11amScheduleGuid,
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
            Guid = Wednesday7pmScheduleGuid,
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

    private async Task<(Group smithFamily, Group johnsonFamily, List<Person> people)> SeedFamiliesAsync(
        int familyGroupTypeId, int adultRoleId, int childRoleId, DateTime now, CancellationToken cancellationToken = default)
    {
        var people = new List<Person>();

        // Smith family
        var smithFamily = new Group
        {
            Guid = SmithFamilyGuid,
            GroupTypeId = familyGroupTypeId,
            Name = "Smith Family",
            IsSystem = false,
            IsActive = true,
            CreatedDateTime = now
        };
        _context.Groups.Add(smithFamily);
        // Save family group so we have its ID for people
        await _context.SaveChangesAsync(cancellationToken);

        // John Smith (Adult)
        var johnSmith = new Person
        {
            Guid = JohnSmithGuid,
            FirstName = "John",
            LastName = "Smith",
            NickName = "John",
            Gender = Gender.Male,
            BirthYear = 1985,
            BirthMonth = 6,
            BirthDay = 15,
            Email = "john.smith@example.com",
            IsEmailActive = true,
            PrimaryFamilyId = smithFamily.Id,
            CreatedDateTime = now
        };
        people.Add(johnSmith);

        // Jane Smith (Adult)
        var janeSmith = new Person
        {
            Guid = JaneSmithGuid,
            FirstName = "Jane",
            LastName = "Smith",
            NickName = "Jane",
            Gender = Gender.Female,
            BirthYear = 1987,
            BirthMonth = 8,
            BirthDay = 22,
            Email = "jane.smith@example.com",
            IsEmailActive = true,
            PrimaryFamilyId = smithFamily.Id,
            CreatedDateTime = now
        };
        people.Add(janeSmith);

        // Johnny Smith (Child, age 6)
        var currentYear = DateTime.UtcNow.Year;
        var johnnySmith = new Person
        {
            Guid = JohnnySmithGuid,
            FirstName = "Johnny",
            LastName = "Smith",
            NickName = "Johnny",
            Gender = Gender.Male,
            BirthYear = currentYear - 6,
            BirthMonth = 3,
            BirthDay = 10,
            PrimaryFamilyId = smithFamily.Id,
            CreatedDateTime = now
        };
        people.Add(johnnySmith);

        // Jenny Smith (Child, age 4)
        var jennySmith = new Person
        {
            Guid = JennySmithGuid,
            FirstName = "Jenny",
            LastName = "Smith",
            NickName = "Jenny",
            Gender = Gender.Female,
            BirthYear = currentYear - 4,
            BirthMonth = 11,
            BirthDay = 5,
            PrimaryFamilyId = smithFamily.Id,
            Allergies = "Peanuts",
            HasCriticalAllergies = true,
            CreatedDateTime = now
        };
        people.Add(jennySmith);

        // Add all Smith family people
        _context.People.AddRange(johnSmith, janeSmith, johnnySmith, jennySmith);

        // Johnson family
        var johnsonFamily = new Group
        {
            Guid = JohnsonFamilyGuid,
            GroupTypeId = familyGroupTypeId,
            Name = "Johnson Family",
            IsSystem = false,
            IsActive = true,
            CreatedDateTime = now
        };
        _context.Groups.Add(johnsonFamily);
        // Save Johnson family group so we have its ID for people
        await _context.SaveChangesAsync(cancellationToken);

        // Bob Johnson (Adult)
        var bobJohnson = new Person
        {
            Guid = BobJohnsonGuid,
            FirstName = "Bob",
            LastName = "Johnson",
            NickName = "Bob",
            Gender = Gender.Male,
            BirthYear = 1980,
            BirthMonth = 2,
            BirthDay = 14,
            Email = "bob.johnson@example.com",
            IsEmailActive = true,
            PrimaryFamilyId = johnsonFamily.Id,
            CreatedDateTime = now
        };
        people.Add(bobJohnson);

        // Barbara Johnson (Adult)
        var barbaraJohnson = new Person
        {
            Guid = BarbaraJohnsonGuid,
            FirstName = "Barbara",
            LastName = "Johnson",
            NickName = "Barbara",
            Gender = Gender.Female,
            BirthYear = 1982,
            BirthMonth = 9,
            BirthDay = 30,
            Email = "barbara.johnson@example.com",
            IsEmailActive = true,
            PrimaryFamilyId = johnsonFamily.Id,
            CreatedDateTime = now
        };
        people.Add(barbaraJohnson);

        // Billy Johnson (Child, age 5)
        var billyJohnson = new Person
        {
            Guid = BillyJohnsonGuid,
            FirstName = "Billy",
            LastName = "Johnson",
            NickName = "Billy",
            Gender = Gender.Male,
            BirthYear = currentYear - 5,
            BirthMonth = 7,
            BirthDay = 18,
            PrimaryFamilyId = johnsonFamily.Id,
            CreatedDateTime = now
        };
        people.Add(billyJohnson);

        // Add all Johnson family people
        _context.People.AddRange(bobJohnson, barbaraJohnson, billyJohnson);
        // Save all people so we have their IDs for group members
        await _context.SaveChangesAsync(cancellationToken);

        // Batch add all group members for both families
        var groupMembers = new List<GroupMember>
        {
            new() { GroupId = smithFamily.Id, PersonId = johnSmith.Id, GroupRoleId = adultRoleId, GroupMemberStatus = GroupMemberStatus.Active, DateTimeAdded = now, CreatedDateTime = now },
            new() { GroupId = smithFamily.Id, PersonId = janeSmith.Id, GroupRoleId = adultRoleId, GroupMemberStatus = GroupMemberStatus.Active, DateTimeAdded = now, CreatedDateTime = now },
            new() { GroupId = smithFamily.Id, PersonId = johnnySmith.Id, GroupRoleId = childRoleId, GroupMemberStatus = GroupMemberStatus.Active, DateTimeAdded = now, CreatedDateTime = now },
            new() { GroupId = smithFamily.Id, PersonId = jennySmith.Id, GroupRoleId = childRoleId, GroupMemberStatus = GroupMemberStatus.Active, DateTimeAdded = now, CreatedDateTime = now },
            new() { GroupId = johnsonFamily.Id, PersonId = bobJohnson.Id, GroupRoleId = adultRoleId, GroupMemberStatus = GroupMemberStatus.Active, DateTimeAdded = now, CreatedDateTime = now },
            new() { GroupId = johnsonFamily.Id, PersonId = barbaraJohnson.Id, GroupRoleId = adultRoleId, GroupMemberStatus = GroupMemberStatus.Active, DateTimeAdded = now, CreatedDateTime = now },
            new() { GroupId = johnsonFamily.Id, PersonId = billyJohnson.Id, GroupRoleId = childRoleId, GroupMemberStatus = GroupMemberStatus.Active, DateTimeAdded = now, CreatedDateTime = now }
        };
        _context.GroupMembers.AddRange(groupMembers);
        // Save all group members
        await _context.SaveChangesAsync(cancellationToken);

        return (smithFamily, johnsonFamily, people);
    }

    private Task SeedCheckinGroupsAsync(int checkinGroupTypeId, int memberRoleId, int scheduleId, DateTime now, CancellationToken cancellationToken = default)
    {
        // Nursery group (ages 0-2)
        var nursery = new Group
        {
            Guid = NurseryGroupGuid,
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
            Guid = PreschoolGroupGuid,
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
            Guid = ElementaryGroupGuid,
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
