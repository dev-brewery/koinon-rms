namespace Koinon.Application.Helpers;

/// <summary>
/// Helpers for coercing DateTime values to a Kind that PostgreSQL's
/// <c>timestamp with time zone</c> (timestamptz) columns will accept.
/// </summary>
/// <remarks>
/// Npgsql refuses to write a <see cref="DateTime"/> whose
/// <see cref="DateTime.Kind"/> is <see cref="DateTimeKind.Unspecified"/> to a
/// timestamptz column, which is how System.Text.Json-bound DateTimes arrive
/// from the frontend. These helpers normalize such values.
/// </remarks>
public static class DateTimeNormalization
{
    /// <summary>
    /// Normalizes a <see cref="DateTime"/> to <see cref="DateTimeKind.Utc"/>
    /// so it can be persisted to (or compared against) a PostgreSQL
    /// timestamptz column.
    /// </summary>
    /// <remarks>
    /// Treats <see cref="DateTimeKind.Unspecified"/> as already UTC. The
    /// frontend contract for this application is ISO-8601 with a <c>Z</c>
    /// suffix (or date-only strings) — both of those deserialize to a
    /// DateTime whose wall-clock is UTC but whose Kind is Unspecified,
    /// so re-stamping the Kind without converting is correct here. If a
    /// future caller sends genuinely local-time values, it should convert
    /// before handing the value in.
    /// </remarks>
    public static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
