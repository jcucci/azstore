namespace AzStore.Core.Models;

/// <summary>
/// Represents a request for a page of results in a paginated operation.
/// </summary>
public record PageRequest
{
    /// <summary>
    /// The maximum number of items to return in a single page.
    /// Default is 100, maximum is 5000 (Azure Storage limit).
    /// </summary>
    public int PageSize { get; init; } = 100;

    /// <summary>
    /// Token from a previous response to continue pagination.
    /// If null, starts from the beginning.
    /// </summary>
    public string? ContinuationToken { get; init; }

    /// <summary>
    /// Initializes a new instance of PageRequest with default page size.
    /// </summary>
    public PageRequest() { }

    /// <summary>
    /// Initializes a new instance of PageRequest with the specified page size.
    /// </summary>
    /// <param name="pageSize">The maximum number of items to return.</param>
    public PageRequest(int pageSize)
    {
        if (pageSize <= 0)
            throw new ArgumentException("Page size must be greater than zero.", nameof(pageSize));
        if (pageSize > 5000)
            throw new ArgumentException("Page size cannot exceed 5000 items.", nameof(pageSize));

        PageSize = pageSize;
    }

    /// <summary>
    /// Initializes a new instance of PageRequest with the specified page size and continuation token.
    /// </summary>
    /// <param name="pageSize">The maximum number of items to return.</param>
    /// <param name="continuationToken">Token from a previous response to continue pagination.</param>
    public PageRequest(int pageSize, string? continuationToken) : this(pageSize)
    {
        ContinuationToken = continuationToken;
    }

    /// <summary>
    /// Creates a new PageRequest for the next page using the provided continuation token.
    /// </summary>
    /// <param name="continuationToken">The continuation token from the current page.</param>
    /// <returns>A new PageRequest for the next page.</returns>
    public PageRequest NextPage(string continuationToken) => this with { ContinuationToken = continuationToken };

    /// <summary>
    /// Creates a PageRequest for the first page with the default page size.
    /// </summary>
    /// <returns>A new PageRequest for the first page.</returns>
    public static PageRequest FirstPage() => new();

    /// <summary>
    /// Creates a PageRequest for the first page with the specified page size.
    /// </summary>
    /// <param name="pageSize">The maximum number of items to return.</param>
    /// <returns>A new PageRequest for the first page.</returns>
    public static PageRequest FirstPage(int pageSize) => new(pageSize);
}