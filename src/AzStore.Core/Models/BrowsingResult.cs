using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace AzStore.Core.Models;

/// <summary>
/// Represents the result of a hierarchical blob browsing operation containing both virtual directories and blobs.
/// </summary>
/// <param name="VirtualDirectories">The virtual directories at the current navigation level.</param>
/// <param name="Blobs">The blobs at the current navigation level.</param>
/// <param name="ContinuationToken">Token for retrieving the next page of results.</param>
/// <param name="ContainerName">The name of the container being browsed.</param>
/// <param name="CurrentPrefix">The current blob prefix (virtual directory path) being browsed.</param>
/// <param name="TotalCount">The total number of items (directories + blobs) in this result.</param>
public record BrowsingResult(
    [property: Required] IReadOnlyList<VirtualDirectory> VirtualDirectories,
    [property: Required] IReadOnlyList<Blob> Blobs,
    string? ContinuationToken,
    [property: Required, MinLength(1)] string ContainerName,
    string? CurrentPrefix,
    int TotalCount)
{
    /// <summary>
    /// Creates an empty browsing result for cases where no items are found.
    /// </summary>
    /// <param name="containerName">The name of the container being browsed.</param>
    /// <param name="currentPrefix">The current prefix being browsed.</param>
    /// <returns>An empty BrowsingResult.</returns>
    public static BrowsingResult Empty(string containerName, string? currentPrefix = null)
    {
        return new BrowsingResult(
            VirtualDirectories: [],
            Blobs: [],
            ContinuationToken: null,
            ContainerName: containerName,
            CurrentPrefix: currentPrefix,
            TotalCount: 0);
    }

    /// <summary>
    /// Creates a browsing result from separate collections of directories and blobs.
    /// </summary>
    /// <param name="directories">The virtual directories to include.</param>
    /// <param name="blobs">The blobs to include.</param>
    /// <param name="containerName">The name of the container being browsed.</param>
    /// <param name="currentPrefix">The current prefix being browsed.</param>
    /// <param name="continuationToken">Optional continuation token for pagination.</param>
    /// <returns>A new BrowsingResult containing the specified items.</returns>
    public static BrowsingResult Create(
        IEnumerable<VirtualDirectory> directories,
        IEnumerable<Blob> blobs,
        string containerName,
        string? currentPrefix = null,
        string? continuationToken = null)
    {
        var dirList = directories.ToList();
        var blobList = blobs.ToList();

        return new BrowsingResult(
            VirtualDirectories: dirList,
            Blobs: blobList,
            ContinuationToken: continuationToken,
            ContainerName: containerName,
            CurrentPrefix: currentPrefix,
            TotalCount: dirList.Count + blobList.Count);
    }

    /// <summary>
    /// Gets all items (virtual directories and blobs) as a unified collection of StorageItem.
    /// Virtual directories are returned first, followed by blobs.
    /// </summary>
    /// <returns>A unified collection of all storage items.</returns>
    public IEnumerable<StorageItem> GetAllItems()
    {
        return VirtualDirectories.Cast<StorageItem>().Concat(Blobs.Cast<StorageItem>());
    }

    /// <summary>
    /// Gets all items sorted by a specified criteria.
    /// </summary>
    /// <param name="sortBy">The sorting criteria to apply.</param>
    /// <returns>A sorted collection of all storage items.</returns>
    public IEnumerable<StorageItem> GetAllItemsSorted(BrowsingSortOrder sortBy = BrowsingSortOrder.NameAscending)
    {
        var allItems = GetAllItems();

        return sortBy switch
        {
            BrowsingSortOrder.NameAscending => allItems.OrderBy(item => item.Name),
            BrowsingSortOrder.NameDescending => allItems.OrderByDescending(item => item.Name),
            BrowsingSortOrder.SizeAscending => allItems.OrderBy(item => item.Size ?? -1),
            BrowsingSortOrder.SizeDescending => allItems.OrderByDescending(item => item.Size ?? -1),
            BrowsingSortOrder.DateAscending => allItems.OrderBy(item => item.LastModified ?? DateTimeOffset.MinValue),
            BrowsingSortOrder.DateDescending => allItems.OrderByDescending(item => item.LastModified ?? DateTimeOffset.MinValue),
            BrowsingSortOrder.TypeThenName => allItems.OrderBy(item => item is VirtualDirectory ? 0 : 1).ThenBy(item => item.Name),
            _ => allItems
        };
    }

    /// <summary>
    /// Filters the browsing result to include only items matching the specified pattern.
    /// </summary>
    /// <param name="pattern">The pattern to match against item names (supports wildcards * and ?).</param>
    /// <param name="ignoreCase">Whether to ignore case when matching.</param>
    /// <returns>A new BrowsingResult containing only matching items.</returns>
    public BrowsingResult FilterByPattern(string pattern, bool ignoreCase = true)
    {
        if (string.IsNullOrEmpty(pattern))
            return this;

        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        var options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
        var regex = new Regex(regexPattern, options);

        var filteredDirectories = VirtualDirectories.Where(d => regex.IsMatch(d.Name)).ToList();
        var filteredBlobs = Blobs.Where(b => regex.IsMatch(b.Name)).ToList();

        return new BrowsingResult(
            VirtualDirectories: filteredDirectories,
            Blobs: filteredBlobs,
            ContinuationToken: null, // Filtering breaks pagination
            ContainerName: ContainerName,
            CurrentPrefix: CurrentPrefix,
            TotalCount: filteredDirectories.Count + filteredBlobs.Count);
    }

    /// <summary>
    /// Gets a value indicating whether there are more results available.
    /// </summary>
    /// <returns>true if there are more results available; otherwise, false.</returns>
    public bool HasMoreResults => !string.IsNullOrEmpty(ContinuationToken);

    /// <summary>
    /// Gets the breadcrumb path for the current browsing location.
    /// </summary>
    /// <returns>A formatted breadcrumb path.</returns>
    public string GetBreadcrumbPath()
    {
        if (string.IsNullOrEmpty(CurrentPrefix))
            return ContainerName;

        var trimmedPrefix = CurrentPrefix.TrimEnd('/');
        return $"{ContainerName}/{trimmedPrefix.Replace("/", " > ")}";
    }

    /// <summary>
    /// Returns a summary string representation of the browsing result.
    /// </summary>
    /// <returns>A formatted string containing result statistics.</returns>
    public override string ToString()
    {
        var pathInfo = string.IsNullOrEmpty(CurrentPrefix) ? ContainerName : $"{ContainerName}/{CurrentPrefix.TrimEnd('/')}";
        return $"BrowsingResult: {pathInfo} ({VirtualDirectories.Count} directories, {Blobs.Count} blobs)";
    }
}