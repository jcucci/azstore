namespace AzStore.Core.Models;

/// <summary>
/// Represents a page of results from a paginated operation.
/// </summary>
/// <typeparam name="T">The type of items in the page.</typeparam>
public record PagedResult<T>
{
    /// <summary>
    /// The items in this page.
    /// </summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>
    /// Token to retrieve the next page of results, or null if this is the last page.
    /// </summary>
    public string? ContinuationToken { get; init; }

    /// <summary>
    /// Indicates whether there are more pages available.
    /// </summary>
    public bool HasMore => !string.IsNullOrEmpty(ContinuationToken);

    /// <summary>
    /// The number of items in this page.
    /// </summary>
    public int Count => Items.Count;

    /// <summary>
    /// Initializes a new instance of PagedResult with the specified items and continuation token.
    /// </summary>
    /// <param name="items">The items in this page.</param>
    /// <param name="continuationToken">Token for the next page, or null if this is the last page.</param>
    public PagedResult(IReadOnlyList<T> items, string? continuationToken = null)
    {
        Items = items ?? [];
        ContinuationToken = continuationToken;
    }

    /// <summary>
    /// Creates an empty paged result with no items and no continuation token.
    /// </summary>
    /// <returns>An empty PagedResult.</returns>
    public static PagedResult<T> Empty() => new([], null);
}