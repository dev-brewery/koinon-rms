using AutoMapper;
using AutoMapper.QueryableExtensions;
using FluentValidation;
using Koinon.Application.Common;
using Koinon.Application.Constants;
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
/// Service for person management operations.
/// </summary>
public class PersonService(
    IApplicationDbContext context,
    IMapper mapper,
    IValidator<CreatePersonRequest> createValidator,
    IValidator<UpdatePersonRequest> updateValidator,
    IUserContext userContext,
    ILogger<PersonService> logger) : IPersonService
{
    public async Task<PersonDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var person = await context.People
            .AsNoTracking()
            .Include(p => p.PhoneNumbers)
            .Include(p => p.RecordStatusValue)
            .Include(p => p.ConnectionStatusValue)
            .Include(p => p.TitleValue)
            .Include(p => p.SuffixValue)
            .Include(p => p.MaritalStatusValue)
            .Include(p => p.PrimaryCampus)
            .Include(p => p.Photo)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (person is null)
        {
            return null;
        }

        var dto = mapper.Map<PersonDto>(person);

        // Query primary family via FamilyMember junction
        var primaryFamilyMember = await context.FamilyMembers
            .AsNoTracking()
            .Include(fm => fm.Family)
            .Where(fm => fm.PersonId == id && fm.IsPrimary)
            .FirstOrDefaultAsync(ct);

        if (primaryFamilyMember?.Family != null)
        {
            var memberCount = await context.FamilyMembers
                .CountAsync(fm => fm.FamilyId == primaryFamilyMember.FamilyId, ct);

            dto = dto with
            {
                PrimaryFamily = new FamilySummaryDto
                {
                    IdKey = primaryFamilyMember.Family.IdKey,
                    Name = primaryFamilyMember.Family.Name,
                    MemberCount = memberCount
                }
            };
        }

        return dto;
    }

    public async Task<PersonDto?> GetByIdKeyAsync(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return null;
        }

        return await GetByIdAsync(id, ct);
    }

    public async Task<PagedResult<PersonSummaryDto>> SearchAsync(
        PersonSearchParameters parameters,
        CancellationToken ct = default)
    {
        IQueryable<Person> query = context.People
            .AsNoTracking()
            .Include(p => p.Photo)
            .Include(p => p.PrimaryCampus);

        // Apply full-text search if query provided
        if (!string.IsNullOrWhiteSpace(parameters.Query))
        {
            // Case-insensitive search using LIKE
            var searchTerm = $"%{parameters.Query}%";
            query = query.Where(p =>
                EF.Functions.Like(p.FirstName, searchTerm) ||
                EF.Functions.Like(p.LastName, searchTerm) ||
                (p.NickName != null && EF.Functions.Like(p.NickName, searchTerm)) ||
                (p.Email != null && EF.Functions.Like(p.Email, searchTerm))
            );
        }

        // Filter by campus
        if (!string.IsNullOrWhiteSpace(parameters.CampusId))
        {
            if (IdKeyHelper.TryDecode(parameters.CampusId, out int campusId))
            {
                query = query.Where(p => p.PrimaryCampusId == campusId);
            }
        }

        // Filter by record status
        if (!string.IsNullOrWhiteSpace(parameters.RecordStatusId))
        {
            if (IdKeyHelper.TryDecode(parameters.RecordStatusId, out int recordStatusId))
            {
                query = query.Where(p => p.RecordStatusValueId == recordStatusId);
            }
        }

        // Filter by connection status
        if (!string.IsNullOrWhiteSpace(parameters.ConnectionStatusId))
        {
            if (IdKeyHelper.TryDecode(parameters.ConnectionStatusId, out int connectionStatusId))
            {
                query = query.Where(p => p.ConnectionStatusValueId == connectionStatusId);
            }
        }

        // Exclude inactive by default
        if (!parameters.IncludeInactive)
        {
            // Assuming Active status has a well-known GUID (would need to fetch from DefinedValues)
            // For now, just exclude IsDeceased
            query = query.Where(p => !p.IsDeceased);
        }

        // Get total count
        var totalCount = await query.CountAsync(ct);

        // Apply pagination and projection
        var items = await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ProjectTo<PersonSummaryDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        logger.LogInformation(
            "Person search completed: Query={Query}, Results={Count}, Page={Page}",
            parameters.Query, totalCount, parameters.Page);

        return new PagedResult<PersonSummaryDto>(
            items, totalCount, parameters.Page, parameters.PageSize);
    }

    public async Task<Result<PersonDto>> CreateAsync(
        CreatePersonRequest request,
        CancellationToken ct = default)
    {
        // Validate
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<PersonDto>.Failure(Error.FromFluentValidation(validation));
        }

        // Map to entity
        var person = mapper.Map<Person>(request);
        person.CreatedDateTime = DateTime.UtcNow;

        // Decode and set status IDs if provided
        if (!string.IsNullOrWhiteSpace(request.ConnectionStatusValueId))
        {
            if (IdKeyHelper.TryDecode(request.ConnectionStatusValueId, out int connectionStatusId))
            {
                person.ConnectionStatusValueId = connectionStatusId;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.RecordStatusValueId))
        {
            if (IdKeyHelper.TryDecode(request.RecordStatusValueId, out int recordStatusId))
            {
                person.RecordStatusValueId = recordStatusId;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.CampusId))
        {
            if (IdKeyHelper.TryDecode(request.CampusId, out int campusId))
            {
                person.PrimaryCampusId = campusId;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.TitleValueId))
        {
            if (IdKeyHelper.TryDecode(request.TitleValueId, out int titleValueId))
            {
                person.TitleValueId = titleValueId;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.SuffixValueId))
        {
            if (IdKeyHelper.TryDecode(request.SuffixValueId, out int suffixValueId))
            {
                person.SuffixValueId = suffixValueId;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.MaritalStatusValueId))
        {
            if (IdKeyHelper.TryDecode(request.MaritalStatusValueId, out int maritalStatusValueId))
            {
                person.MaritalStatusValueId = maritalStatusValueId;
            }
        }

        person.AnniversaryDate = request.AnniversaryDate;

        // Add phone numbers
        if (request.PhoneNumbers != null)
        {
            foreach (var phoneRequest in request.PhoneNumbers)
            {
                var phone = mapper.Map<PhoneNumber>(phoneRequest);

                if (!string.IsNullOrWhiteSpace(phoneRequest.PhoneTypeValueId))
                {
                    if (IdKeyHelper.TryDecode(phoneRequest.PhoneTypeValueId, out int phoneTypeId))
                    {
                        phone.NumberTypeValueId = phoneTypeId;
                    }
                }

                person.PhoneNumbers.Add(phone);
            }
        }

        // Add to database
        await context.People.AddAsync(person, ct);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Created person {PersonId}: {Name}", person.Id, person.FullName);

        // Fetch full person with includes
        var createdPerson = await GetByIdAsync(person.Id, ct);
        return createdPerson != null
            ? Result<PersonDto>.Success(createdPerson)
            : Result<PersonDto>.Failure(Error.UnprocessableEntity("Failed to retrieve created person"));
    }

    public async Task<Result<PersonDto>> UpdateAsync(
        string idKey,
        UpdatePersonRequest request,
        CancellationToken ct = default)
    {
        // Validate
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result<PersonDto>.Failure(Error.FromFluentValidation(validation));
        }

        // Get person
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result<PersonDto>.Failure(Error.NotFound("Person", idKey));
        }

        var person = await context.People
            .Include(p => p.PhoneNumbers)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (person is null)
        {
            return Result<PersonDto>.Failure(Error.NotFound("Person", idKey));
        }

        // Update fields
        if (request.FirstName != null)
        {
            person.FirstName = request.FirstName;
        }

        if (request.NickName != null)
        {
            person.NickName = request.NickName;
        }

        if (request.MiddleName != null)
        {
            person.MiddleName = request.MiddleName;
        }

        if (request.LastName != null)
        {
            person.LastName = request.LastName;
        }

        if (request.Email != null)
        {
            person.Email = request.Email;
        }

        if (request.IsEmailActive.HasValue)
        {
            person.IsEmailActive = request.IsEmailActive.Value;
        }

        if (request.EmailPreference != null)
        {
            person.EmailPreference = request.EmailPreference switch
            {
                EmailPreferenceValues.EmailAllowed => EmailPreference.EmailAllowed,
                EmailPreferenceValues.NoMassEmails => EmailPreference.NoMassEmails,
                EmailPreferenceValues.DoNotEmail => EmailPreference.DoNotEmail,
                _ => EmailPreference.EmailAllowed
            };
        }

        if (request.Gender != null)
        {
            person.Gender = request.Gender switch
            {
                GenderValues.Male => Gender.Male,
                GenderValues.Female => Gender.Female,
                _ => Gender.Unknown
            };
        }

        if (request.BirthDate.HasValue)
        {
            person.BirthYear = request.BirthDate.Value.Year;
            person.BirthMonth = request.BirthDate.Value.Month;
            person.BirthDay = request.BirthDate.Value.Day;
        }

        if (!string.IsNullOrWhiteSpace(request.ConnectionStatusValueId))
        {
            if (IdKeyHelper.TryDecode(request.ConnectionStatusValueId, out int connectionStatusId))
            {
                person.ConnectionStatusValueId = connectionStatusId;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.RecordStatusValueId))
        {
            if (IdKeyHelper.TryDecode(request.RecordStatusValueId, out int recordStatusId))
            {
                person.RecordStatusValueId = recordStatusId;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.PrimaryCampusId))
        {
            if (IdKeyHelper.TryDecode(request.PrimaryCampusId, out int campusId))
            {
                person.PrimaryCampusId = campusId;
            }
        }

        if (request.TitleValueId != null)
        {
            person.TitleValueId = !string.IsNullOrEmpty(request.TitleValueId)
                ? IdKeyHelper.Decode(request.TitleValueId)
                : null;
        }

        if (request.SuffixValueId != null)
        {
            person.SuffixValueId = !string.IsNullOrEmpty(request.SuffixValueId)
                ? IdKeyHelper.Decode(request.SuffixValueId)
                : null;
        }

        if (request.MaritalStatusValueId != null)
        {
            person.MaritalStatusValueId = !string.IsNullOrEmpty(request.MaritalStatusValueId)
                ? IdKeyHelper.Decode(request.MaritalStatusValueId)
                : null;
        }

        if (request.AnniversaryDate.HasValue)
        {
            person.AnniversaryDate = request.AnniversaryDate.Value;
        }

        person.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Updated person {PersonId}: {Name}", person.Id, person.FullName);

        // Return updated person
        var updatedPerson = await GetByIdAsync(person.Id, ct);
        return updatedPerson != null
            ? Result<PersonDto>.Success(updatedPerson)
            : Result<PersonDto>.Failure(Error.UnprocessableEntity("Failed to retrieve updated person"));
    }

    public async Task<Result> DeleteAsync(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result.Failure(Error.NotFound("Person", idKey));
        }

        var person = await context.People.FindAsync(new object[] { id }, ct);

        if (person is null)
        {
            return Result.Failure(Error.NotFound("Person", idKey));
        }

        // Soft delete by setting IsDeceased (or would set RecordStatus to Inactive with proper DefinedValue lookup)
        person.IsDeceased = true;
        person.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Deleted (soft) person {PersonId}: {Name}", person.Id, person.FullName);

        return Result.Success();
    }

    public async Task<Result<FamilySummaryDto?>> GetFamilyAsync(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result<FamilySummaryDto?>.Failure(Error.NotFound("Person", idKey));
        }

        var personExists = await context.People.AnyAsync(p => p.Id == id, ct);
        if (!personExists)
        {
            return Result<FamilySummaryDto?>.Failure(Error.NotFound("Person", idKey));
        }

        // Query primary family via FamilyMember junction
        var primaryFamilyMember = await context.FamilyMembers
            .AsNoTracking()
            .Include(fm => fm.Family)
            .Where(fm => fm.PersonId == id && fm.IsPrimary)
            .FirstOrDefaultAsync(ct);

        if (primaryFamilyMember?.Family == null)
        {
            return Result<FamilySummaryDto?>.Success(null);
        }

        var memberCount = await context.FamilyMembers
            .CountAsync(fm => fm.FamilyId == primaryFamilyMember.FamilyId, ct);

        var familyDto = new FamilySummaryDto
        {
            IdKey = primaryFamilyMember.Family.IdKey,
            Name = primaryFamilyMember.Family.Name,
            MemberCount = memberCount
        };

        return Result<FamilySummaryDto?>.Success(familyDto);
    }

    public async Task<Result<PersonDto>> UpdatePhotoAsync(string idKey, string? photoIdKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result<PersonDto>.Failure(Error.NotFound("Person", idKey));
        }

        var person = await context.People
            .Include(p => p.Photo)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (person is null)
        {
            return Result<PersonDto>.Failure(Error.NotFound("Person", idKey));
        }

        // CRITICAL: Authorization check - user must be the person or have admin/staff permissions
        if (!userContext.IsAuthenticated)
        {
            logger.LogWarning("Unauthorized photo update attempt - no authenticated user");
            return Result<PersonDto>.Failure(Error.Forbidden("Authentication required to update photo"));
        }

        // Check if user can access this person (handles own photo + Admin/Staff roles)
        if (!userContext.CanAccessPerson(person.Id))
        {
            logger.LogWarning(
                "Unauthorized photo update attempt: User {UserId} attempted to update photo for Person {PersonId}",
                userContext.CurrentPersonId, person.Id);
            return Result<PersonDto>.Failure(Error.Forbidden("You do not have permission to update this person's photo"));
        }

        // Decode photo ID if provided
        int? photoId = null;
        if (!string.IsNullOrWhiteSpace(photoIdKey))
        {
            if (!IdKeyHelper.TryDecode(photoIdKey, out int decodedPhotoId))
            {
                return Result<PersonDto>.Failure(new Error("VALIDATION_ERROR", "Invalid photo IdKey"));
            }

            // Verify the photo exists
            var photoExists = await context.BinaryFiles.AnyAsync(bf => bf.Id == decodedPhotoId, ct);
            if (!photoExists)
            {
                return Result<PersonDto>.Failure(Error.NotFound("Photo", photoIdKey));
            }

            photoId = decodedPhotoId;
        }

        // Update photo
        person.PhotoId = photoId;
        person.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Updated photo for person {PersonId}: PhotoId={PhotoId}", person.Id, photoId);

        // Return updated person
        var updatedPerson = await GetByIdAsync(person.Id, ct);
        return updatedPerson != null
            ? Result<PersonDto>.Success(updatedPerson)
            : Result<PersonDto>.Failure(Error.UnprocessableEntity("Failed to retrieve updated person"));
    }

    public async Task<Result<PagedResult<NoteDto>>> GetNotesAsync(
        string personIdKey,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(personIdKey, out int personId))
        {
            return Result<PagedResult<NoteDto>>.Failure(Error.NotFound("Person", personIdKey));
        }

        var personExists = await context.People.AnyAsync(p => p.Id == personId, ct);
        if (!personExists)
        {
            return Result<PagedResult<NoteDto>>.Failure(Error.NotFound("Person", personIdKey));
        }

        // Collect all alias IDs for this person so notes on any alias are returned
        var aliasIds = await context.PersonAliases
            .AsNoTracking()
            .Where(pa => pa.PersonId == personId)
            .Select(pa => pa.Id)
            .ToListAsync(ct);

        var query = context.Notes
            .AsNoTracking()
            .Include(n => n.NoteTypeValue)
            .Include(n => n.AuthorPersonAlias)
                .ThenInclude(pa => pa!.Person)
            .Where(n => aliasIds.Contains(n.PersonAliasId));

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(n => n.NoteDateTime)
            .ThenByDescending(n => n.CreatedDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NoteDto
            {
                IdKey = IdKeyHelper.Encode(n.Id),
                NoteTypeValueIdKey = IdKeyHelper.Encode(n.NoteTypeValueId),
                NoteTypeName = n.NoteTypeValue != null ? n.NoteTypeValue.Value : string.Empty,
                Text = n.Text,
                NoteDateTime = n.NoteDateTime,
                AuthorPersonIdKey = n.AuthorPersonAlias != null && n.AuthorPersonAlias.Person != null
                    ? IdKeyHelper.Encode(n.AuthorPersonAlias.Person.Id)
                    : null,
                AuthorPersonName = n.AuthorPersonAlias != null && n.AuthorPersonAlias.Person != null
                    ? n.AuthorPersonAlias.Person.FullName
                    : null,
                IsPrivate = n.IsPrivate,
                IsAlert = n.IsAlert,
                CreatedDateTime = n.CreatedDateTime,
                ModifiedDateTime = n.ModifiedDateTime
            })
            .ToListAsync(ct);

        logger.LogInformation(
            "Retrieved notes for person {PersonId}: Count={Count}, Page={Page}",
            personId, totalCount, page);

        return Result<PagedResult<NoteDto>>.Success(
            new PagedResult<NoteDto>(items, totalCount, page, pageSize));
    }

    public async Task<Result<NoteDto>> CreateNoteAsync(
        string personIdKey,
        CreateNoteRequest request,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(personIdKey, out int personId))
        {
            return Result<NoteDto>.Failure(Error.NotFound("Person", personIdKey));
        }

        // Verify person exists and get their primary alias
        var primaryAlias = await context.PersonAliases
            .AsNoTracking()
            .Where(pa => pa.PersonId == personId)
            .OrderBy(pa => pa.Id)
            .FirstOrDefaultAsync(ct);

        if (primaryAlias is null)
        {
            return Result<NoteDto>.Failure(Error.NotFound("Person", personIdKey));
        }

        // Decode note type
        if (!IdKeyHelper.TryDecode(request.NoteTypeValueIdKey, out int noteTypeValueId))
        {
            return Result<NoteDto>.Failure(Error.Validation("Invalid NoteTypeValueIdKey"));
        }

        var noteTypeExists = await context.DefinedValues.AnyAsync(dv => dv.Id == noteTypeValueId, ct);
        if (!noteTypeExists)
        {
            return Result<NoteDto>.Failure(Error.NotFound("DefinedValue", request.NoteTypeValueIdKey));
        }

        // Resolve the current user's PersonAlias for authorship attribution.
        // TODO(#486): Replace with IUserContext.CurrentPersonAliasId once that property is added
        //             to the auth context pipeline. For now, look up the primary alias by PersonId.
        int? authorAliasId = null;
        if (userContext.CurrentPersonId.HasValue)
        {
            authorAliasId = await context.PersonAliases
                .AsNoTracking()
                .Where(pa => pa.PersonId == userContext.CurrentPersonId.Value)
                .OrderBy(pa => pa.Id)
                .Select(pa => (int?)pa.Id)
                .FirstOrDefaultAsync(ct);
        }

        var note = new Note
        {
            PersonAliasId = primaryAlias.Id,
            NoteTypeValueId = noteTypeValueId,
            Text = request.Text,
            NoteDateTime = request.NoteDateTime?.ToUniversalTime() ?? DateTime.UtcNow,
            AuthorPersonAliasId = authorAliasId,
            IsPrivate = request.IsPrivate,
            IsAlert = request.IsAlert,
            CreatedDateTime = DateTime.UtcNow
        };

        await context.Notes.AddAsync(note, ct);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Created note {NoteId} on person {PersonId} by alias {AuthorAliasId}",
            note.Id, personId, authorAliasId);

        // Fetch with includes for the full response DTO
        var created = await context.Notes
            .AsNoTracking()
            .Include(n => n.NoteTypeValue)
            .Include(n => n.AuthorPersonAlias)
                .ThenInclude(pa => pa!.Person)
            .FirstAsync(n => n.Id == note.Id, ct);

        return Result<NoteDto>.Success(mapper.Map<NoteDto>(created));
    }

    public async Task<Result<NoteDto>> UpdateNoteAsync(
        string noteIdKey,
        UpdateNoteRequest request,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(noteIdKey, out int noteId))
        {
            return Result<NoteDto>.Failure(Error.NotFound("Note", noteIdKey));
        }

        var note = await context.Notes.FindAsync(new object[] { noteId }, ct);

        if (note is null)
        {
            return Result<NoteDto>.Failure(Error.NotFound("Note", noteIdKey));
        }

        if (request.NoteTypeValueIdKey is not null)
        {
            if (!IdKeyHelper.TryDecode(request.NoteTypeValueIdKey, out int noteTypeValueId))
            {
                return Result<NoteDto>.Failure(Error.Validation("Invalid NoteTypeValueIdKey"));
            }

            var noteTypeExists = await context.DefinedValues.AnyAsync(dv => dv.Id == noteTypeValueId, ct);
            if (!noteTypeExists)
            {
                return Result<NoteDto>.Failure(Error.NotFound("DefinedValue", request.NoteTypeValueIdKey));
            }

            note.NoteTypeValueId = noteTypeValueId;
        }

        if (request.Text is not null)
        {
            note.Text = request.Text;
        }

        if (request.NoteDateTime.HasValue)
        {
            note.NoteDateTime = request.NoteDateTime.Value.ToUniversalTime();
        }

        if (request.IsPrivate.HasValue)
        {
            note.IsPrivate = request.IsPrivate.Value;
        }

        if (request.IsAlert.HasValue)
        {
            note.IsAlert = request.IsAlert.Value;
        }

        note.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Updated note {NoteId}", noteId);

        var updated = await context.Notes
            .AsNoTracking()
            .Include(n => n.NoteTypeValue)
            .Include(n => n.AuthorPersonAlias)
                .ThenInclude(pa => pa!.Person)
            .FirstAsync(n => n.Id == noteId, ct);

        return Result<NoteDto>.Success(mapper.Map<NoteDto>(updated));
    }

    public async Task<Result> DeleteNoteAsync(string noteIdKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(noteIdKey, out int noteId))
        {
            return Result.Failure(Error.NotFound("Note", noteIdKey));
        }

        var note = await context.Notes.FindAsync(new object[] { noteId }, ct);

        if (note is null)
        {
            return Result.Failure(Error.NotFound("Note", noteIdKey));
        }

        context.Notes.Remove(note);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Deleted note {NoteId}", noteId);

        return Result.Success();
    }

    public async Task<PagedResult<PersonGroupMembershipDto>> GetGroupsAsync(
        string idKey,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return new PagedResult<PersonGroupMembershipDto>(
                new List<PersonGroupMembershipDto>(), 0, page, pageSize);
        }

        // Query group memberships excluding family groups
        var query = context.GroupMembers
            .AsNoTracking()
            .Include(gm => gm.Group)
                .ThenInclude(g => g!.GroupType)
            .Include(gm => gm.GroupRole)
            .Where(gm => gm.PersonId == id && gm.Group != null && gm.Group.GroupType != null);

        // Get total count
        var totalCount = await query.CountAsync(ct);

        // Apply pagination and map to DTO
        var items = await query
            .OrderBy(gm => gm.Group!.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(gm => new PersonGroupMembershipDto
            {
                IdKey = IdKeyHelper.Encode(gm.Id),
                Guid = gm.Guid,
                GroupIdKey = IdKeyHelper.Encode(gm.GroupId),
                GroupName = gm.Group!.Name,
                GroupTypeIdKey = IdKeyHelper.Encode(gm.Group.GroupTypeId),
                GroupTypeName = gm.Group.GroupType!.Name,
                RoleIdKey = IdKeyHelper.Encode(gm.GroupRoleId),
                RoleName = gm.GroupRole != null ? gm.GroupRole.Name : "(Unknown)",
                MemberStatus = gm.GroupMemberStatus.ToString(),
                CreatedDateTime = gm.CreatedDateTime,
                ModifiedDateTime = gm.ModifiedDateTime
            })
            .ToListAsync(ct);

        logger.LogInformation(
            "Retrieved groups for person {PersonId}: Count={Count}, Page={Page}",
            id, totalCount, page);

        return new PagedResult<PersonGroupMembershipDto>(
            items, totalCount, page, pageSize);
    }

}
