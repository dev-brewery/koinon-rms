using FluentValidation;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Koinon.Application.Services.Common;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for first-time family self-registration at the kiosk.
/// Creates a Family, a parent Person with a phone number, and zero or more child Persons
/// inside a single database transaction, then returns a check-in-ready search result so
/// the kiosk can proceed directly to member selection.
/// </summary>
public class CheckinRegistrationService(
    IApplicationDbContext context,
    IValidator<KioskFamilyRegistrationRequest> validator,
    ILogger<CheckinRegistrationService> logger)
    : ICheckinRegistrationService
{
    public async Task<CheckinFamilySearchResultDto> RegisterFamilyAsync(
        KioskFamilyRegistrationRequest request,
        CancellationToken ct = default)
    {
        // Validate input
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        // Resolve Adult and Child family roles by well-known GUID
        var adultRole = await context.GroupTypeRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Guid == SystemGuid.GroupTypeRole.FamilyAdult, ct)
            ?? throw new InvalidOperationException(
                "Family Adult role not found. Ensure the database seed data has been applied.");

        var childRole = await context.GroupTypeRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Guid == SystemGuid.GroupTypeRole.FamilyChild, ct)
            ?? throw new InvalidOperationException(
                "Family Child role not found. Ensure the database seed data has been applied.");

        // Normalize the phone number once — strip all non-digit characters
        var normalizedPhone = new string(request.PhoneNumber.Where(char.IsDigit).ToArray());

        var now = DateTime.UtcNow;

        // All writes happen inside a single transaction
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        try
        {
            // 1. Create the Family
            var family = new Family
            {
                Name = $"{request.ParentLastName} Family",
                IsActive = true,
                CreatedDateTime = now
            };

            await context.Families.AddAsync(family, ct);

            // 2. Create the parent Person
            var parent = new Person
            {
                FirstName = request.ParentFirstName,
                LastName = request.ParentLastName,
                CreatedDateTime = now
            };

            // Attach the mobile phone number
            parent.PhoneNumbers.Add(new PhoneNumber
            {
                Number = normalizedPhone,
                NumberNormalized = normalizedPhone,
                IsMessagingEnabled = true,
                CreatedDateTime = now
            });

            await context.People.AddAsync(parent, ct);

            // 3. Link parent to family as Adult (IsPrimary = true)
            //    Navigation properties are used so EF Core resolves FK order automatically.
            var parentMember = new FamilyMember
            {
                Family = family,
                Person = parent,
                FamilyRoleId = adultRole.Id,
                IsPrimary = true,
                DateAdded = now,
                CreatedDateTime = now
            };

            await context.FamilyMembers.AddAsync(parentMember, ct);

            // 4. Create child Persons and their FamilyMember rows
            var childPeople = new List<Person>();

            foreach (var childRequest in request.Children)
            {
                var childLastName = string.IsNullOrWhiteSpace(childRequest.LastName)
                    ? request.ParentLastName
                    : childRequest.LastName;

                var child = new Person
                {
                    FirstName = childRequest.FirstName,
                    LastName = childLastName,
                    CreatedDateTime = now
                };

                // Map DateOnly BirthDate to the three separate birth component columns
                if (childRequest.BirthDate.HasValue)
                {
                    child.BirthYear = childRequest.BirthDate.Value.Year;
                    child.BirthMonth = childRequest.BirthDate.Value.Month;
                    child.BirthDay = childRequest.BirthDate.Value.Day;
                }

                await context.People.AddAsync(child, ct);

                var childMember = new FamilyMember
                {
                    Family = family,
                    Person = child,
                    FamilyRoleId = childRole.Id,
                    IsPrimary = true,
                    DateAdded = now,
                    CreatedDateTime = now
                };

                await context.FamilyMembers.AddAsync(childMember, ct);

                childPeople.Add(child);
            }

            // Single SaveChangesAsync — EF Core inserts in dependency order:
            // Family → People → PhoneNumbers → FamilyMembers
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            logger.LogInformation(
                "Kiosk family registration complete: FamilyId={FamilyId}, FamilyName={FamilyName}, " +
                "ChildCount={ChildCount}",
                family.Id, family.Name, request.Children.Count);

            // 5. Build the response DTO from in-memory objects.
            //    All entities now have their generated IDs — no additional DB round-trip needed.
            var members = new List<CheckinFamilyMemberDto>(1 + childPeople.Count)
            {
                BuildMemberDto(parent, adultRole.Name, isChild: false)
            };

            foreach (var child in childPeople)
            {
                members.Add(BuildMemberDto(child, childRole.Name, isChild: true));
            }

            return new CheckinFamilySearchResultDto
            {
                FamilyIdKey = family.IdKey,
                FamilyName = family.Name,
                AddressSummary = null,
                CampusName = null,
                Members = members,
                RecentCheckInCount = 0
            };
        }
        catch (Exception ex) when (ex is not ValidationException && ex is not OperationCanceledException)
        {
            logger.LogError(ex,
                "Family registration failed for parent {FirstName} {LastName}. Rolling back.",
                request.ParentFirstName, request.ParentLastName);

            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static CheckinFamilyMemberDto BuildMemberDto(Person person, string roleName, bool isChild) =>
        new()
        {
            PersonIdKey = person.IdKey,
            FullName = person.FullName,
            FirstName = person.FirstName,
            LastName = person.LastName,
            NickName = person.NickName,
            Age = CalculateAge(person),
            Gender = person.Gender.ToString(),
            PhotoUrl = null,
            RoleName = roleName,
            IsChild = isChild,
            HasRecentCheckIn = false,
            LastCheckIn = null,
            Grade = null,
            Allergies = person.Allergies,
            HasCriticalAllergies = person.HasCriticalAllergies,
            SpecialNeeds = person.SpecialNeeds
        };

    private static int? CalculateAge(Person person)
    {
        if (person.BirthYear is null || person.BirthMonth is null || person.BirthDay is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var birthDate = new DateOnly(person.BirthYear.Value, person.BirthMonth.Value, person.BirthDay.Value);
        var age = today.Year - birthDate.Year;

        if (today < birthDate.AddYears(age))
        {
            age--;
        }

        return age;
    }
}
