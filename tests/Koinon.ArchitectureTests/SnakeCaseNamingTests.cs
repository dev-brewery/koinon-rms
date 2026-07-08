using System.Text.RegularExpressions;
using FluentAssertions;
using Koinon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Koinon.ArchitectureTests;

/// <summary>
/// Enforces snake_case database naming (ADR 0003). There is no global EF
/// naming convention — every entity's *Configuration.cs must map names
/// explicitly — so this test is what turns that convention into a guarantee.
/// It builds the EF model in memory (no database connection is made).
/// </summary>
public partial class SnakeCaseNamingTests
{
    [GeneratedRegex("^[a-z][a-z0-9_]*$")]
    private static partial Regex SnakeCase();

    private static KoinonDbContext CreateModelOnlyContext()
    {
        // Model building never opens the connection; the string just selects
        // the Npgsql provider so mappings match production.
        var options = new DbContextOptionsBuilder<KoinonDbContext>()
            .UseNpgsql("Host=model-only;Database=model-only", o => o.UseNetTopologySuite())
            .Options;
        return new KoinonDbContext(options);
    }

    [Fact]
    public void AllTablesAndColumns_AreSnakeCase()
    {
        using var context = CreateModelOnlyContext();
        var violations = new List<string>();

        foreach (var entityType in context.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (tableName is null)
            {
                continue; // not mapped to a table (view/query type)
            }

            if (!SnakeCase().IsMatch(tableName))
            {
                violations.Add($"table \"{tableName}\" ({entityType.ClrType.Name})");
            }

            var storeObject = StoreObjectIdentifier.Table(tableName, entityType.GetSchema());
            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName(storeObject);
                if (columnName is not null && !SnakeCase().IsMatch(columnName))
                {
                    violations.Add(
                        $"column \"{tableName}.{columnName}\" ({entityType.ClrType.Name}.{property.Name})");
                }
            }
        }

        violations.Should().BeEmpty(
            "every table/column must be snake_case via an explicit *Configuration.cs — " +
            "a violation usually means a new entity is missing its EF configuration " +
            "(ADR 0003, docs/adr/0003-snake-case-database-naming.md)");
    }
}
