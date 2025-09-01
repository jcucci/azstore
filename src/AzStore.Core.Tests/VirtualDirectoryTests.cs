using Xunit;
using AzStore.Core.Models;
using AzStore.Core.Models.Storage;

namespace AzStore.Core.Tests;

[Trait("Category", "Unit")]
public class VirtualDirectoryTests
{
    [Fact]
    public void Create_WithRootLevelPrefix_CreatesVirtualDirectory()
    {
        var prefix = "documents/";
        var containerName = "test-container";

        var directory = VirtualDirectory.Create(prefix, containerName);

        Assert.Equal("documents", directory.Name);
        Assert.Equal("test-container/documents/", directory.Path);
        Assert.Equal(prefix, directory.Prefix);
        Assert.Equal(containerName, directory.ContainerName);
        Assert.Equal(1, directory.Depth);
    }

    [Fact]
    public void Create_WithNestedPrefix_CreatesVirtualDirectory()
    {
        var prefix = "documents/2024/reports/";
        var containerName = "test-container";

        var directory = VirtualDirectory.Create(prefix, containerName);

        Assert.Equal("reports", directory.Name);
        Assert.Equal("test-container/documents/2024/reports/", directory.Path);
        Assert.Equal(prefix, directory.Prefix);
        Assert.Equal(containerName, directory.ContainerName);
        Assert.Equal(3, directory.Depth);
    }

    [Fact]
    public void Create_WithPrefixWithoutTrailingSlash_HandlesCorrectly()
    {
        var prefix = "documents";
        var containerName = "test-container";

        var directory = VirtualDirectory.Create(prefix, containerName);

        Assert.Equal("documents", directory.Name);
        Assert.Equal("test-container/documents", directory.Path);
        Assert.Equal(prefix, directory.Prefix);
        Assert.Equal(1, directory.Depth);
    }

    [Fact]
    public void Create_WithItemCount_SetsItemCount()
    {
        var prefix = "documents/";
        var containerName = "test-container";
        var itemCount = 42;

        var directory = VirtualDirectory.Create(prefix, containerName, itemCount);

        Assert.Equal(itemCount, directory.ItemCount);
    }

    [Fact]
    public void GetParentPrefix_WithNestedDirectory_ReturnsParentPrefix()
    {
        var directory = VirtualDirectory.Create("documents/2024/reports/", "container");

        var parentPrefix = directory.GetParentPrefix();

        Assert.Equal("documents/2024/", parentPrefix);
    }

    [Fact]
    public void GetParentPrefix_WithRootDirectory_ReturnsNull()
    {
        var directory = VirtualDirectory.Create("documents/", "container");

        var parentPrefix = directory.GetParentPrefix();

        Assert.Null(parentPrefix);
    }

    [Fact]
    public void GetParentPrefix_WithSingleLevelPrefix_ReturnsNull()
    {
        var directory = VirtualDirectory.Create("documents", "container");

        var parentPrefix = directory.GetParentPrefix();

        Assert.Null(parentPrefix);
    }

    [Theory]
    [InlineData("documents/", new[] { "container", "documents" })]
    [InlineData("documents/2024/", new[] { "container", "documents", "2024" })]
    [InlineData("documents/2024/reports/", new[] { "container", "documents", "2024", "reports" })]
    public void GetPathSegments_ReturnsCorrectSegments(string prefix, string[] expectedSegments)
    {
        var directory = VirtualDirectory.Create(prefix, "container");

        var segments = directory.GetPathSegments();

        Assert.Equal(expectedSegments, segments);
    }

    [Theory]
    [InlineData("documents/", null, true)] // Root level directory
    [InlineData("documents/2024/", "documents/", true)] // Direct child
    [InlineData("documents/2024/reports/", "documents/2024/", true)] // Direct child
    [InlineData("documents/2024/", "other/", false)] // Not a child
    [InlineData("documents/2024/reports/", "documents/", false)] // Not direct (grandchild)
    public void IsDirectChildOf_ReturnsCorrectResult(string prefix, string? parentPrefix, bool expected)
    {
        var directory = VirtualDirectory.Create(prefix, "container");

        var result = directory.IsDirectChildOf(parentPrefix);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToString_WithItemCount_IncludesItemCount()
    {
        var directory = VirtualDirectory.Create("documents/", "container", 5);

        var result = directory.ToString();

        Assert.Contains("documents", result);
        Assert.Contains("5 items", result);
    }

    [Fact]
    public void ToString_WithoutItemCount_DoesNotIncludeItemCount()
    {
        var directory = VirtualDirectory.Create("documents/", "container");

        var result = directory.ToString();

        Assert.Contains("documents", result);
        Assert.DoesNotContain("items", result);
    }

    [Fact]
    public void VirtualDirectory_InheritsFromStorageItem()
    {
        var directory = VirtualDirectory.Create("test/", "container");

        Assert.IsAssignableFrom<StorageItem>(directory);
    }

    [Fact]
    public void Create_SetsLastModifiedAndSizeToNull()
    {
        var directory = VirtualDirectory.Create("test/", "container");

        Assert.Null(directory.LastModified);
        Assert.Null(directory.Size);
    }
}