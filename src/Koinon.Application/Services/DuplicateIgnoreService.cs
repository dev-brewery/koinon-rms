using Koinon.Application.Common;
using Koinon.Application.DTOs.PersonMerge;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for managing ignored duplicate person pairs.
/// </summary>
public class DuplicateIgnoreService(
    IApplicationDbContext context,
    ILogger<DuplicateIgnoreService> logger) : IDuplicateIgnoreService
{
    public async Task<Result> IgnoreDuplicateAsync(
        IgnoreDuplicateRequestDto request,
        int currentUserId,
        CancellationToken ct = default)
    {
        logger.LogInformation("Ignoring duplicate pair: {Person1IdKey} and {Person2IdKey}",
            request.Person1IdKey, request.Person2IdKey);

        // Decode IdKeys
        if (!IdKeyHelper.TryDecode(request.Person1IdKey, out int person1Id) ||
            !IdKeyHelper.TryDecode(request.Person2IdKey, out int person2Id))
        {
            return Result.Failure(Error.Validation("Invalid IdKey format"));
        }

        // Verify both persons exist
        var person1Exists = await context.People.AnyAsync(p => p.Id == person1Id, ct);
        var person2Exists = await context.People.AnyAsync(p => p.Id == person2Id, ct);

        if (!person1Exists)
        {
            return Result.Failure(Error.NotFound("Person", request.Person1IdKey));
        }

        if (!person2Exists)
        {
            return Result.Failure(Error.NotFound("Person", request.Person2IdKey));
        }

        // Store with smaller ID first for consistency
        int smallerId = Math.Min(person1Id, person2Id);
        int largerId = Math.Max(person1Id, person2Id);

        // Check if pair already exists
        var existingIgnore = await context.PersonDuplicateIgnores
            .FirstOrDefaultAsync(i => i.PersonId1 == smallerId && i.PersonId2 == largerId, ct);

        if (existingIgnore != null)
        {
            logger.LogInformation("Duplicate pair already ignored");
            return Result.Failure(Error.Conflict("This duplicate pair is already marked as ignored"));
        }

        // Create new ignore record
        var ignore = new PersonDuplicateIgnore
        {
            PersonId1 = smallerId,
            PersonId2 = largerId,
            MarkedByPersonId = currentUserId,
            MarkedDateTime = DateTime.UtcNow,
            Reason = request.Reason
        };

        context.PersonDuplicateIgnores.Add(ignore);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Successfully marked duplicate pair as ignored");
        return Result.Success();
    }

    public async Task<Result> UnignoreDuplicateAsync(
        string person1IdKey,
        string person2IdKey,
        CancellationToken ct = default)
    {
        logger.LogInformation("Removing ignore flag for duplicate pair: {Person1IdKey} and {Person2IdKey}",
            person1IdKey, person2IdKey);

        // Decode IdKeys
        if (!IdKeyHelper.TryDecode(person1IdKey, out int person1Id) ||
            !IdKeyHelper.TryDecode(person2IdKey, out int person2Id))
        {
            return Result.Failure(Error.Validation("Invalid IdKey format"));
        }

        // Determine order
        int smallerId = Math.Min(person1Id, person2Id);
        int largerId = Math.Max(person1Id, person2Id);

        // Find existing ignore record
        var ignore = await context.PersonDuplicateIgnores
            .FirstOrDefaultAsync(i => i.PersonId1 == smallerId && i.PersonId2 == largerId, ct);

        if (ignore == null)
        {
            logger.LogWarning("No ignore record found for this pair");
            return Result.Failure(Error.NotFound("PersonDuplicateIgnore", $"{person1IdKey},{person2IdKey}"));
        }

        // Remove the ignore record
        context.PersonDuplicateIgnores.Remove(ignore);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Successfully removed ignore flag");
        return Result.Success();
    }
}
