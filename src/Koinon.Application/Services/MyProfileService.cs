using FluentValidation;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for self-service profile management.
/// Allows authenticated users to view and update their own profile and family information.
/// </summary>
public class MyProfileService(
    IApplicationDbContext context,
    IUserContext userContext,
    IValidator<UpdateMyProfileRequest> updateProfileValidator,
    IValidator<UpdateFamilyMemberRequest> updateFamilyMemberValidator,
    ILogger<MyProfileService> logger) : IMyProfileService
{
    public async Task<Result<MyProfileDto>> GetMyProfileAsync(CancellationToken ct = default)
    {
        var currentPersonId = userContext.CurrentPersonId;
        if (!currentPersonId.HasValue)
        {
            return Result<MyProfileDto>.Failure(
                Error.Forbidden("User is not authenticated"));
        }

        var person = await context.People
            .AsNoTracking()
            .Include(p => p.PhoneNumbers)
                .ThenInclude(pn => pn.NumberTypeValue)
            .Include(p => p.PrimaryFamily)
            .Include(p => p.PrimaryCampus)
            .FirstOrDefaultAsync(p => p.Id == currentPersonId.Value, ct);

        if (person == null)
        {
            return Result<MyProfileDto>.Failure(
                Error.NotFound("Person", IdKeyHelper.Encode(currentPersonId.Value)));
        }

        var dto = MapToMyProfileDto(person);

        logger.LogInformation(
            "Retrieved profile for PersonIdKey={PersonIdKey}",
            person.IdKey);

        return Result<MyProfileDto>.Success(dto);
    }

    public async Task<Result<MyProfileDto>> UpdateMyProfileAsync(
        UpdateMyProfileRequest request,
        CancellationToken ct = default)
    {
        // Validate request
        var validationResult = await updateProfileValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Result<MyProfileDto>.Failure(Error.FromFluentValidation(validationResult));
        }

        var currentPersonId = userContext.CurrentPersonId;
        if (!currentPersonId.HasValue)
        {
            return Result<MyProfileDto>.Failure(
                Error.Forbidden("User is not authenticated"));
        }

        var person = await context.People
            .Include(p => p.PhoneNumbers)
                .ThenInclude(pn => pn.NumberTypeValue)
            .Include(p => p.PrimaryFamily)
            .Include(p => p.PrimaryCampus)
            .FirstOrDefaultAsync(p => p.Id == currentPersonId.Value, ct);

        if (person == null)
        {
            return Result<MyProfileDto>.Failure(
                Error.NotFound("Person", IdKeyHelper.Encode(currentPersonId.Value)));
        }

        // Update allowed fields only
        if (request.Email != null)
        {
            person.Email = request.Email;
        }

        if (request.EmailPreference != null)
        {
            person.EmailPreference = request.EmailPreference switch
            {
                "EmailAllowed" => EmailPreference.EmailAllowed,
                "NoMassEmails" => EmailPreference.NoMassEmails,
                "DoNotEmail" => EmailPreference.DoNotEmail,
                _ => person.EmailPreference
            };
        }

        if (request.NickName != null)
        {
            person.NickName = request.NickName;
        }

        // Update phone numbers
        if (request.PhoneNumbers != null)
        {
            await UpdatePhoneNumbersAsync(person, request.PhoneNumbers, ct);
        }

        person.ModifiedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        // Reload to get updated data with navigation properties
        await context.Entry(person).ReloadAsync(ct);
        await context.Entry(person)
            .Collection(p => p.PhoneNumbers)
            .Query()
            .Include(pn => pn.NumberTypeValue)
            .LoadAsync(ct);

        var dto = MapToMyProfileDto(person);

        logger.LogInformation(
            "Updated profile for PersonIdKey={PersonIdKey}",
            person.IdKey);

        return Result<MyProfileDto>.Success(dto);
    }

    public async Task<Result<IReadOnlyList<MyFamilyMemberDto>>> GetMyFamilyAsync(CancellationToken ct = default)
    {
        var currentPersonId = userContext.CurrentPersonId;
        if (!currentPersonId.HasValue)
        {
            return Result<IReadOnlyList<MyFamilyMemberDto>>.Failure(
                Error.Forbidden("User is not authenticated"));
        }

        var person = await context.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == currentPersonId.Value, ct);

        if (person == null || !person.PrimaryFamilyId.HasValue)
        {
            return Result<IReadOnlyList<MyFamilyMemberDto>>.Success(
                Array.Empty<MyFamilyMemberDto>());
        }

        // Get current user's role in family to determine edit permissions
        var currentUserRole = await context.GroupMembers
            .AsNoTracking()
            .Include(gm => gm.GroupRole)
            .Where(gm => gm.GroupId == person.PrimaryFamilyId.Value
                && gm.PersonId == currentPersonId.Value
                && gm.GroupMemberStatus == GroupMemberStatus.Active)
            .Select(gm => gm.GroupRole)
            .FirstOrDefaultAsync(ct);

        bool currentUserIsAdult = currentUserRole?.Guid == SystemGuid.GroupTypeRole.FamilyAdult;

        // Get family members
        var familyMembers = await context.GroupMembers
            .AsNoTracking()
            .Include(gm => gm.Person)
                .ThenInclude(p => p!.PhoneNumbers)
                    .ThenInclude(pn => pn.NumberTypeValue)
            .Include(gm => gm.GroupRole)
            .Where(gm => gm.GroupId == person.PrimaryFamilyId.Value
                && gm.GroupMemberStatus == GroupMemberStatus.Active)
            .OrderByDescending(gm => gm.GroupRole!.Guid == SystemGuid.GroupTypeRole.FamilyAdult)
            .ThenBy(gm => gm.Person!.BirthYear)
            .ThenBy(gm => gm.Person!.BirthMonth)
            .ThenBy(gm => gm.Person!.BirthDay)
            .ToListAsync(ct);

        var dtos = familyMembers.Select(fm =>
        {
            var isChild = fm.GroupRole?.Guid == SystemGuid.GroupTypeRole.FamilyChild;
            var canEdit = currentUserIsAdult && isChild;

            return new MyFamilyMemberDto
            {
                IdKey = fm.Person!.IdKey,
                FirstName = fm.Person.FirstName,
                NickName = fm.Person.NickName,
                LastName = fm.Person.LastName,
                FullName = fm.Person.FullName,
                BirthDate = fm.Person.BirthDate,
                Age = fm.Person.BirthDate.HasValue
                    ? DateTime.UtcNow.Year - fm.Person.BirthDate.Value.Year
                    : null,
                Gender = fm.Person.Gender.ToString(),
                Email = fm.Person.Email,
                PhoneNumbers = fm.Person.PhoneNumbers
                    .OrderBy(pn => pn.NumberTypeValue?.Order ?? 999)
                    .Select(pn => new PhoneNumberDto
                    {
                        IdKey = pn.IdKey,
                        Number = pn.Number,
                        NumberFormatted = FormatPhoneNumber(pn.Number),
                        Extension = pn.Extension,
                        PhoneType = pn.NumberTypeValue != null ? new DefinedValueDto
                        {
                            IdKey = pn.NumberTypeValue.IdKey,
                            Guid = pn.NumberTypeValue.Guid,
                            Value = pn.NumberTypeValue.Value,
                            Description = pn.NumberTypeValue.Description,
                            IsActive = pn.NumberTypeValue.IsActive,
                            Order = pn.NumberTypeValue.Order
                        } : null,
                        IsMessagingEnabled = pn.IsMessagingEnabled,
                        IsUnlisted = pn.IsUnlisted
                    })
                    .ToList(),
                PhotoUrl = null, // TODO(#116): Implement photo URLs
                FamilyRole = fm.GroupRole?.Name ?? "Unknown",
                CanEdit = canEdit,
                Allergies = fm.Person.Allergies,
                HasCriticalAllergies = fm.Person.HasCriticalAllergies,
                SpecialNeeds = fm.Person.SpecialNeeds
            };
        }).ToList();

        logger.LogInformation(
            "Retrieved {Count} family members for PersonIdKey={PersonIdKey}",
            dtos.Count,
            IdKeyHelper.Encode(currentPersonId.Value));

        return Result<IReadOnlyList<MyFamilyMemberDto>>.Success(dtos);
    }

    public async Task<Result<MyFamilyMemberDto>> UpdateFamilyMemberAsync(
        string personIdKey,
        UpdateFamilyMemberRequest request,
        CancellationToken ct = default)
    {
        // Validate request
        var validationResult = await updateFamilyMemberValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Result<MyFamilyMemberDto>.Failure(Error.FromFluentValidation(validationResult));
        }

        var currentPersonId = userContext.CurrentPersonId;
        if (!currentPersonId.HasValue)
        {
            return Result<MyFamilyMemberDto>.Failure(
                Error.Forbidden("User is not authenticated"));
        }

        if (!IdKeyHelper.TryDecode(personIdKey, out int targetPersonId))
        {
            return Result<MyFamilyMemberDto>.Failure(Error.NotFound("Person", personIdKey));
        }

        // Get current user
        var currentPerson = await context.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == currentPersonId.Value, ct);

        if (currentPerson == null || !currentPerson.PrimaryFamilyId.HasValue)
        {
            return Result<MyFamilyMemberDto>.Failure(
                Error.Forbidden("You must be part of a family to update family members"));
        }

        // Get current user's role
        var currentUserMembership = await context.GroupMembers
            .AsNoTracking()
            .Include(gm => gm.GroupRole)
            .Where(gm => gm.GroupId == currentPerson.PrimaryFamilyId.Value
                && gm.PersonId == currentPersonId.Value
                && gm.GroupMemberStatus == GroupMemberStatus.Active)
            .FirstOrDefaultAsync(ct);

        if (currentUserMembership?.GroupRole?.Guid != SystemGuid.GroupTypeRole.FamilyAdult)
        {
            return Result<MyFamilyMemberDto>.Failure(
                Error.Forbidden("Only adults can update family member information"));
        }

        // Get target person
        var targetPerson = await context.People
            .Include(p => p.PhoneNumbers)
                .ThenInclude(pn => pn.NumberTypeValue)
            .FirstOrDefaultAsync(p => p.Id == targetPersonId, ct);

        if (targetPerson == null)
        {
            return Result<MyFamilyMemberDto>.Failure(Error.NotFound("Person", personIdKey));
        }

        // Verify target person is in same family
        var targetMembership = await context.GroupMembers
            .AsNoTracking()
            .Include(gm => gm.GroupRole)
            .Where(gm => gm.GroupId == currentPerson.PrimaryFamilyId.Value
                && gm.PersonId == targetPersonId
                && gm.GroupMemberStatus == GroupMemberStatus.Active)
            .FirstOrDefaultAsync(ct);

        if (targetMembership == null)
        {
            return Result<MyFamilyMemberDto>.Failure(
                Error.Forbidden("Person is not part of your family"));
        }

        // Verify target is a child
        if (targetMembership.GroupRole?.Guid != SystemGuid.GroupTypeRole.FamilyChild)
        {
            return Result<MyFamilyMemberDto>.Failure(
                Error.Forbidden("You can only update information for children in your family"));
        }

        // Update allowed fields
        if (request.NickName != null)
        {
            targetPerson.NickName = request.NickName;
        }

        if (request.Allergies != null)
        {
            targetPerson.Allergies = request.Allergies;
        }

        if (request.HasCriticalAllergies.HasValue)
        {
            targetPerson.HasCriticalAllergies = request.HasCriticalAllergies.Value;
        }

        if (request.SpecialNeeds != null)
        {
            targetPerson.SpecialNeeds = request.SpecialNeeds;
        }

        // Update phone numbers
        if (request.PhoneNumbers != null)
        {
            await UpdatePhoneNumbersAsync(targetPerson, request.PhoneNumbers, ct);
        }

        targetPerson.ModifiedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        // Reload to get updated data
        await context.Entry(targetPerson).ReloadAsync(ct);
        await context.Entry(targetPerson)
            .Collection(p => p.PhoneNumbers)
            .Query()
            .Include(pn => pn.NumberTypeValue)
            .LoadAsync(ct);

        var dto = new MyFamilyMemberDto
        {
            IdKey = targetPerson.IdKey,
            FirstName = targetPerson.FirstName,
            NickName = targetPerson.NickName,
            LastName = targetPerson.LastName,
            FullName = targetPerson.FullName,
            BirthDate = targetPerson.BirthDate,
            Age = targetPerson.BirthDate.HasValue
                ? DateTime.UtcNow.Year - targetPerson.BirthDate.Value.Year
                : null,
            Gender = targetPerson.Gender.ToString(),
            Email = targetPerson.Email,
            PhoneNumbers = targetPerson.PhoneNumbers
                .OrderBy(pn => pn.NumberTypeValue?.Order ?? 999)
                .Select(pn => new PhoneNumberDto
                {
                    IdKey = pn.IdKey,
                    Number = pn.Number,
                    NumberFormatted = FormatPhoneNumber(pn.Number),
                    Extension = pn.Extension,
                    PhoneType = pn.NumberTypeValue != null ? new DefinedValueDto
                    {
                        IdKey = pn.NumberTypeValue.IdKey,
                        Guid = pn.NumberTypeValue.Guid,
                        Value = pn.NumberTypeValue.Value,
                        Description = pn.NumberTypeValue.Description,
                        IsActive = pn.NumberTypeValue.IsActive,
                        Order = pn.NumberTypeValue.Order
                    } : null,
                    IsMessagingEnabled = pn.IsMessagingEnabled,
                    IsUnlisted = pn.IsUnlisted
                })
                .ToList(),
            PhotoUrl = null,
            FamilyRole = targetMembership.GroupRole?.Name ?? "Child",
            CanEdit = true,
            Allergies = targetPerson.Allergies,
            HasCriticalAllergies = targetPerson.HasCriticalAllergies,
            SpecialNeeds = targetPerson.SpecialNeeds
        };

        logger.LogInformation(
            "Updated family member PersonIdKey={PersonIdKey} by PersonIdKey={CurrentPersonIdKey}",
            targetPerson.IdKey,
            IdKeyHelper.Encode(currentPersonId.Value));

        return Result<MyFamilyMemberDto>.Success(dto);
    }

    public async Task<Result<MyInvolvementDto>> GetMyInvolvementAsync(CancellationToken ct = default)
    {
        var currentPersonId = userContext.CurrentPersonId;
        if (!currentPersonId.HasValue)
        {
            return Result<MyInvolvementDto>.Failure(
                Error.Forbidden("User is not authenticated"));
        }

        // Get groups (excluding family)
        var groupMemberships = await context.GroupMembers
            .AsNoTracking()
            .Include(gm => gm.Group)
                .ThenInclude(g => g!.GroupType)
            .Include(gm => gm.Group)
                .ThenInclude(g => g!.Campus)
            .Include(gm => gm.GroupRole)
            .Where(gm => gm.PersonId == currentPersonId.Value
                && gm.GroupMemberStatus == GroupMemberStatus.Active
                && !gm.Group!.IsArchived
                && !gm.Group.GroupType!.IsFamilyGroupType)
            .ToListAsync(ct);

        var groupIds = groupMemberships.Select(gm => gm.GroupId).ToList();

        // Get last attendance dates for these groups
        var lastAttendances = await context.Attendances
            .AsNoTracking()
            .Include(a => a.Occurrence)
            .Where(a => a.PersonAliasId.HasValue
                && a.Occurrence != null
                && a.Occurrence.GroupId.HasValue
                && groupIds.Contains(a.Occurrence.GroupId.Value))
            .GroupBy(a => a.Occurrence!.GroupId!.Value)
            .Select(g => new
            {
                GroupId = g.Key,
                LastAttendance = g.Max(a => a.StartDateTime)
            })
            .ToDictionaryAsync(x => x.GroupId, x => (DateTime?)x.LastAttendance, ct);

        // Get recent attendance count (last 30 days)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var recentAttendanceCount = await context.Attendances
            .AsNoTracking()
            .Include(a => a.PersonAlias)
            .Where(a => a.PersonAliasId.HasValue
                && a.PersonAlias!.PersonId == currentPersonId.Value
                && a.StartDateTime >= thirtyDaysAgo
                && a.DidAttend == true)
            .CountAsync(ct);

        var groupDtos = groupMemberships.Select(gm => new MyInvolvementGroupDto
        {
            IdKey = gm.Group!.IdKey,
            GroupName = gm.Group.Name,
            Description = gm.Group.Description,
            GroupTypeName = gm.Group.GroupType?.Name ?? "Unknown",
            Role = gm.GroupRole?.Name ?? "Member",
            IsLeader = gm.GroupRole?.IsLeader ?? false,
            LastAttendanceDate = lastAttendances.ContainsKey(gm.GroupId)
                ? lastAttendances[gm.GroupId]
                : null,
            JoinedDate = gm.DateTimeAdded,
            Campus = gm.Group.Campus != null ? new CampusSummaryDto
            {
                IdKey = gm.Group.Campus.IdKey,
                Name = gm.Group.Campus.Name,
                ShortCode = gm.Group.Campus.ShortCode
            } : null
        }).OrderBy(g => g.GroupName).ToList();

        var dto = new MyInvolvementDto
        {
            Groups = groupDtos,
            RecentAttendanceCount = recentAttendanceCount,
            TotalGroupsCount = groupDtos.Count
        };

        logger.LogInformation(
            "Retrieved involvement for PersonIdKey={PersonIdKey}: {GroupCount} groups, {AttendanceCount} recent attendances",
            IdKeyHelper.Encode(currentPersonId.Value), dto.TotalGroupsCount, dto.RecentAttendanceCount);

        return Result<MyInvolvementDto>.Success(dto);
    }

    // Private helper methods

    private static MyProfileDto MapToMyProfileDto(Person person)
    {
        return new MyProfileDto
        {
            IdKey = person.IdKey,
            Guid = person.Guid,
            FirstName = person.FirstName,
            NickName = person.NickName,
            MiddleName = person.MiddleName,
            LastName = person.LastName,
            FullName = person.FullName,
            BirthDate = person.BirthDate,
            Age = person.BirthDate.HasValue
                ? DateTime.UtcNow.Year - person.BirthDate.Value.Year
                : null,
            Gender = person.Gender.ToString(),
            Email = person.Email,
            IsEmailActive = person.IsEmailActive,
            EmailPreference = person.EmailPreference.ToString(),
            PhoneNumbers = person.PhoneNumbers
                .OrderBy(pn => pn.NumberTypeValue?.Order ?? 999)
                .Select(pn => new PhoneNumberDto
                {
                    IdKey = pn.IdKey,
                    Number = pn.Number,
                    NumberFormatted = FormatPhoneNumber(pn.Number),
                    Extension = pn.Extension,
                    PhoneType = pn.NumberTypeValue != null ? new DefinedValueDto
                    {
                        IdKey = pn.NumberTypeValue.IdKey,
                        Guid = pn.NumberTypeValue.Guid,
                        Value = pn.NumberTypeValue.Value,
                        Description = pn.NumberTypeValue.Description,
                        IsActive = pn.NumberTypeValue.IsActive,
                        Order = pn.NumberTypeValue.Order
                    } : null,
                    IsMessagingEnabled = pn.IsMessagingEnabled,
                    IsUnlisted = pn.IsUnlisted
                })
                .ToList(),
            PrimaryFamily = person.PrimaryFamily != null ? new FamilySummaryDto
            {
                IdKey = person.PrimaryFamily.IdKey,
                Name = person.PrimaryFamily.Name,
                MemberCount = 0 // Will be populated separately if needed
            } : null,
            PrimaryCampus = person.PrimaryCampus != null ? new CampusSummaryDto
            {
                IdKey = person.PrimaryCampus.IdKey,
                Name = person.PrimaryCampus.Name,
                ShortCode = person.PrimaryCampus.ShortCode
            } : null,
            PhotoUrl = null, // TODO(#116): Implement photo URLs
            CreatedDateTime = person.CreatedDateTime,
            ModifiedDateTime = person.ModifiedDateTime
        };
    }

    private Task UpdatePhoneNumbersAsync(
        Person person,
        IReadOnlyList<UpdatePhoneNumberRequest> phoneNumberRequests,
        CancellationToken ct)
    {
        // Get existing phone numbers
        var existingPhoneNumbers = person.PhoneNumbers.ToList();

        // Remove phone numbers not in the request
        var requestIdKeys = phoneNumberRequests
            .Where(pn => !string.IsNullOrWhiteSpace(pn.IdKey))
            .Select(pn => pn.IdKey!)
            .ToHashSet();

        foreach (var existingPhone in existingPhoneNumbers)
        {
            if (!requestIdKeys.Contains(existingPhone.IdKey))
            {
                context.PhoneNumbers.Remove(existingPhone);
            }
        }

        // Update or add phone numbers
        foreach (var phoneRequest in phoneNumberRequests)
        {
            PhoneNumber? phoneNumber;

            if (!string.IsNullOrWhiteSpace(phoneRequest.IdKey)
                && IdKeyHelper.TryDecode(phoneRequest.IdKey, out int phoneId))
            {
                // Update existing
                phoneNumber = existingPhoneNumbers.FirstOrDefault(pn => pn.Id == phoneId);
                if (phoneNumber == null)
                {
                    continue;
                }
            }
            else
            {
                // Create new
                phoneNumber = new PhoneNumber
                {
                    Number = phoneRequest.Number,
                    PersonId = person.Id,
                    Guid = Guid.NewGuid(),
                    CreatedDateTime = DateTime.UtcNow
                };
                person.PhoneNumbers.Add(phoneNumber);
            }

            phoneNumber.Number = phoneRequest.Number;
            phoneNumber.Extension = phoneRequest.Extension;
            phoneNumber.IsMessagingEnabled = phoneRequest.IsMessagingEnabled;
            phoneNumber.IsUnlisted = phoneRequest.IsUnlisted;
            phoneNumber.ModifiedDateTime = DateTime.UtcNow;

            // Set phone type
            if (!string.IsNullOrWhiteSpace(phoneRequest.PhoneTypeIdKey)
                && IdKeyHelper.TryDecode(phoneRequest.PhoneTypeIdKey, out int phoneTypeId))
            {
                phoneNumber.NumberTypeValueId = phoneTypeId;
            }
        }

        return Task.CompletedTask;
    }

    private static string FormatPhoneNumber(string number)
    {
        // Simple US phone number formatting using range operators
        if (number.Length == 10)
        {
            return $"({number[..3]}) {number[3..6]}-{number[6..]}";
        }

        if (number.Length == 11 && number.StartsWith('1'))
        {
            return $"+1 ({number[1..4]}) {number[4..7]}-{number[7..]}";
        }

        return number;
    }
}
