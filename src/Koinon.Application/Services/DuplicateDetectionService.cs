using Koinon.Application.Common;
using Koinon.Application.DTOs.PersonMerge;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for detecting potential duplicate person records using name, email, and phone matching.
/// </summary>
public class DuplicateDetectionService(
    IApplicationDbContext context,
    ILogger<DuplicateDetectionService> logger) : IDuplicateDetectionService
{
    private const int DuplicateThreshold = 50;

    public async Task<PagedResult<DuplicateMatchDto>> FindDuplicatesAsync(
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        logger.LogInformation("Finding all potential duplicate persons (page {Page}, pageSize {PageSize})",
            page, pageSize);

        // Get all active persons
        var activePeople = await context.People
            .AsNoTracking()
            .Where(p => p.RecordStatusValueId == null || p.RecordStatusValue!.Value != "Inactive")
            .Include(p => p.PhoneNumbers)
            .Include(p => p.Photo)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync(ct);

        // Get all ignored pairs
        var ignoredPairs = await context.PersonDuplicateIgnores
            .AsNoTracking()
            .Select(i => new { i.PersonId1, i.PersonId2 })
            .ToListAsync(ct);

        var ignoredSet = new HashSet<(int, int)>(
            ignoredPairs.Select(i => (Math.Min(i.PersonId1, i.PersonId2), Math.Max(i.PersonId1, i.PersonId2))));

        // Find duplicates
        var matches = new List<DuplicateMatchDto>();

        for (int i = 0; i < activePeople.Count; i++)
        {
            for (int j = i + 1; j < activePeople.Count; j++)
            {
                var person1 = activePeople[i];
                var person2 = activePeople[j];

                // Skip if pair is ignored
                var pairKey = (Math.Min(person1.Id, person2.Id), Math.Max(person1.Id, person2.Id));
                if (ignoredSet.Contains(pairKey))
                {
                    continue;
                }

                var (score, reasons) = CalculateMatchScore(person1, person2);

                if (score >= DuplicateThreshold)
                {
                    matches.Add(new DuplicateMatchDto
                    {
                        Person1IdKey = person1.IdKey,
                        Person1Name = person1.FullName,
                        Person1Email = person1.Email,
                        Person1Phone = person1.PhoneNumbers.FirstOrDefault()?.Number,
                        Person1PhotoUrl = person1.Photo != null ? $"/api/v1/files/{person1.Photo.IdKey}" : null,
                        Person2IdKey = person2.IdKey,
                        Person2Name = person2.FullName,
                        Person2Email = person2.Email,
                        Person2Phone = person2.PhoneNumbers.FirstOrDefault()?.Number,
                        Person2PhotoUrl = person2.Photo != null ? $"/api/v1/files/{person2.Photo.IdKey}" : null,
                        MatchScore = score,
                        MatchReasons = reasons
                    });
                }
            }
        }

        // Order by match score descending
        matches = matches.OrderByDescending(m => m.MatchScore).ToList();

        // Apply pagination
        var totalCount = matches.Count;
        var pagedMatches = matches
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        logger.LogInformation("Found {TotalCount} potential duplicate pairs", totalCount);

        return new PagedResult<DuplicateMatchDto>(pagedMatches, totalCount, page, pageSize);
    }

    public async Task<List<DuplicateMatchDto>> FindDuplicatesForPersonAsync(
        string personIdKey,
        CancellationToken ct = default)
    {
        logger.LogInformation("Finding duplicates for person {PersonIdKey}", personIdKey);

        if (!IdKeyHelper.TryDecode(personIdKey, out int personId))
        {
            logger.LogWarning("Invalid personIdKey: {PersonIdKey}", personIdKey);
            return new List<DuplicateMatchDto>();
        }

        var targetPerson = await context.People
            .AsNoTracking()
            .Include(p => p.PhoneNumbers)
            .Include(p => p.Photo)
            .FirstOrDefaultAsync(p => p.Id == personId, ct);

        if (targetPerson == null)
        {
            logger.LogWarning("Person not found: {PersonIdKey}", personIdKey);
            return new List<DuplicateMatchDto>();
        }

        // Get all other active persons
        var otherPeople = await context.People
            .AsNoTracking()
            .Where(p => p.Id != personId &&
                        (p.RecordStatusValueId == null || p.RecordStatusValue!.Value != "Inactive"))
            .Include(p => p.PhoneNumbers)
            .Include(p => p.Photo)
            .ToListAsync(ct);

        // Get ignored pairs for this person
        var ignoredPairIds = await context.PersonDuplicateIgnores
            .AsNoTracking()
            .Where(i => i.PersonId1 == personId || i.PersonId2 == personId)
            .Select(i => i.PersonId1 == personId ? i.PersonId2 : i.PersonId1)
            .ToListAsync(ct);

        var ignoredSet = new HashSet<int>(ignoredPairIds);

        // Find duplicates
        var matches = new List<DuplicateMatchDto>();

        foreach (var otherPerson in otherPeople)
        {
            if (ignoredSet.Contains(otherPerson.Id))
            {
                continue;
            }

            var (score, reasons) = CalculateMatchScore(targetPerson, otherPerson);

            if (score >= DuplicateThreshold)
            {
                matches.Add(new DuplicateMatchDto
                {
                    Person1IdKey = targetPerson.IdKey,
                    Person1Name = targetPerson.FullName,
                    Person1Email = targetPerson.Email,
                    Person1Phone = targetPerson.PhoneNumbers.FirstOrDefault()?.Number,
                    Person1PhotoUrl = targetPerson.Photo != null ? $"/api/v1/files/{targetPerson.Photo.IdKey}" : null,
                    Person2IdKey = otherPerson.IdKey,
                    Person2Name = otherPerson.FullName,
                    Person2Email = otherPerson.Email,
                    Person2Phone = otherPerson.PhoneNumbers.FirstOrDefault()?.Number,
                    Person2PhotoUrl = otherPerson.Photo != null ? $"/api/v1/files/{otherPerson.Photo.IdKey}" : null,
                    MatchScore = score,
                    MatchReasons = reasons
                });
            }
        }

        // Order by match score descending
        matches = matches.OrderByDescending(m => m.MatchScore).ToList();

        logger.LogInformation("Found {Count} potential duplicates for person {PersonIdKey}",
            matches.Count, personIdKey);

        return matches;
    }

    /// <summary>
    /// Calculates the match score and reasons for two person records.
    /// </summary>
    private (int Score, List<string> Reasons) CalculateMatchScore(Person person1, Person person2)
    {
        int score = 0;
        var reasons = new List<string>();

        // Email matching (40 points for exact match, 10 for same domain)
        if (!string.IsNullOrWhiteSpace(person1.Email) && !string.IsNullOrWhiteSpace(person2.Email))
        {
            if (string.Equals(person1.Email, person2.Email, StringComparison.OrdinalIgnoreCase))
            {
                score += 40;
                reasons.Add("Same email address");
            }
            else
            {
                var domain1 = person1.Email.Split('@').LastOrDefault();
                var domain2 = person2.Email.Split('@').LastOrDefault();
                if (!string.IsNullOrEmpty(domain1) &&
                    string.Equals(domain1, domain2, StringComparison.OrdinalIgnoreCase))
                {
                    score += 10;
                    reasons.Add("Same email domain");
                }
            }
        }

        // Phone matching (30 points for normalized match)
        var phone1 = person1.PhoneNumbers.FirstOrDefault()?.Number;
        var phone2 = person2.PhoneNumbers.FirstOrDefault()?.Number;
        if (!string.IsNullOrWhiteSpace(phone1) && !string.IsNullOrWhiteSpace(phone2))
        {
            var normalized1 = NormalizePhone(phone1);
            var normalized2 = NormalizePhone(phone2);
            if (normalized1 == normalized2 && normalized1.Length >= 10)
            {
                score += 30;
                reasons.Add("Same phone number");
            }
        }

        // Name matching
        var firstName1 = person1.NickName ?? person1.FirstName;
        var firstName2 = person2.NickName ?? person2.FirstName;

        // Same first name and last name starting with same letter (15 points)
        if (string.Equals(firstName1, firstName2, StringComparison.OrdinalIgnoreCase) &&
            person1.LastName.Length > 0 && person2.LastName.Length > 0 &&
            char.ToUpperInvariant(person1.LastName[0]) == char.ToUpperInvariant(person2.LastName[0]))
        {
            score += 15;
            reasons.Add("Same first name and similar last name");
        }

        // Full name similarity using Levenshtein distance
        var fullName1 = $"{firstName1} {person1.LastName}".ToLowerInvariant();
        var fullName2 = $"{firstName2} {person2.LastName}".ToLowerInvariant();
        var similarity = CalculateNameSimilarity(fullName1, fullName2);
        if (similarity >= 0.8) // 80% similar
        {
            int nameScore = (int)(similarity * 20); // Up to 20 points
            score += nameScore;
            reasons.Add($"Names are {similarity:P0} similar");
        }

        return (score, reasons);
    }

    /// <summary>
    /// Normalizes a phone number by removing all non-digit characters.
    /// </summary>
    private string NormalizePhone(string phone)
    {
        return new string(phone.Where(char.IsDigit).ToArray());
    }

    /// <summary>
    /// Calculates name similarity using Levenshtein distance.
    /// Returns a value between 0 and 1, where 1 is identical.
    /// </summary>
    private double CalculateNameSimilarity(string name1, string name2)
    {
        if (string.IsNullOrEmpty(name1) || string.IsNullOrEmpty(name2))
        {
            return 0;
        }

        int distance = LevenshteinDistance(name1, name2);
        int maxLength = Math.Max(name1.Length, name2.Length);

        return 1.0 - (double)distance / maxLength;
    }

    /// <summary>
    /// Calculates the Levenshtein distance between two strings.
    /// </summary>
    private int LevenshteinDistance(string s1, string s2)
    {
        int[,] d = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
        {
            d[i, 0] = i;
        }

        for (int j = 0; j <= s2.Length; j++)
        {
            d[0, j] = j;
        }

        for (int j = 1; j <= s2.Length; j++)
        {
            for (int i = 1; i <= s1.Length; i++)
            {
                int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[s1.Length, s2.Length];
    }
}
