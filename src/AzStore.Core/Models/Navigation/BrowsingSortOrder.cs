namespace AzStore.Core.Models.Navigation;

/// <summary>
/// Defines the available sorting options for browsing results.
/// </summary>
public enum BrowsingSortOrder
{
    /// <summary>
    /// Sort by name in ascending order (A-Z).
    /// </summary>
    NameAscending,

    /// <summary>
    /// Sort by name in descending order (Z-A).
    /// </summary>
    NameDescending,

    /// <summary>
    /// Sort by size in ascending order (smallest first).
    /// </summary>
    SizeAscending,

    /// <summary>
    /// Sort by size in descending order (largest first).
    /// </summary>
    SizeDescending,

    /// <summary>
    /// Sort by last modified date in ascending order (oldest first).
    /// </summary>
    DateAscending,

    /// <summary>
    /// Sort by last modified date in descending order (newest first).
    /// </summary>
    DateDescending,

    /// <summary>
    /// Sort by type (directories first) then by name.
    /// </summary>
    TypeThenName
}