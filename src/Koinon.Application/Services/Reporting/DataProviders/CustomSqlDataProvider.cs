using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Koinon.Application.Interfaces;
using Koinon.Application.Interfaces.Reporting;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koinon.Application.Services.Reporting.DataProviders;

/// <summary>
/// Data provider for custom SQL queries in reports.
/// Executes user-defined SQL queries with security restrictions.
/// </summary>
public sealed class CustomSqlDataProvider(
    IApplicationDbContext context,
    ILogger<CustomSqlDataProvider> logger) : IReportDataProvider
{
    private const int MaxResultRows = 10000;
    private const int SlowQueryThresholdMs = 30000;

    // Security: Disallowed SQL keywords (read-only enforcement)
    private static readonly string[] _disallowedKeywords =
    [
        "DROP", "DELETE", "UPDATE", "INSERT", "TRUNCATE",
        "ALTER", "CREATE", "EXEC", "EXECUTE", "GRANT",
        "REVOKE", "DENY", "MERGE"
    ];

    public ReportType ReportType => ReportType.Custom;

    /// <summary>
    /// Executes a custom SQL query with security restrictions.
    /// </summary>
    /// <param name="parametersJson">JSON containing 'Query' field and optional 'QueryParameters'.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Query results limited to MaxResultRows.</returns>
    /// <exception cref="InvalidOperationException">Thrown when query contains disallowed SQL operations.</exception>
    public async Task<IReadOnlyList<Dictionary<string, object?>>> GetDataAsync(
        string parametersJson,
        CancellationToken ct = default)
    {
        var parameters = JsonSerializer.Deserialize<CustomSqlParameters>(parametersJson)
            ?? throw new ArgumentException("Invalid parameters JSON", nameof(parametersJson));

        if (string.IsNullOrWhiteSpace(parameters.Query))
        {
            throw new ArgumentException("Query field is required in parameters", nameof(parametersJson));
        }

        // Security validation
        ValidateQuerySecurity(parameters.Query);

        // Log query execution for audit trail
        logger.LogInformation(
            "Executing custom SQL query. Length: {QueryLength} characters, Parameters: {ParameterCount}",
            parameters.Query.Length,
            parameters.QueryParameters?.Count ?? 0);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var results = await ExecuteQueryAsync(parameters, ct);

            stopwatch.Stop();

            // Log slow queries
            if (stopwatch.ElapsedMilliseconds > SlowQueryThresholdMs)
            {
                logger.LogWarning(
                    "Slow custom SQL query executed in {ElapsedMs}ms. Consider optimizing the query.",
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                logger.LogInformation(
                    "Custom SQL query executed successfully in {ElapsedMs}ms, returned {RowCount} rows",
                    stopwatch.ElapsedMilliseconds,
                    results.Count);
            }

            return results;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            logger.LogError(ex, "Error executing custom SQL query");
            throw;
        }
    }

    /// <summary>
    /// Validates that the query does not contain disallowed SQL operations.
    /// </summary>
    /// <param name="query">SQL query to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown when query contains disallowed operations.</exception>
    private static void ValidateQuerySecurity(string query)
    {
        var upperQuery = query.ToUpperInvariant();

        // Check for disallowed keywords (whole word matches)
        foreach (var keyword in _disallowedKeywords)
        {
            // Use word boundary regex to avoid false positives (e.g., "INSERT" in "INSERTED_DATE")
            var pattern = $@"\b{keyword}\b";
            if (Regex.IsMatch(upperQuery, pattern))
            {
                throw new InvalidOperationException(
                    $"Query contains disallowed SQL keyword: {keyword}. Only SELECT queries are permitted.");
            }
        }

        // Check for multiple statements (no semicolons except optionally at the very end)
        var trimmedQuery = query.TrimEnd();
        if (trimmedQuery.EndsWith(';'))
        {
            trimmedQuery = trimmedQuery[..^1]; // Remove trailing semicolon
        }

        if (trimmedQuery.Contains(';'))
        {
            throw new InvalidOperationException(
                "Query contains multiple SQL statements. Only single SELECT queries are permitted.");
        }
    }

    /// <summary>
    /// Executes the SQL query using ADO.NET and returns results as dictionaries.
    /// </summary>
    private async Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteQueryAsync(
        CustomSqlParameters parameters,
        CancellationToken ct)
    {
        var connection = context.Database.GetDbConnection();
        
        await using var command = connection.CreateCommand();
        command.CommandText = parameters.Query;
        command.CommandType = CommandType.Text;
        command.CommandTimeout = SlowQueryThresholdMs / 1000; // Convert to seconds

        // Add query parameters if provided
        if (parameters.QueryParameters != null)
        {
            foreach (var (key, value) in parameters.QueryParameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = key.StartsWith('@') ? key : $"@{key}";
                parameter.Value = value ?? DBNull.Value;
                command.Parameters.Add(parameter);

                logger.LogDebug("Added parameter {ParameterName} with value type {ValueType}",
                    parameter.ParameterName,
                    value?.GetType().Name ?? "null");
            }
        }

        // Ensure connection is open
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        var results = new List<Dictionary<string, object?>>();

        await using var reader = await command.ExecuteReaderAsync(ct);

        var rowCount = 0;
        while (await reader.ReadAsync(ct))
        {
            if (rowCount >= MaxResultRows)
            {
                logger.LogWarning(
                    "Custom SQL query exceeded maximum row limit of {MaxRows}. Results truncated.",
                    MaxResultRows);
                break;
            }

            var row = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var value = await reader.IsDBNullAsync(i, ct) ? null : reader.GetValue(i);
                row[columnName] = value;
            }

            results.Add(row);
            rowCount++;
        }

        return results.AsReadOnly();
    }

    /// <summary>
    /// Parameters for custom SQL queries.
    /// </summary>
    private sealed class CustomSqlParameters
    {
        /// <summary>
        /// The SQL query to execute.
        /// </summary>
        public string Query { get; init; } = string.Empty;

        /// <summary>
        /// Optional parameterized query parameters.
        /// </summary>
        public Dictionary<string, object>? QueryParameters { get; init; }
    }
}
