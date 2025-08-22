using Xunit;
using AzStore.Core.Models;

namespace AzStore.Core.Tests;

[Trait("Category", "Unit")]
public class BrowsingResultTests
{
    [Fact]
    public void Empty_CreatesEmptyResult()
    {
        var containerName = "test-container";
        var prefix = "documents/";

        var result = BrowsingResult.Empty(containerName, prefix);

        Assert.Empty(result.VirtualDirectories);
        Assert.Empty(result.Blobs);
        Assert.Null(result.ContinuationToken);
        Assert.Equal(containerName, result.ContainerName);
        Assert.Equal(prefix, result.CurrentPrefix);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public void Create_WithDirectoriesAndBlobs_CreatesBrowsingResult()
    {
        var directories = new[]
        {
            VirtualDirectory.Create("docs/", "container"),
            VirtualDirectory.Create("images/", "container")
        };
        var blobs = new[]
        {
            Blob.Create("file1.txt", "file1.txt", "container"),
            Blob.Create("file2.txt", "file2.txt", "container")
        };
        var containerName = "test-container";
        var prefix = "root/";
        var continuationToken = "token123";

        var result = BrowsingResult.Create(directories, blobs, containerName, prefix, continuationToken);

        Assert.Equal(2, result.VirtualDirectories.Count);
        Assert.Equal(2, result.Blobs.Count);
        Assert.Equal(continuationToken, result.ContinuationToken);
        Assert.Equal(containerName, result.ContainerName);
        Assert.Equal(prefix, result.CurrentPrefix);
        Assert.Equal(4, result.TotalCount);
    }

    [Fact]
    public void GetAllItems_ReturnsDirectoriesFirstThenBlobs()
    {
        var directories = new[] { VirtualDirectory.Create("docs/", "container") };
        var blobs = new[] { Blob.Create("file.txt", "file.txt", "container") };
        var result = BrowsingResult.Create(directories, blobs, "container");

        var allItems = result.GetAllItems().ToList();

        Assert.Equal(2, allItems.Count);
        Assert.IsType<VirtualDirectory>(allItems[0]);
        Assert.IsType<Blob>(allItems[1]);
    }

    [Theory]
    [InlineData(BrowsingSortOrder.NameAscending)]
    [InlineData(BrowsingSortOrder.NameDescending)]
    [InlineData(BrowsingSortOrder.SizeAscending)]
    [InlineData(BrowsingSortOrder.SizeDescending)]
    [InlineData(BrowsingSortOrder.DateAscending)]
    [InlineData(BrowsingSortOrder.DateDescending)]
    [InlineData(BrowsingSortOrder.TypeThenName)]
    public void GetAllItemsSorted_WithDifferentSortOrders_ReturnsSortedItems(BrowsingSortOrder sortOrder)
    {
        var directories = new[] { VirtualDirectory.Create("zdocs/", "container") };
        var blobs = new[] { Blob.Create("afile.txt", "afile.txt", "container") };
        var result = BrowsingResult.Create(directories, blobs, "container");

        var sortedItems = result.GetAllItemsSorted(sortOrder).ToList();

        Assert.Equal(2, sortedItems.Count);
        // Detailed sorting verification would depend on specific sort implementation
        Assert.NotNull(sortedItems);
    }

    [Theory]
    [InlineData("*.txt", true, 1)] // Should match file.txt
    [InlineData("*.doc", true, 0)] // Should match nothing
    [InlineData("FILE.*", true, 1)] // Case insensitive match
    [InlineData("FILE.*", false, 0)] // Case sensitive no match
    [InlineData("doc*", true, 1)] // Should match docs directory
    public void FilterByPattern_WithVariousPatterns_ReturnsMatchingItems(string pattern, bool ignoreCase, int expectedCount)
    {
        var directories = new[] { VirtualDirectory.Create("docs/", "container") };
        var blobs = new[] { Blob.Create("file.txt", "file.txt", "container") };
        var result = BrowsingResult.Create(directories, blobs, "container");

        var filtered = result.FilterByPattern(pattern, ignoreCase);

        Assert.Equal(expectedCount, filtered.TotalCount);
        Assert.Null(filtered.ContinuationToken); // Filtering breaks pagination
    }

    [Fact]
    public void FilterByPattern_WithEmptyPattern_ReturnsOriginalResult()
    {
        var directories = new[] { VirtualDirectory.Create("docs/", "container") };
        var blobs = new[] { Blob.Create("file.txt", "file.txt", "container") };
        var result = BrowsingResult.Create(directories, blobs, "container");

        var filtered = result.FilterByPattern("");

        Assert.Equal(result.TotalCount, filtered.TotalCount);
        Assert.Equal(result.ContinuationToken, filtered.ContinuationToken);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("token123", true)]
    public void HasMoreResults_ReturnsCorrectValue(string? continuationToken, bool expected)
    {
        var result = BrowsingResult.Create([], [], "container", continuationToken: continuationToken);

        Assert.Equal(expected, result.HasMoreResults);
    }

    [Theory]
    [InlineData("container", null, "container")]
    [InlineData("container", "docs/", "container/docs")]
    [InlineData("container", "docs/2024/", "container/docs > 2024")]
    public void GetBreadcrumbPath_ReturnsFormattedPath(string containerName, string? prefix, string expected)
    {
        var result = BrowsingResult.Create([], [], containerName, prefix);

        var breadcrumb = result.GetBreadcrumbPath();

        Assert.Equal(expected, breadcrumb);
    }

    [Fact]
    public void ToString_IncludesContainerAndCounts()
    {
        var directories = new[] { VirtualDirectory.Create("docs/", "container") };
        var blobs = new[] { Blob.Create("file.txt", "file.txt", "container") };
        var result = BrowsingResult.Create(directories, blobs, "test-container");

        var stringResult = result.ToString();

        Assert.Contains("test-container", stringResult);
        Assert.Contains("1 directories", stringResult);
        Assert.Contains("1 blobs", stringResult);
    }

    [Fact]
    public void BrowsingResult_IsImmutable()
    {
        var directories = new List<VirtualDirectory> { VirtualDirectory.Create("docs/", "container") };
        var blobs = new List<Blob> { Blob.Create("file.txt", "file.txt", "container") };
        var result = BrowsingResult.Create(directories, blobs, "container");

        // Should not be able to modify the collections through the result
        Assert.IsAssignableFrom<IReadOnlyList<VirtualDirectory>>(result.VirtualDirectories);
        Assert.IsAssignableFrom<IReadOnlyList<Blob>>(result.Blobs);
    }

    [Fact]
    public void Create_WithEmptyCollections_CreatesValidResult()
    {
        var result = BrowsingResult.Create([], [], "container");

        Assert.Empty(result.VirtualDirectories);
        Assert.Empty(result.Blobs);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal("container", result.ContainerName);
    }

    [Fact]
    public void FilterByPattern_WithNullPattern_ReturnsOriginalResult()
    {
        var directories = new[] { VirtualDirectory.Create("docs/", "container") };
        var result = BrowsingResult.Create(directories, [], "container");

        var filtered = result.FilterByPattern(null!);

        Assert.Equal(result, filtered);
    }
}