using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services;

/// <summary>
/// Service for performing global searches across People, Families, and Groups.
/// </summary>
public class GlobalSearchService(
    IApplicationDbContext context,
    ILogger<GlobalSearchService> logger) : IGlobalSearchService
{
    private const int MaxPageSize = 100;
    private const string CategoryPeople = "People";
    private const string CategoryFamilies = "Families";
    private const string CategoryGroups = "Groups";

    public async Task<GlobalSearchResponse> SearchAsync(
        string query,
        string? category = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Handle empty query
        if (string.IsNullOrWhiteSpace(query))
        {
            logger.LogDebug("Empty search query received, returning empty results");
            return new GlobalSearchResponse(
                Results: Array.Empty<GlobalSearchResultDto>(),
                TotalCount: 0,
                PageNumber: pageNumber,
                PageSize: pageSize,
                CategoryCounts: new Dictionary<string, int>());
        }

        // Enforce max page size
        if (pageSize > MaxPageSize)
        {
            pageSize = MaxPageSize;
        }

        // Normalize search term
        var searchTerm = query.Trim();
        var normalizedPhone = NormalizePhoneNumber(searchTerm);

        logger.LogInformation("Global search for query: {Query}, category: {Category}, page: {Page}, pageSize: {PageSize}",
            searchTerm, category ?? "all", pageNumber, pageSize);

        // Get category counts (always return total counts regardless of pagination)
        var categoryCounts = new Dictionary<string, int>();
        
        var shouldSearchPeople = string.IsNullOrEmpty(category) || category.Equals(CategoryPeople, StringComparison.OrdinalIgnoreCase);
        var shouldSearchFamilies = string.IsNullOrEmpty(category) || category.Equals(CategoryFamilies, StringComparison.OrdinalIgnoreCase);
        var shouldSearchGroups = string.IsNullOrEmpty(category) || category.Equals(CategoryGroups, StringComparison.OrdinalIgnoreCase);

        // Count results for each category
        var peopleCount = shouldSearchPeople 
            ? await CountPeopleAsync(searchTerm, normalizedPhone, cancellationToken)
            : 0;
        
        var familiesCount = shouldSearchFamilies 
            ? await CountFamiliesAsync(searchTerm, cancellationToken)
            : 0;
        
        var groupsCount = shouldSearchGroups 
            ? await CountGroupsAsync(searchTerm, cancellationToken)
            : 0;

        if (peopleCount > 0) categoryCounts[CategoryPeople] = peopleCount;
        if (familiesCount > 0) categoryCounts[CategoryFamilies] = familiesCount;
        if (groupsCount > 0) categoryCounts[CategoryGroups] = groupsCount;

        var totalCount = peopleCount + familiesCount + groupsCount;

        // Get paginated results
        var results = new List<GlobalSearchResultDto>();

        if (totalCount > 0)
        {
            var skip = (pageNumber - 1) * pageSize;
            var take = pageSize;

            // Fetch results from each category WITHOUT pagination
            // We limit each to skip+take to avoid fetching entire DB
            var maxResults = skip + take;
            var peopleResults = shouldSearchPeople
                ? await SearchPeopleAsync(searchTerm, normalizedPhone, maxResults, cancellationToken)
                : Array.Empty<GlobalSearchResultDto>();

            var familiesResults = shouldSearchFamilies
                ? await SearchFamiliesAsync(searchTerm, maxResults, cancellationToken)
                : Array.Empty<GlobalSearchResultDto>();

            var groupsResults = shouldSearchGroups
                ? await SearchGroupsAsync(searchTerm, maxResults, cancellationToken)
                : Array.Empty<GlobalSearchResultDto>();

            // Combine all results and order (exact matches first, then partial matches)
            var combined = new List<GlobalSearchResultDto>();
            combined.AddRange(OrderResults(peopleResults, searchTerm));
            combined.AddRange(OrderResults(familiesResults, searchTerm));
            combined.AddRange(OrderResults(groupsResults, searchTerm));

            // Apply pagination ONCE to the combined set
            results = combined.Skip(skip).Take(take).ToList();
        }

        logger.LogInformation("Global search completed: {TotalCount} results ({PeopleCount} people, {FamiliesCount} families, {GroupsCount} groups)",
            totalCount, peopleCount, familiesCount, groupsCount);

        return new GlobalSearchResponse(
            Results: results.AsReadOnly(),
            TotalCount: totalCount,
            PageNumber: pageNumber,
            PageSize: pageSize,
            CategoryCounts: categoryCounts);
    }

    private async Task<int> CountPeopleAsync(string searchTerm, string normalizedPhone, CancellationToken cancellationToken)
    {
        var query = BuildPeopleQuery(searchTerm, normalizedPhone);
        return await query.CountAsync(cancellationToken);
    }

    private async Task<int> CountFamiliesAsync(string searchTerm, CancellationToken cancellationToken)
    {
        var query = BuildFamiliesQuery(searchTerm);
        return await query.CountAsync(cancellationToken);
    }

    private async Task<int> CountGroupsAsync(string searchTerm, CancellationToken cancellationToken)
    {
        var query = BuildGroupsQuery(searchTerm);
        return await query.CountAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<GlobalSearchResultDto>> SearchPeopleAsync(
        string searchTerm,
        string normalizedPhone,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var query = BuildPeopleQuery(searchTerm, normalizedPhone);

        var people = await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Take(maxResults)
            .Select(p => new
            {
                p.Id,
                p.FirstName,
                p.NickName,
                p.LastName,
                p.Email,
                p.PhotoId
            })
            .ToListAsync(cancellationToken);

        return people.Select(p => new GlobalSearchResultDto(
            Category: CategoryPeople,
            IdKey: IdKeyHelper.Encode(p.Id),
            Title: $"{(string.IsNullOrWhiteSpace(p.NickName) ? p.FirstName : p.NickName)} {p.LastName}".Trim(),
            Subtitle: p.Email,
            ImageUrl: p.PhotoId.HasValue ? $"/api/v1/files/{IdKeyHelper.Encode(p.PhotoId.Value)}" : null
        )).ToList();
    }

    private async Task<IReadOnlyList<GlobalSearchResultDto>> SearchFamiliesAsync(
        string searchTerm,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var query = BuildFamiliesQuery(searchTerm);

        var families = await query
            .OrderBy(f => f.Name)
            .Take(maxResults)
            .Select(f => new
            {
                f.Id,
                f.Name,
                CampusName = f.Campus != null ? f.Campus.Name : null
            })
            .ToListAsync(cancellationToken);

        return families.Select(f => new GlobalSearchResultDto(
            Category: CategoryFamilies,
            IdKey: IdKeyHelper.Encode(f.Id),
            Title: f.Name,
            Subtitle: f.CampusName,
            ImageUrl: null
        )).ToList();
    }

    private async Task<IReadOnlyList<GlobalSearchResultDto>> SearchGroupsAsync(
        string searchTerm,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var query = BuildGroupsQuery(searchTerm);

        var groups = await query
            .OrderBy(g => g.Name)
            .Take(maxResults)
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.Description,
                CampusName = g.Campus != null ? g.Campus.Name : null
            })
            .ToListAsync(cancellationToken);

        return groups.Select(g => new GlobalSearchResultDto(
            Category: CategoryGroups,
            IdKey: IdKeyHelper.Encode(g.Id),
            Title: g.Name,
            Subtitle: !string.IsNullOrWhiteSpace(g.Description)
                ? TruncateDescription(g.Description)
                : g.CampusName,
            ImageUrl: null
        )).ToList();
    }

    private IQueryable<Domain.Entities.Person> BuildPeopleQuery(string searchTerm, string normalizedPhone)
    {
        var query = context.People.AsNoTracking();
        var pattern = $"%{searchTerm}%";

        // Search by name (FirstName, LastName, NickName) and email
        query = query.Where(p =>
            EF.Functions.Like(p.FirstName.ToLower(), pattern.ToLower()) ||
            EF.Functions.Like(p.LastName.ToLower(), pattern.ToLower()) ||
            (p.NickName != null && EF.Functions.Like(p.NickName.ToLower(), pattern.ToLower())) ||
            (p.Email != null && EF.Functions.Like(p.Email.ToLower(), pattern.ToLower())));

        // Add phone number search if applicable
        if (!string.IsNullOrEmpty(normalizedPhone))
        {
            query = query.Where(p =>
                p.PhoneNumbers.Any(pn => pn.NumberNormalized.Contains(normalizedPhone)));
        }

        return query;
    }

    private IQueryable<Domain.Entities.Family> BuildFamiliesQuery(string searchTerm)
    {
        var pattern = $"%{searchTerm}%";
        return context.Families
            .AsNoTracking()
            .Include(f => f.Campus)
            .Where(f => EF.Functions.Like(f.Name.ToLower(), pattern.ToLower()));
    }

    private IQueryable<Domain.Entities.Group> BuildGroupsQuery(string searchTerm)
    {
        var pattern = $"%{searchTerm}%";
        return context.Groups
            .AsNoTracking()
            .Include(g => g.Campus)
            .Where(g =>
                EF.Functions.Like(g.Name.ToLower(), pattern.ToLower()) ||
                (g.Description != null && EF.Functions.Like(g.Description.ToLower(), pattern.ToLower())));
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return string.Empty;
        }

        // Strip all non-digit characters
        return new string(phoneNumber.Where(char.IsDigit).ToArray());
    }

    private static IReadOnlyList<GlobalSearchResultDto> OrderResults(
        IReadOnlyList<GlobalSearchResultDto> results,
        string searchTerm)
    {
        // Order exact matches first, then partial matches
        return results
            .OrderBy(r => r.Title.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(r => r.Title)
            .ToList();
    }

    private static string TruncateDescription(string description, int maxLength = 100)
    {
        if (description.Length <= maxLength)
        {
            return description;
        }

        var truncated = description[..maxLength];
        var lastSpace = truncated.LastIndexOf(' ');
        
        if (lastSpace > 0)
        {
            truncated = truncated[..lastSpace];
        }

        return $"{truncated}...";
    }
}
