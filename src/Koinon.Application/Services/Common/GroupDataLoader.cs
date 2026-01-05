using Koinon.Application.Interfaces;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services.Common;

/// <summary>
/// Batch-loads all group-related data to eliminate N+1 query patterns.
/// All methods return pre-loaded dictionaries for O(1) lookups.
///
/// DESIGN RULES:
/// 1. Each method represents a complete data-loading operation
/// 2. No nested calls that make additional database queries
/// 3. Return dictionaries for O(1) lookups (never enumerate query results)
/// 4. Use Include/ThenInclude strategically to batch load relationships
/// 5. Use AsNoTracking() for all read queries
///
/// PERFORMANCE GOALS:
/// - LoadGroupsWithMembersAsync: 1 query for N groups, <100ms for 100 groups
/// - LoadMemberCountsAsync: 1 query, <50ms for 100 groups
/// - LoadGroupHierarchyAsync: 2-3 queries, <100ms for deep hierarchies
///
/// ANTI-PATTERN (DON'T DO THIS IN SERVICES):
///   foreach (var groupId in groupIds) {
///       var group = await context.Groups.FindAsync(groupId);        // N queries!
///       var members = await context.GroupMembers                   // N queries!
///           .Where(gm => gm.GroupId == groupId)
///           .ToListAsync();
///   }
///
/// CORRECT PATTERN (USE THIS):
///   var data = await dataLoader.LoadGroupsWithMembersAsync(groupIds, ct);
///   foreach (var groupId in groupIds) {
///       var group = data[groupId];  // O(1) lookup, zero queries
///   }
/// </summary>
public class GroupDataLoader(IApplicationDbContext context, ILogger<GroupDataLoader> logger)
{
    /// <summary>
    /// Loads groups with their active members, GroupType, and Campus in ONE query.
    /// Returns dictionary for O(1) lookup by group ID.
    ///
    /// WHY THIS MATTERS:
    /// - Old way: Query groups, then for each group query members = N+1 queries
    /// - New way: Single Include query, all data loaded
    ///
    /// QUERY PLAN:
    /// 1. SELECT g.*, gm.*, p.*, gr.*, gt.*, c.*
    ///    FROM groups g
    ///    LEFT JOIN group_member gm ON g.id = gm.group_id
    ///    LEFT JOIN person p ON gm.person_id = p.id
    ///    LEFT JOIN group_type_role gr ON gm.group_role_id = gr.id
    ///    LEFT JOIN group_type gt ON g.group_type_id = gt.id
    ///    LEFT JOIN campus c ON g.campus_id = c.id
    ///    WHERE g.id IN (group_ids)
    ///    AND gm.group_member_status = 'Active'
    ///
    /// DATABASE INDEXES REQUIRED:
    /// - groups.id (primary key, already exists)
    /// - group_member.group_id
    /// - group_member.group_member_status
    /// - group_member.person_id
    ///
    /// USAGE EXAMPLE:
    ///   var groups = await dataLoader.LoadGroupsWithMembersAsync(
    ///       new[] { 123, 456, 789 },
    ///       cancellationToken);
    ///
    ///   if (groups.TryGetValue(groupId, out var group)) {
    ///       var activeMembers = group.Members.ToList();
    ///       var groupType = group.GroupType;
    ///   }
    /// </summary>
    public async Task<Dictionary<int, Group>> LoadGroupsWithMembersAsync(
        IEnumerable<int> groupIds,
        CancellationToken ct = default)
    {
        var ids = groupIds.ToList();
        if (ids.Count == 0)
        {
            return new();
        }

        // SINGLE optimized query with all relationships
        var groups = await context.Groups
            .AsNoTracking()
            .Where(g => ids.Contains(g.Id))
            .Include(g => g.Members.Where(m => m.GroupMemberStatus == GroupMemberStatus.Active))
                .ThenInclude(m => m.Person)
            .Include(g => g.Members.Where(m => m.GroupMemberStatus == GroupMemberStatus.Active))
                .ThenInclude(m => m.GroupRole)
            .Include(g => g.GroupType)
            .Include(g => g.Campus)
            .ToListAsync(ct);

        var result = groups.ToDictionary(g => g.Id);

        // Log any missing data (data quality issue, not performance)
        var missing = ids.Where(id => !result.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            logger.LogWarning(
                "Groups not found for {Count} IDs: {GroupIds}",
                missing.Count, string.Join(", ", missing.Take(5)));
        }

        return result;
    }

    /// <summary>
    /// Loads active member counts for groups in ONE query.
    /// Returns dictionary mapping groupId -> active member count.
    /// Does NOT load full Member entities for efficiency.
    ///
    /// WHY THIS MATTERS:
    /// - Old way: Load full groups with members, count in memory = wasted bandwidth
    /// - New way: Database counts only, minimal data transfer
    ///
    /// QUERY PLAN:
    /// 1. SELECT gm.group_id, COUNT(*) as member_count
    ///    FROM group_member gm
    ///    WHERE gm.group_id IN (group_ids)
    ///    AND gm.group_member_status = 'Active'
    ///    GROUP BY gm.group_id
    ///
    /// DATABASE INDEXES REQUIRED:
    /// - group_member.group_id
    /// - group_member.group_member_status
    ///
    /// USAGE EXAMPLE:
    ///   var counts = await dataLoader.LoadMemberCountsAsync(
    ///       new[] { 123, 456, 789 },
    ///       cancellationToken);
    ///
    ///   if (counts.TryGetValue(groupId, out var count)) {
    ///       Console.WriteLine($"Group has {count} active members");
    ///   }
    /// </summary>
    public async Task<Dictionary<int, int>> LoadMemberCountsAsync(
        IEnumerable<int> groupIds,
        CancellationToken ct = default)
    {
        var ids = groupIds.ToList();
        if (ids.Count == 0)
        {
            return new();
        }

        // SINGLE optimized query - count only, no entity loading
        var counts = await context.GroupMembers
            .AsNoTracking()
            .Where(gm => ids.Contains(gm.GroupId) &&
                        gm.GroupMemberStatus == GroupMemberStatus.Active)
            .GroupBy(gm => gm.GroupId)
            .Select(g => new
            {
                GroupId = g.Key,
                Count = g.Count()
            })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, ct);

        return counts;
    }

    /// <summary>
    /// Loads a group with its complete hierarchy (parent chain and child groups).
    /// Uses iterative parent traversal and single query for children.
    ///
    /// NOTE: This method uses N+2 queries for depth N (1 for group + N for parents + 1 for children).
    /// This is acceptable because:
    /// 1. Group hierarchies are typically shallow (3-5 levels in most church organizations)
    /// 2. The method is used for single-group detail views, not bulk operations
    /// 3. Each query is indexed and returns minimal data
    ///
    /// For bulk hierarchy operations, prefer LoadGroupsWithMembersAsync with pre-calculated data.
    ///
    /// QUERY PLAN:
    /// 1. SELECT * FROM groups WHERE id = group_id (with GroupType, Campus)
    /// 2. For each parent level: SELECT * FROM groups WHERE id = parent_id
    /// 3. SELECT * FROM groups WHERE parent_group_id = group_id
    ///
    /// DATABASE INDEXES REQUIRED:
    /// - groups.id (primary key)
    /// - groups.parent_group_id
    ///
    /// USAGE EXAMPLE:
    ///   var hierarchy = await dataLoader.LoadGroupHierarchyAsync(groupId, ct);
    ///
    ///   var rootGroup = hierarchy.ParentChain.LastOrDefault() ?? hierarchy.Group;
    ///   var childCount = hierarchy.ChildGroups.Count;
    ///
    /// RETURNS:
    /// - Group: The requested group
    /// - ParentChain: List of parent groups from immediate parent to root (ascending order)
    /// - ChildGroups: List of immediate child groups
    /// </summary>
    public async Task<GroupHierarchyDto> LoadGroupHierarchyAsync(
        int groupId,
        CancellationToken ct = default)
    {
        // QUERY 1: Load the requested group with GroupType and Campus
        var group = await context.Groups
            .AsNoTracking()
            .Include(g => g.GroupType)
            .Include(g => g.Campus)
            .FirstOrDefaultAsync(g => g.Id == groupId, ct);

        if (group == null)
        {
            logger.LogWarning("Group {GroupId} not found", groupId);
            throw new KeyNotFoundException($"Group with ID {groupId} not found");
        }

        // QUERY 2: Load parent chain recursively
        var parentChain = new List<Group>();
        var currentParentId = group.ParentGroupId;

        while (currentParentId.HasValue)
        {
            var parent = await context.Groups
                .AsNoTracking()
                .Include(g => g.GroupType)
                .Include(g => g.Campus)
                .FirstOrDefaultAsync(g => g.Id == currentParentId.Value, ct);

            if (parent == null)
            {
                logger.LogWarning(
                    "Parent group {ParentGroupId} not found for group {GroupId}",
                    currentParentId.Value, groupId);
                break;
            }

            parentChain.Add(parent);
            currentParentId = parent.ParentGroupId;

            // Safety: prevent infinite loops from circular references
            if (parentChain.Count > 50)
            {
                logger.LogError(
                    "Circular reference detected in group hierarchy for group {GroupId}",
                    groupId);
                break;
            }
        }

        // QUERY 3: Load all child groups
        var childGroups = await context.Groups
            .AsNoTracking()
            .Where(g => g.ParentGroupId == groupId)
            .Include(g => g.GroupType)
            .Include(g => g.Campus)
            .OrderBy(g => g.Order)
            .ThenBy(g => g.Name)
            .ToListAsync(ct);

        return new GroupHierarchyDto(group, parentChain, childGroups);
    }
}

/// <summary>
/// DTO for group hierarchy including parent chain and child groups.
/// </summary>
public record GroupHierarchyDto(
    Group Group,
    List<Group> ParentChain,
    List<Group> ChildGroups);
