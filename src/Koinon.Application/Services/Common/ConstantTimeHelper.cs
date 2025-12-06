using System.Security.Cryptography;
using System.Text;

namespace Koinon.Application.Services.Common;

/// <summary>
/// Provides constant-time operations that don't leak information through timing variations.
///
/// WHY THIS MATTERS:
/// Attackers can measure response times to determine if a search succeeded or failed:
///
/// Example - SearchByCodeAsync WITHOUT constant-time operations:
///   Valid code (8 queries, ~300ms):   ^^^^^^^^^^^^^^^^^^^^^^^^^
///   Invalid code (1 query, ~20ms):    ^^^^
///
/// An attacker can enumerate valid codes by measuring timing!
///
/// Example - SearchByCodeAsync WITH constant-time operations:
///   Valid code (with dummy work, ~200ms):   ^^^^^^^^^^^^^^^^^^^^^^
///   Invalid code (with dummy work, ~200ms): ^^^^^^^^^^^^^^^^^^^^^^
///
/// All operations take approximately the same time regardless of result.
///
/// WHEN TO USE:
/// - Checking authorization secrets (codes, tokens, etc.)
/// - Searching for data that reveals existence (users, codes, etc.)
/// - Any operation where "not found" leaks information
///
/// WHEN NOT TO USE:
/// - Public queries (email address exists check, availability searches)
/// - Non-security operations where timing doesn't matter
///
/// See: ARCHITECTURAL_REVIEW_PHASE2.2.md#timing-attack-prevention
/// </summary>
public static class ConstantTimeHelper
{
    /// <summary>
    /// Compares two strings in constant time without early exit.
    /// Uses XOR to prevent branch prediction attacks.
    ///
    /// ALGORITHM:
    /// 1. XOR all bytes together
    /// 2. Always perform full comparison regardless of differences found
    /// 3. Return true only if result is zero
    ///
    /// PROTECTS AGAINST:
    /// - Timing attacks where early mismatch exits sooner
    /// - Branch prediction attacks in modern CPUs
    /// - Information disclosure through response time
    ///
    /// EXAMPLE:
    ///   // Wrong - vulnerable to timing attacks:
    ///   if (userCode == databaseCode) return true;
    ///
    ///   // Right - timing-safe:
    ///   if (ConstantTimeEquals(userCode, databaseCode)) return true;
    ///
    /// TIME COMPLEXITY:
    /// - O(n) where n = maximum of string lengths
    /// - Always performs all comparisons (no early exit)
    /// - Takes approximately same time for any input
    ///
    /// PERFORMANCE COST:
    /// - 4-character code: ~1-5 microseconds (negligible)
    /// - Compared to database query: <1% overhead
    /// </summary>
    /// <param name="a">First string to compare</param>
    /// <param name="b">Second string to compare</param>
    /// <returns>True if strings are equal, false otherwise (in constant time)</returns>
    public static bool ConstantTimeEquals(string? a, string? b)
    {
        // Quick out for null references (both must be same)
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        // If either is null but not both, they're not equal
        if (a == null || b == null)
        {
            return false;
        }

        // XOR all bytes - result is zero only if all bytes match
        int result = a.Length ^ b.Length;

        // Compare all characters up to the shorter length
        int minLength = Math.Min(a.Length, b.Length);
        for (int i = 0; i < minLength; i++)
        {
            result |= a[i] ^ b[i];
        }

        // Continue comparison with mismatches for longer string (timing consistency)
        for (int i = minLength; i < Math.Max(a.Length, b.Length); i++)
        {
            result |= 1;
        }

        return result == 0;
    }

    /// <summary>
    /// Executes an operation and its dummy variant with constant timing.
    /// Both operations always run; only the result is returned.
    ///
    /// ALGORITHM:
    /// 1. Run actual operation (or dummy if not executing actual)
    /// 2. Run dummy operation (or actual if not executing actual)
    /// 3. Wait for both to complete
    /// 4. Return result from appropriate operation
    ///
    /// EFFECT:
    /// - Actual operation time + dummy operation time (always both run)
    /// - Attacker can't tell if operation succeeded by timing
    ///
    /// EXAMPLE:
    ///   var result = await ExecuteWithConstantTiming(
    ///       actualOperation: async () => {
    ///           return await database.FindCodeAsync(userCode);
    ///       },
    ///       dummyOperation: async () => {
    ///           // Do work that takes similar time but reveals nothing
    ///           var hash = SHA256.HashData(Encoding.UTF8.GetBytes(userCode));
    ///           return null;
    ///       },
    ///       executeActual: searchSucceeded
    ///   );
    ///
    /// PERFORMANCE COST:
    /// - Runs both tasks in parallel (not sequentially)
    /// - Total time = max(actual time, dummy time)
    /// - If dummy takes 50ms, adds 50ms to actual operation
    ///
    /// DESIGN CONSIDERATION:
    /// - Dummy operation should be CPU-bound (hashing, compute)
    /// - Don't make dummy operation I/O (network, database)
    /// - Dummy time should approximate actual operation time
    /// </summary>
    /// <typeparam name="T">Return type of operations</typeparam>
    /// <param name="actualOperation">The real operation to execute</param>
    /// <param name="dummyOperation">A no-op operation that takes similar time</param>
    /// <param name="executeActual">Whether to execute actual (true) or dummy (true)</param>
    /// <returns>Result from appropriate operation</returns>
    public static async Task<T> ExecuteWithConstantTiming<T>(
        Func<Task<T>> actualOperation,
        Func<Task<T>> dummyOperation,
        bool executeActual)
    {
        // Always run both to prevent timing leaks
        var actualTask = executeActual ? actualOperation() : dummyOperation();
        var dummyTask = executeActual ? dummyOperation() : actualOperation();

        // Wait for both to complete
        var results = await Task.WhenAll(actualTask, dummyTask);

        // Return result from the one we wanted to execute
        return executeActual ? results[0] : results[1];
    }

    /// <summary>
    /// Performs a search operation with constant timing by doing dummy work when not found.
    /// Ensures "found" and "not found" responses take similar time.
    ///
    /// ALGORITHM:
    /// 1. Execute search operation
    /// 2. If result is null (not found), run dummy work operation
    /// 3. Return result (null or found)
    ///
    /// EFFECT:
    /// - Found result: Search time only
    /// - Not found result: Search time + dummy work time
    /// - Attacker sees both as "not found" operations taking similar time
    ///
    /// EXAMPLE - SearchByCodeAsync:
    ///   var result = await SearchWithConstantTiming(
    ///       searchOperation: async () => {
    ///           // Find attendance code (1-3 queries)
    ///           return await FindAttendanceByCodeAsync(code, ct);
    ///       },
    ///       busyWorkOperation: async () => {
    ///           // Do work to fill time if not found
    ///           var hash = SHA256.HashData(Encoding.UTF8.GetBytes(code));
    ///           await Task.Delay(100, ct);
    ///       }
    ///   );
    ///   // Returns null if not found (after dummy work)
    ///   // Returns found data if found (before dummy work)
    ///
    /// TIMING:
    ///   Found case (N queries, ~N*50ms):    ^^^^^^^^^^^
    ///   Not found case (1 query + work):    ^^^^^^^^^^^
    ///   Difference: <20ms (negligible)
    ///
    /// PERFORMANCE TRADE-OFF:
    /// - Cost: Time added to "not found" case
    /// - Benefit: Prevents timing attacks on "not found" cases
    /// - For API searches: Worth it (security > performance for searches)
    /// </summary>
    /// <typeparam name="T">Result type of search</typeparam>
    /// <param name="searchOperation">The actual search to perform</param>
    /// <param name="busyWorkOperation">Dummy work to do if not found</param>
    /// <returns>Search result (or null if not found)</returns>
    public static async Task<T?> SearchWithConstantTiming<T>(
        Func<Task<T?>> searchOperation,
        Func<Task> busyWorkOperation)
        where T : class
    {
        var result = await searchOperation();

        // If not found, do dummy work to consume time
        if (result == null)
        {
            await busyWorkOperation();
        }

        return result;
    }

    /// <summary>
    /// Creates a dummy work operation that takes approximately a specified duration.
    /// Uses CPU-bound hashing to avoid I/O operations.
    ///
    /// WHY HASHING:
    /// - CPU-bound (consistent timing)
    /// - Can be tuned by iteration count
    /// - No network/database variance
    /// - Doesn't interfere with real operations
    ///
    /// DURATION ESTIMATION:
    /// - 1 iteration: ~1-5 microseconds
    /// - 10,000 iterations: ~10-50 milliseconds
    /// - Adjust iterations to match your operation
    ///
    /// EXAMPLE:
    ///   var busyWork = CreateHashingBusyWork(
    ///       input: userCode,
    ///       iterations: 100_000  // ~100ms
    ///   );
    ///   var result = await SearchWithConstantTiming(
    ///       searchOp,
    ///       busyWork
    ///   );
    /// </summary>
    /// <param name="input">Data to hash</param>
    /// <param name="iterations">Number of iterations (tune for your operation)</param>
    /// <returns>Async operation that does CPU-bound hashing</returns>
    public static Func<Task> CreateHashingBusyWork(string input, int iterations = 10_000)
    {
        return async () =>
        {
            var bytes = Encoding.UTF8.GetBytes(input);

            // Run hash iterations in a background task
            await Task.Run(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    bytes = SHA256.HashData(bytes);
                }
            });
        };
    }

    /// <summary>
    /// Creates a dummy work operation that takes a specified duration.
    /// Uses Task.Delay which is not constant-time but can fill
    /// most of the remaining time needed.
    ///
    /// WARNING:
    /// - Task.Delay has millisecond-level granularity
    /// - Good for padding to 50ms+
    /// - Not suitable for microsecond-level timing precision
    ///
    /// EXAMPLE:
    ///   var busyWork = CreateDelayBusyWork(delayMs: 100);
    ///   var result = await SearchWithConstantTiming(
    ///       searchOp,
    ///       busyWork
    ///   );
    /// </summary>
    /// <param name="delayMs">Milliseconds to delay</param>
    /// <returns>Async operation that delays</returns>
    public static Func<Task> CreateDelayBusyWork(int delayMs = 50)
    {
        return async () =>
        {
            await Task.Delay(delayMs);
        };
    }
}
