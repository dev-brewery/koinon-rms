using AutoMapper;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.PersonMerge;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for merging duplicate person records and updating all foreign key references.
/// </summary>
public class PersonMergeService(
    IApplicationDbContext context,
    IPersonService personService,
    ILogger<PersonMergeService> logger) : IPersonMergeService
{
    public async Task<PersonComparisonDto?> ComparePersonsAsync(
        string person1IdKey,
        string person2IdKey,
        CancellationToken ct = default)
    {
        logger.LogInformation("Comparing persons {Person1IdKey} and {Person2IdKey}",
            person1IdKey, person2IdKey);

        if (!IdKeyHelper.TryDecode(person1IdKey, out int person1Id) ||
            !IdKeyHelper.TryDecode(person2IdKey, out int person2Id))
        {
            logger.LogWarning("Invalid IdKey provided");
            return null;
        }

        var person1Dto = await personService.GetByIdAsync(person1Id, ct);
        var person2Dto = await personService.GetByIdAsync(person2Id, ct);

        if (person1Dto == null || person2Dto == null)
        {
            logger.LogWarning("One or both persons not found");
            return null;
        }

        // Get attendance counts
        var person1AttendanceCount = await context.Attendances
            .CountAsync(a => a.PersonAliasId != null &&
                            a.PersonAlias!.PersonId == person1Id, ct);

        var person2AttendanceCount = await context.Attendances
            .CountAsync(a => a.PersonAliasId != null &&
                            a.PersonAlias!.PersonId == person2Id, ct);

        // Get group membership counts
        var person1GroupCount = await context.GroupMembers
            .CountAsync(gm => gm.PersonId == person1Id && !gm.IsArchived, ct);

        var person2GroupCount = await context.GroupMembers
            .CountAsync(gm => gm.PersonId == person2Id && !gm.IsArchived, ct);

        // Get contribution totals
        var person1ContributionTotal = await context.Contributions
            .Where(c => c.PersonAliasId != null && c.PersonAlias!.PersonId == person1Id)
            .SelectMany(c => c.ContributionDetails)
            .SumAsync(d => (decimal?)d.Amount, ct) ?? 0m;

        var person2ContributionTotal = await context.Contributions
            .Where(c => c.PersonAliasId != null && c.PersonAlias!.PersonId == person2Id)
            .SelectMany(c => c.ContributionDetails)
            .SumAsync(d => (decimal?)d.Amount, ct) ?? 0m;

        return new PersonComparisonDto
        {
            Person1 = person1Dto,
            Person2 = person2Dto,
            Person1AttendanceCount = person1AttendanceCount,
            Person2AttendanceCount = person2AttendanceCount,
            Person1GroupMembershipCount = person1GroupCount,
            Person2GroupMembershipCount = person2GroupCount,
            Person1ContributionTotal = person1ContributionTotal,
            Person2ContributionTotal = person2ContributionTotal
        };
    }

    public async Task<Result<PersonMergeResultDto>> MergeAsync(
        PersonMergeRequestDto request,
        int currentUserId,
        CancellationToken ct = default)
    {
        logger.LogInformation("Merging person {MergedIdKey} into {SurvivorIdKey}",
            request.MergedIdKey, request.SurvivorIdKey);

        // Decode IdKeys
        if (!IdKeyHelper.TryDecode(request.SurvivorIdKey, out int survivorId) ||
            !IdKeyHelper.TryDecode(request.MergedIdKey, out int mergedId))
        {
            return Result<PersonMergeResultDto>.Failure(
                Error.Validation("Invalid IdKey format"));
        }

        if (survivorId == mergedId)
        {
            return Result<PersonMergeResultDto>.Failure(
                Error.Validation("Cannot merge a person with themselves"));
        }

        using var transaction = await context.Database.BeginTransactionAsync(ct);

        try
        {
            // Get both persons
            var survivor = await context.People
                .Include(p => p.PhoneNumbers)
                .FirstOrDefaultAsync(p => p.Id == survivorId, ct);

            var merged = await context.People
                .Include(p => p.PhoneNumbers)
                .FirstOrDefaultAsync(p => p.Id == mergedId, ct);

            if (survivor == null)
            {
                return Result<PersonMergeResultDto>.Failure(
                    Error.NotFound("Person", request.SurvivorIdKey));
            }

            if (merged == null)
            {
                return Result<PersonMergeResultDto>.Failure(
                    Error.NotFound("Person", request.MergedIdKey));
            }

            var result = new PersonMergeResultDto
            {
                SurvivorIdKey = request.SurvivorIdKey,
                MergedIdKey = request.MergedIdKey,
                MergedDateTime = DateTime.UtcNow
            };

            // Apply field selections
            if (request.FieldSelections != null)
            {
                ApplyFieldSelections(survivor, merged, request.FieldSelections);
            }

            // Update PersonAlias records
            var aliasesUpdated = await context.PersonAliases
                .Where(pa => pa.PersonId == mergedId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(pa => pa.PersonId, survivorId), ct);
            result.AliasesUpdated = aliasesUpdated;

            // Update GroupMember records (handle duplicates)
            var mergedGroupMembers = await context.GroupMembers
                .Where(gm => gm.PersonId == mergedId)
                .ToListAsync(ct);

            var survivorGroupIds = await context.GroupMembers
                .Where(gm => gm.PersonId == survivorId)
                .Select(gm => gm.GroupId)
                .ToListAsync(ct);
            var survivorGroupIdsSet = survivorGroupIds.ToHashSet();

            int groupMembershipsUpdated = 0;
            foreach (var gm in mergedGroupMembers)
            {
                if (survivorGroupIdsSet.Contains(gm.GroupId))
                {
                    // Skip duplicate - survivor already member of this group
                    logger.LogDebug("Skipping duplicate group membership for group {GroupId}", gm.GroupId);
                }
                else
                {
                    gm.PersonId = survivorId;
                    groupMembershipsUpdated++;
                }
            }
            result.GroupMembershipsUpdated = groupMembershipsUpdated;

            // Update FamilyMember records
            var familyMembersUpdated = await context.FamilyMembers
                .Where(fm => fm.PersonId == mergedId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(fm => fm.PersonId, survivorId), ct);
            result.FamilyMembershipsUpdated = familyMembersUpdated;

            // Update PhoneNumber records
            var phoneNumbersUpdated = await context.PhoneNumbers
                .Where(pn => pn.PersonId == mergedId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(pn => pn.PersonId, survivorId), ct);
            result.PhoneNumbersUpdated = phoneNumbersUpdated;

            // Update AuthorizedPickup records (child person)
            var childPickupsUpdated = await context.AuthorizedPickups
                .Where(ap => ap.ChildPersonId == mergedId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(ap => ap.ChildPersonId, survivorId), ct);

            // Update AuthorizedPickup records (authorized person)
            var authorizedPickupsUpdated = await context.AuthorizedPickups
                .Where(ap => ap.AuthorizedPersonId == mergedId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(ap => ap.AuthorizedPersonId, survivorId), ct);

            result.AuthorizedPickupsUpdated = childPickupsUpdated + authorizedPickupsUpdated;

            // Update CommunicationPreference records
            var communicationPreferencesUpdated = await context.CommunicationPreferences
                .Where(cp => cp.PersonId == mergedId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(cp => cp.PersonId, survivorId), ct);
            result.CommunicationPreferencesUpdated = communicationPreferencesUpdated;

            // Update RefreshToken records
            var refreshTokensUpdated = await context.RefreshTokens
                .Where(rt => rt.PersonId == mergedId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(rt => rt.PersonId, survivorId), ct);
            result.RefreshTokensUpdated = refreshTokensUpdated;

            // Update PersonSecurityRole records
            var securityRolesUpdated = await context.PersonSecurityRoles
                .Where(psr => psr.PersonId == mergedId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(psr => psr.PersonId, survivorId), ct);
            result.SecurityRolesUpdated = securityRolesUpdated;

            // Update SupervisorSession records
            var supervisorSessionsUpdated = await context.SupervisorSessions
                .Where(ss => ss.PersonId == mergedId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(ss => ss.PersonId, survivorId), ct);
            result.SupervisorSessionsUpdated = supervisorSessionsUpdated;

            // Update FollowUp records (person)
            var followUpPersonUpdated = await context.FollowUps
                .Where(f => f.PersonId == mergedId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(f => f.PersonId, survivorId), ct);

            // Update FollowUp records (assigned to)
            var followUpAssignedUpdated = await context.FollowUps
                .Where(f => f.AssignedToPersonId == mergedId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(f => f.AssignedToPersonId, survivorId), ct);

            result.FollowUpsUpdated = followUpPersonUpdated + followUpAssignedUpdated;

            // Calculate total
            result.TotalRecordsUpdated = result.AliasesUpdated +
                                        result.GroupMembershipsUpdated +
                                        result.FamilyMembershipsUpdated +
                                        result.PhoneNumbersUpdated +
                                        result.AuthorizedPickupsUpdated +
                                        result.CommunicationPreferencesUpdated +
                                        result.RefreshTokensUpdated +
                                        result.SecurityRolesUpdated +
                                        result.SupervisorSessionsUpdated +
                                        result.FollowUpsUpdated;

            // Create PersonMergeHistory record
            var history = new PersonMergeHistory
            {
                SurvivorPersonId = survivorId,
                MergedPersonId = mergedId,
                MergedByPersonId = currentUserId,
                MergedDateTime = result.MergedDateTime,
                Notes = request.Notes
            };
            context.PersonMergeHistories.Add(history);

            // Mark merged person as inactive
            var inactiveStatusValue = await context.DefinedValues
                .FirstOrDefaultAsync(dv => dv.DefinedType!.Name == "RecordStatus" &&
                                          dv.Value == "Inactive", ct);

            if (inactiveStatusValue != null)
            {
                merged.RecordStatusValueId = inactiveStatusValue.Id;
            }

            // Save all changes
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            logger.LogInformation("Successfully merged person {MergedIdKey} into {SurvivorIdKey}. " +
                                "Updated {TotalRecords} records",
                request.MergedIdKey, request.SurvivorIdKey, result.TotalRecordsUpdated);

            return Result<PersonMergeResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            logger.LogError(ex, "Error merging persons {MergedIdKey} into {SurvivorIdKey}",
                request.MergedIdKey, request.SurvivorIdKey);
            return Result<PersonMergeResultDto>.Failure(
                Error.Internal("An error occurred while merging persons", ex.Message));
        }
    }

    public async Task<PagedResult<PersonMergeHistoryDto>> GetMergeHistoryAsync(
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        logger.LogInformation("Getting merge history (page {Page}, pageSize {PageSize})",
            page, pageSize);

        var query = context.PersonMergeHistories
            .AsNoTracking()
            .Include(h => h.SurvivorPerson)
            .Include(h => h.MergedPerson)
            .Include(h => h.MergedByPerson)
            .OrderByDescending(h => h.MergedDateTime);

        var totalCount = await query.CountAsync(ct);

        var histories = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = histories.Select(h => new PersonMergeHistoryDto
        {
            IdKey = h.IdKey,
            SurvivorIdKey = h.SurvivorPerson?.IdKey ?? "",
            SurvivorName = h.SurvivorPerson?.FullName ?? "(Unknown)",
            MergedIdKey = h.MergedPerson?.IdKey ?? "",
            MergedName = h.MergedPerson?.FullName ?? "(Unknown)",
            MergedByIdKey = h.MergedByPerson?.IdKey,
            MergedByName = h.MergedByPerson?.FullName,
            MergedDateTime = h.MergedDateTime,
            Notes = h.Notes
        }).ToList();

        return new PagedResult<PersonMergeHistoryDto>(dtos, totalCount, page, pageSize);
    }

    /// <summary>
    /// Applies field selections from the merge request to update the survivor person.
    /// </summary>
    private void ApplyFieldSelections(Person survivor, Person merged, Dictionary<string, string> fieldSelections)
    {
        foreach (var selection in fieldSelections)
        {
            var fieldName = selection.Key;
            var source = selection.Value.ToLowerInvariant();

            if (source != "merged")
                continue; // Keep survivor's value

            // Apply merged person's value to survivor for selected fields
            switch (fieldName)
            {
                case "FirstName":
                    survivor.FirstName = merged.FirstName;
                    break;
                case "NickName":
                    survivor.NickName = merged.NickName;
                    break;
                case "MiddleName":
                    survivor.MiddleName = merged.MiddleName;
                    break;
                case "LastName":
                    survivor.LastName = merged.LastName;
                    break;
                case "Email":
                    survivor.Email = merged.Email;
                    survivor.IsEmailActive = merged.IsEmailActive;
                    break;
                case "Gender":
                    survivor.Gender = merged.Gender;
                    break;
                case "BirthDate":
                    survivor.BirthDay = merged.BirthDay;
                    survivor.BirthMonth = merged.BirthMonth;
                    survivor.BirthYear = merged.BirthYear;
                    break;
                case "ConnectionStatus":
                    survivor.ConnectionStatusValueId = merged.ConnectionStatusValueId;
                    break;
                case "MaritalStatus":
                    survivor.MaritalStatusValueId = merged.MaritalStatusValueId;
                    break;
                case "AnniversaryDate":
                    survivor.AnniversaryDate = merged.AnniversaryDate;
                    break;
                case "GraduationYear":
                    survivor.GraduationYear = merged.GraduationYear;
                    break;
                case "Photo":
                    survivor.PhotoId = merged.PhotoId;
                    break;
                case "PrimaryCampus":
                    survivor.PrimaryCampusId = merged.PrimaryCampusId;
                    break;
                case "Allergies":
                    survivor.Allergies = merged.Allergies;
                    survivor.HasCriticalAllergies = merged.HasCriticalAllergies;
                    break;
                case "SpecialNeeds":
                    survivor.SpecialNeeds = merged.SpecialNeeds;
                    break;
            }
        }

        survivor.ModifiedDateTime = DateTime.UtcNow;
    }
}
