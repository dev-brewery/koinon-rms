using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Interface for application database context.
/// Allows the Application layer to access the database without depending on Infrastructure.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Person> People { get; }
    DbSet<PersonAlias> PersonAliases { get; }
    DbSet<PhoneNumber> PhoneNumbers { get; }
    DbSet<Group> Groups { get; }
    DbSet<GroupType> GroupTypes { get; }
    DbSet<GroupTypeRole> GroupTypeRoles { get; }
    DbSet<GroupMember> GroupMembers { get; }
    DbSet<GroupSchedule> GroupSchedules { get; }
    DbSet<Campus> Campuses { get; }
    DbSet<Location> Locations { get; }
    DbSet<DefinedType> DefinedTypes { get; }
    DbSet<DefinedValue> DefinedValues { get; }
    DbSet<Schedule> Schedules { get; }
    DbSet<Attendance> Attendances { get; }
    DbSet<AttendanceOccurrence> AttendanceOccurrences { get; }
    DbSet<AttendanceCode> AttendanceCodes { get; }
    DbSet<Device> Devices { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<SupervisorSession> SupervisorSessions { get; }
    DbSet<SupervisorAuditLog> SupervisorAuditLogs { get; }
    DbSet<FollowUp> FollowUps { get; }
    DbSet<PagerAssignment> PagerAssignments { get; }
    DbSet<PagerMessage> PagerMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
}
