using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

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
    DbSet<GroupMemberRequest> GroupMemberRequests { get; }
    DbSet<GroupSchedule> GroupSchedules { get; }
    DbSet<Family> Families { get; }
    DbSet<FamilyMember> FamilyMembers { get; }
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
    DbSet<AuthorizedPickup> AuthorizedPickups { get; }
    DbSet<PickupLog> PickupLogs { get; }
    DbSet<Communication> Communications { get; }
    DbSet<CommunicationRecipient> CommunicationRecipients { get; }
    DbSet<CommunicationTemplate> CommunicationTemplates { get; }
    DbSet<CommunicationPreference> CommunicationPreferences { get; }
    DbSet<BinaryFile> BinaryFiles { get; }
    DbSet<ImportTemplate> ImportTemplates { get; }
    DbSet<ImportJob> ImportJobs { get; }

    // Security/RBAC
    DbSet<SecurityRole> SecurityRoles { get; }
    DbSet<SecurityClaim> SecurityClaims { get; }
    DbSet<PersonSecurityRole> PersonSecurityRoles { get; }
    DbSet<RoleSecurityClaim> RoleSecurityClaims { get; }

    // Giving/Financial
    DbSet<Fund> Funds { get; }
    DbSet<ContributionBatch> ContributionBatches { get; }
    DbSet<Contribution> Contributions { get; }
    DbSet<ContributionDetail> ContributionDetails { get; }
    DbSet<ContributionStatement> ContributionStatements { get; }
    DbSet<FinancialAuditLog> FinancialAuditLogs { get; }
    DbSet<AuditLog> AuditLogs { get; }

    // Person merge and duplicate tracking
    DbSet<PersonMergeHistory> PersonMergeHistories { get; }
    DbSet<PersonDuplicateIgnore> PersonDuplicateIgnores { get; }

    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
}
