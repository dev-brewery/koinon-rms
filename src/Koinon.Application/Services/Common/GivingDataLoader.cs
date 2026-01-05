using Koinon.Application.Interfaces;
using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services.Common;

/// <summary>
/// Batch-loads all giving related data to eliminate N+1 query patterns.
/// All methods return pre-loaded dictionaries for O(1) lookups.
///
/// DESIGN RULES:
/// 1. Each method represents a complete data-loading operation
/// 2. No nested calls that make additional database queries
/// 3. Return dictionaries for O(1) lookups (never enumerate query results)
/// 4. Use Include/ThenInclude strategically to batch load relationships
/// 5. SelectMany + GroupBy on collections that have been pre-filtered
///
/// PERFORMANCE GOALS:
/// - LoadContributionsForBatchAsync: 1 query for batch, <100ms for 1000 contributions
/// - LoadPersonAliasesAsync: 1 query for batch of N people, <50ms for 1000 people
/// - LoadFundsByIdsAsync: 1 query for batch of N funds, <30ms for 100 funds
///
/// ANTI-PATTERN (DON'T DO THIS IN SERVICES):
///   foreach (var contribution in contributions) {
///       var details = await context.ContributionDetails               // N queries!
///           .Where(cd => cd.ContributionId == contribution.Id)
///           .ToListAsync();
///       foreach (var detail in details) {
///           var fund = await context.Funds.FindAsync(detail.FundId);  // N*M queries!
///       }
///   }
///
/// CORRECT PATTERN (USE THIS):
///   var data = await dataLoader.LoadContributionsForBatchAsync(batchId, ct);
///   foreach (var contribution in data) {
///       // All ContributionDetails and Funds already loaded
///       var details = contribution.ContributionDetails;  // O(1) lookup, zero queries
///   }
///
/// See: ARCHITECTURAL_REVIEW_PHASE2.2.md#systematic-n-1-elimination
/// </summary>
public class GivingDataLoader(IApplicationDbContext context, ILogger<GivingDataLoader> logger)
{
    /// <summary>
    /// Loads all contributions for a batch with ContributionDetails and Fund data in ONE optimized query.
    /// Returns list of fully-loaded contribution entities.
    ///
    /// WHY THIS MATTERS:
    /// - Old way: Query contributions, then for each contribution query details, then for each detail query fund = N+N*M queries
    /// - New way: Single query with Include chains, all data loaded at once
    ///
    /// QUERY PLAN:
    /// 1. SELECT c.*, cd.*, f.* FROM contribution c
    ///    LEFT JOIN contribution_detail cd ON c.id = cd.contribution_id
    ///    LEFT JOIN fund f ON cd.fund_id = f.id
    ///    WHERE c.batch_id = batchId
    ///
    /// DATABASE INDEXES REQUIRED:
    /// - contribution.batch_id
    /// - contribution_detail.contribution_id
    /// - contribution_detail.fund_id
    /// - fund.id (primary key, already exists)
    ///
    /// USAGE EXAMPLE:
    ///   var contributions = await dataLoader.LoadContributionsForBatchAsync(
    ///       batchId: 123,
    ///       cancellationToken);
    ///
    ///   // Now all data is pre-loaded:
    ///   foreach (var contribution in contributions) {
    ///       foreach (var detail in contribution.ContributionDetails) {
    ///           var fundName = detail.Fund?.Name;  // O(1) lookup, already loaded
    ///       }
    ///   }
    /// </summary>
    public async Task<List<Contribution>> LoadContributionsForBatchAsync(
        int batchId,
        CancellationToken ct = default)
    {
        // SINGLE optimized query with all relationships
        var contributions = await context.Contributions
            .AsNoTracking()
            .Where(c => c.BatchId == batchId)
            .Include(c => c.ContributionDetails)
                .ThenInclude(cd => cd.Fund)
            .ToListAsync(ct);

        logger.LogDebug(
            "Loaded {Count} contributions with details for batch {BatchId}",
            contributions.Count, batchId);

        return contributions;
    }

    /// <summary>
    /// Loads person aliases for given person IDs in ONE query.
    /// Returns dictionary of personId -> PersonAlias? for O(1) lookup.
    ///
    /// WHY THIS MATTERS:
    /// - Old way: For each person ID, query their alias = N queries
    /// - New way: Single query, dictionary for O(1) lookup
    ///
    /// QUERY PLAN:
    /// 1. SELECT pa.* FROM person_alias pa
    ///    WHERE pa.person_id IN (person_ids)
    ///    AND pa.alias_person_id IS NULL
    ///
    /// DATABASE INDEXES REQUIRED:
    /// - person_alias.person_id
    /// - person_alias.alias_person_id
    ///
    /// USAGE EXAMPLE:
    ///   var aliases = await dataLoader.LoadPersonAliasesAsync(
    ///       new[] { 123, 456, 789 },
    ///       cancellationToken);
    ///
    ///   // Now lookup is O(1):
    ///   if (aliases.TryGetValue(personId, out var alias)) {
    ///       var aliasId = alias?.Id;
    ///   }
    /// </summary>
    public async Task<Dictionary<int, PersonAlias?>> LoadPersonAliasesAsync(
        IEnumerable<int> personIds,
        CancellationToken ct = default)
    {
        var ids = personIds.ToList();
        if (ids.Count == 0)
        {
            return new();
        }

        // SINGLE optimized query - get primary aliases only
        var aliases = await context.PersonAliases
            .AsNoTracking()
            .Where(pa => pa.PersonId.HasValue &&
                        ids.Contains(pa.PersonId.Value) &&
                        pa.AliasPersonId == null) // Only primary aliases
            .ToListAsync(ct);

        // Build dictionary for O(1) lookup
        var result = aliases
            .Where(pa => pa.PersonId.HasValue)
            .GroupBy(pa => pa.PersonId!.Value)
            .ToDictionary(
                g => g.Key,
                g => (PersonAlias?)g.First());

        // Ensure all requested IDs have an entry (even if null)
        foreach (var id in ids)
        {
            if (!result.ContainsKey(id))
            {
                result[id] = null;
            }
        }

        // Log any missing data (data quality issue, not performance)
        var missing = ids.Where(id => result[id] == null).ToList();
        if (missing.Count > 0)
        {
            logger.LogWarning(
                "PersonAlias not found for {Count} people: {PersonIds}",
                missing.Count, string.Join(", ", missing.Take(5)));
        }

        return result;
    }

    /// <summary>
    /// Loads funds by IDs in ONE query.
    /// Returns dictionary of fundId -> Fund for O(1) lookup.
    ///
    /// WHY THIS MATTERS:
    /// - Old way: For each fund ID, query the fund = N queries
    /// - New way: Single query, dictionary for O(1) lookup
    ///
    /// QUERY PLAN:
    /// 1. SELECT f.* FROM fund f
    ///    WHERE f.id IN (fund_ids)
    ///
    /// DATABASE INDEXES REQUIRED:
    /// - fund.id (primary key, already exists)
    ///
    /// USAGE EXAMPLE:
    ///   var funds = await dataLoader.LoadFundsByIdsAsync(
    ///       new[] { 1, 2, 3 },
    ///       cancellationToken);
    ///
    ///   // Now lookup is O(1):
    ///   if (funds.TryGetValue(fundId, out var fund)) {
    ///       var fundName = fund.Name;
    ///   }
    /// </summary>
    public async Task<Dictionary<int, Fund>> LoadFundsByIdsAsync(
        IEnumerable<int> fundIds,
        CancellationToken ct = default)
    {
        var ids = fundIds.ToList();
        if (ids.Count == 0)
        {
            return new();
        }

        // SINGLE optimized query
        var funds = await context.Funds
            .AsNoTracking()
            .Where(f => ids.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, ct);

        // Log any missing data (data quality issue, not performance)
        var missing = ids.Where(id => !funds.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            logger.LogWarning(
                "Fund not found for {Count} IDs: {FundIds}",
                missing.Count, string.Join(", ", missing.Take(5)));
        }

        return funds;
    }
}
