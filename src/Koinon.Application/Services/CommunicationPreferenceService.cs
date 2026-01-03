using AutoMapper;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for managing communication preferences.
/// </summary>
public class CommunicationPreferenceService(
    IApplicationDbContext context,
    IMapper mapper,
    ILogger<CommunicationPreferenceService> logger) : ICommunicationPreferenceService
{
    public async Task<List<CommunicationPreferenceDto>> GetByPersonAsync(
        string personIdKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(personIdKey, out int personId))
        {
            return [];
        }

        // Verify person exists
        var personExists = await context.People
            .AnyAsync(p => p.Id == personId, ct);

        if (!personExists)
        {
            return [];
        }

        // Load existing preferences
        var existingPreferences = await context.CommunicationPreferences
            .AsNoTracking()
            .Where(cp => cp.PersonId == personId)
            .ToListAsync(ct);

        var result = new List<CommunicationPreferenceDto>();

        // Return preferences for all communication types
        // If no preference exists, default is opted-in (IsOptedOut = false)
        foreach (CommunicationType type in Enum.GetValues<CommunicationType>())
        {
            var preference = existingPreferences.FirstOrDefault(p => p.CommunicationType == type);

            if (preference != null)
            {
                result.Add(mapper.Map<CommunicationPreferenceDto>(preference));
            }
            else
            {
                // Create default preference (opted in)
                result.Add(new CommunicationPreferenceDto
                {
                    IdKey = string.Empty, // No database record exists yet
                    PersonIdKey = personIdKey,
                    CommunicationType = type.ToString(),
                    IsOptedOut = false,
                    OptOutDateTime = null,
                    OptOutReason = null
                });
            }
        }

        return result;
    }

    public async Task<Result<CommunicationPreferenceDto>> UpdateAsync(
        string personIdKey,
        string communicationType,
        UpdateCommunicationPreferenceDto dto,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(personIdKey, out int personId))
        {
            return Result<CommunicationPreferenceDto>.Failure(Error.NotFound("Person", personIdKey));
        }

        // Parse communication type
        if (!Enum.TryParse<CommunicationType>(communicationType, true, out var commType))
        {
            return Result<CommunicationPreferenceDto>.Failure(
                Error.UnprocessableEntity($"Invalid communication type: {communicationType}"));
        }

        // Verify person exists
        var personExists = await context.People
            .AnyAsync(p => p.Id == personId, ct);

        if (!personExists)
        {
            return Result<CommunicationPreferenceDto>.Failure(Error.NotFound("Person", personIdKey));
        }

        // Validate: opt-out reason should be provided when opting out
        if (dto.IsOptedOut && string.IsNullOrWhiteSpace(dto.OptOutReason))
        {
            logger.LogWarning(
                "Opt-out reason not provided for person {PersonIdKey} and type {CommunicationType}",
                personIdKey,
                communicationType);
        }

        // Find or create preference
        var preference = await context.CommunicationPreferences
            .FirstOrDefaultAsync(cp => cp.PersonId == personId && cp.CommunicationType == commType, ct);

        if (preference == null)
        {
            // Create new preference
            preference = new CommunicationPreference
            {
                PersonId = personId,
                CommunicationType = commType,
                IsOptedOut = dto.IsOptedOut,
                OptOutDateTime = dto.IsOptedOut ? DateTime.UtcNow : null,
                OptOutReason = dto.IsOptedOut ? dto.OptOutReason : null,
                CreatedDateTime = DateTime.UtcNow
            };

            context.CommunicationPreferences.Add(preference);

            logger.LogInformation(
                "Created communication preference for person {PersonIdKey}, type {CommunicationType}, opted out: {IsOptedOut}",
                personIdKey,
                communicationType,
                dto.IsOptedOut);
        }
        else
        {
            // Update existing preference
            preference.IsOptedOut = dto.IsOptedOut;

            if (dto.IsOptedOut)
            {
                // Opting out: set timestamp and reason
                preference.OptOutDateTime = DateTime.UtcNow;
                preference.OptOutReason = dto.OptOutReason;
            }
            else
            {
                // Opting back in: clear timestamp and reason
                preference.OptOutDateTime = null;
                preference.OptOutReason = null;
            }

            preference.ModifiedDateTime = DateTime.UtcNow;

            logger.LogInformation(
                "Updated communication preference for person {PersonIdKey}, type {CommunicationType}, opted out: {IsOptedOut}",
                personIdKey,
                communicationType,
                dto.IsOptedOut);
        }

        await context.SaveChangesAsync(ct);

        return Result<CommunicationPreferenceDto>.Success(mapper.Map<CommunicationPreferenceDto>(preference));
    }

    public async Task<Result<List<CommunicationPreferenceDto>>> BulkUpdateAsync(
        string personIdKey,
        BulkUpdatePreferencesDto dto,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(personIdKey, out int personId))
        {
            return Result<List<CommunicationPreferenceDto>>.Failure(Error.NotFound("Person", personIdKey));
        }

        // Verify person exists
        var personExists = await context.People
            .AnyAsync(p => p.Id == personId, ct);

        if (!personExists)
        {
            return Result<List<CommunicationPreferenceDto>>.Failure(Error.NotFound("Person", personIdKey));
        }

        // Validate all communication types
        var updates = new List<(CommunicationType Type, bool IsOptedOut, string? Reason)>();
        foreach (var item in dto.Preferences)
        {
            if (!Enum.TryParse<CommunicationType>(item.CommunicationType, true, out var commType))
            {
                return Result<List<CommunicationPreferenceDto>>.Failure(
                    Error.UnprocessableEntity($"Invalid communication type: {item.CommunicationType}"));
            }

            updates.Add((commType, item.IsOptedOut, item.OptOutReason));
        }

        // Load all existing preferences for this person
        var existingPreferences = await context.CommunicationPreferences
            .Where(cp => cp.PersonId == personId)
            .ToListAsync(ct);

        var updatedPreferences = new List<CommunicationPreference>();

        // Apply updates
        foreach (var (type, isOptedOut, reason) in updates)
        {
            var preference = existingPreferences.FirstOrDefault(p => p.CommunicationType == type);

            if (preference == null)
            {
                // Create new preference
                preference = new CommunicationPreference
                {
                    PersonId = personId,
                    CommunicationType = type,
                    IsOptedOut = isOptedOut,
                    OptOutDateTime = isOptedOut ? DateTime.UtcNow : null,
                    OptOutReason = isOptedOut ? reason : null,
                    CreatedDateTime = DateTime.UtcNow
                };

                context.CommunicationPreferences.Add(preference);
            }
            else
            {
                // Update existing preference
                preference.IsOptedOut = isOptedOut;

                if (isOptedOut)
                {
                    preference.OptOutDateTime = DateTime.UtcNow;
                    preference.OptOutReason = reason;
                }
                else
                {
                    preference.OptOutDateTime = null;
                    preference.OptOutReason = null;
                }

                preference.ModifiedDateTime = DateTime.UtcNow;
            }

            updatedPreferences.Add(preference);
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Bulk updated {Count} communication preferences for person {PersonIdKey}",
            updatedPreferences.Count,
            personIdKey);

        return Result<List<CommunicationPreferenceDto>>.Success(
            updatedPreferences.Select(p => mapper.Map<CommunicationPreferenceDto>(p)).ToList());
    }

    public async Task<bool> IsOptedOutAsync(int personId, CommunicationType type, CancellationToken ct = default)
    {
        var preference = await context.CommunicationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(cp => cp.PersonId == personId && cp.CommunicationType == type, ct);

        // Default is opted-in (no record = opted in)
        return preference?.IsOptedOut ?? false;
    }

    public async Task<Dictionary<int, bool>> IsOptedOutBatchAsync(
        List<int> personIds,
        CommunicationType type,
        CancellationToken ct = default)
    {
        if (personIds.Count == 0)
        {
            return new Dictionary<int, bool>();
        }

        var optedOutPersonIds = await context.CommunicationPreferences
            .AsNoTracking()
            .Where(cp => personIds.Contains(cp.PersonId) && cp.CommunicationType == type && cp.IsOptedOut)
            .Select(cp => cp.PersonId)
            .ToListAsync(ct);

        return personIds.ToDictionary(id => id, id => optedOutPersonIds.Contains(id));
    }
}
