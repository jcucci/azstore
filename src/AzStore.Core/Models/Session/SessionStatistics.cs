namespace AzStore.Core.Models.Session;

/// <summary>
/// Provides statistics about session storage and usage patterns.
/// </summary>
/// <param name="TotalSessions">The total number of sessions currently stored.</param>
/// <param name="ActiveSessions">The number of sessions accessed within the last 7 days.</param>
/// <param name="OldSessions">The number of sessions not accessed within the last 30 days.</param>
/// <param name="AverageAge">The average age of all sessions in days.</param>
/// <param name="OldestSessionAge">The age of the oldest session in days.</param>
/// <param name="StorageAccountsCount">The number of unique storage accounts across all sessions.</param>
public record SessionStatistics(
    int TotalSessions,
    int ActiveSessions,
    int OldSessions,
    double AverageAge,
    double OldestSessionAge,
    int StorageAccountsCount)
{
    /// <summary>
    /// Creates empty session statistics for when no sessions exist.
    /// </summary>
    /// <returns>A SessionStatistics instance with all values set to zero.</returns>
    public static SessionStatistics Empty => new(0, 0, 0, 0, 0, 0);

    /// <summary>
    /// Returns a formatted string representation of the session statistics.
    /// </summary>
    /// <returns>A multi-line string containing formatted statistics.</returns>
    public override string ToString()
    {
        return $"""
            Total Sessions: {TotalSessions}
            Active (last 7 days): {ActiveSessions}
            Old (30+ days): {OldSessions}
            Average Age: {AverageAge:F1} days
            Oldest Session: {OldestSessionAge:F1} days
            Storage Accounts: {StorageAccountsCount}
            """;
    }
}