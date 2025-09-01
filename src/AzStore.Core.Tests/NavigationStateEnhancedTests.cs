using Xunit;
using AzStore.Core.Models;
using AzStore.Core.Models.Navigation;

namespace AzStore.Core.Tests;

[Trait("Category", "Unit")]
public class NavigationStateEnhancedTests
{
    [Theory]
    [InlineData("documents/file.txt", "documents")]
    [InlineData("documents/2024/report.pdf", "documents/2024")]
    [InlineData("file.txt", null)] // Root level file
    public void NavigateToBlobPath_WithBlobInDirectory_NavigatesToContainingDirectory(string blobPath, string? expectedPrefix)
    {
        var state = NavigationState.CreateInContainer("session", "account", "container");

        var result = state.NavigateToBlobPath(blobPath);

        Assert.Equal(expectedPrefix, result.BlobPrefix);
        Assert.Equal("container", result.ContainerName);
    }

    [Fact]
    public void NavigateToBlobPath_WhenNotInContainer_ReturnsCurrentState()
    {
        var state = NavigationState.CreateAtRoot("session", "account");

        var result = state.NavigateToBlobPath("documents/file.txt");

        Assert.Equal(state, result);
    }

    [Theory]
    [InlineData("session", "account", null, null, true)] // At root
    [InlineData("session", "account", "container", null, false)] // In container
    [InlineData("session", "account", "container", "docs/", false)] // In subdirectory
    public void GetParentState_ReturnsCorrectParent(string session, string account, string? container, string? prefix, bool expectNull)
    {
        var state = container == null
            ? NavigationState.CreateAtRoot(session, account)
            : NavigationState.CreateInContainer(session, account, container, prefix);

        var parent = state.GetParentState();

        if (expectNull)
        {
            Assert.Null(parent);
        }
        else
        {
            Assert.NotNull(parent);
        }
    }

    [Theory]
    [InlineData("account", null, null, new[] { "account" })]
    [InlineData("account", "container", null, new[] { "account", "container" })]
    [InlineData("account", "container", "docs/", new[] { "account", "container", "docs" })]
    [InlineData("account", "container", "docs/2024/reports/", new[] { "account", "container", "docs", "2024", "reports" })]
    public void GetPathSegments_ReturnsCorrectSegments(string account, string? container, string? prefix, string[] expected)
    {
        var state = container == null
            ? NavigationState.CreateAtRoot("session", account)
            : NavigationState.CreateInContainer("session", account, container, prefix);

        var segments = state.GetPathSegments();

        Assert.Equal(expected, segments);
    }

    [Theory]
    [InlineData(0, "account", null, null)] // Navigate to root
    [InlineData(1, "account", "container", null)] // Navigate to container
    [InlineData(2, "account", "container", "docs")] // Navigate to first level
    public void NavigateToSegment_NavigatesToCorrectLevel(int segmentIndex, string expectedAccount, string? expectedContainer, string? expectedPrefix)
    {
        var state = NavigationState.CreateInContainer("session", "account", "container", "docs/2024/reports/");

        var result = state.NavigateToSegment(segmentIndex);

        Assert.Equal(expectedAccount, result.StorageAccountName);
        Assert.Equal(expectedContainer, result.ContainerName);
        Assert.Equal(expectedPrefix, result.BlobPrefix);
    }

    [Fact]
    public void NavigateToSegment_WithInvalidIndex_ReturnsCurrentState()
    {
        var state = NavigationState.CreateInContainer("session", "account", "container", "docs/");

        var result = state.NavigateToSegment(-1);
        Assert.Equal(state, result);

        result = state.NavigateToSegment(10);
        Assert.Equal(state, result);
    }

    [Theory]
    [InlineData("account", null, null, "account")]
    [InlineData("account", "container", null, "account/container")]
    [InlineData("account", "container", "docs/", "account/container/docs")]
    [InlineData("account", "container", "docs/2024/", "account/container/docs/2024")]
    public void GetCurrentPath_ReturnsFormattedPath(string account, string? container, string? prefix, string expected)
    {
        var state = container == null
            ? NavigationState.CreateAtRoot("session", account)
            : NavigationState.CreateInContainer("session", account, container, prefix);

        var path = state.GetCurrentPath();

        Assert.Equal(expected, path);
    }

    [Theory]
    [InlineData("docs/", null, true)] // Deeper than root
    [InlineData("docs/2024/", "docs/", true)] // Deeper than parent
    [InlineData("docs/", "docs/2024/", false)] // Shallower than child
    [InlineData("docs/", "other/", false)] // Same depth, different path
    public void IsDeeperThan_ComparesDepthCorrectly(string? thisPrefix, string? otherPrefix, bool expected)
    {
        var thisState = NavigationState.CreateInContainer("session", "account", "container", thisPrefix);
        var otherState = otherPrefix == null
            ? NavigationState.CreateAtRoot("session", "account")
            : NavigationState.CreateInContainer("session", "account", "container", otherPrefix);

        var result = thisState.IsDeeperThan(otherState);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("account", "container", "docs/", "account", "container", "docs/", true)]
    [InlineData("account", "container", "docs/", "account", "container", "other/", false)]
    [InlineData("account", "container", null, "account", "other", null, false)]
    [InlineData("account1", "container", null, "account2", "container", null, false)]
    public void IsSameLocationAs_ComparesLocationCorrectly(
        string account1, string container1, string? prefix1,
        string account2, string container2, string? prefix2,
        bool expected)
    {
        var state1 = NavigationState.CreateInContainer("session", account1, container1, prefix1);
        var state2 = NavigationState.CreateInContainer("session", account2, container2, prefix2);

        var result = state1.IsSameLocationAs(state2);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("container", null, "test query", "account/container (search: test query)")]
    [InlineData("container", "docs/", "*.txt", "account/container/docs/ (search: *.txt)")]
    public void WithSearchContext_AddsSearchToBreadcrumb(string container, string? prefix, string searchQuery, string expectedBreadcrumb)
    {
        var state = NavigationState.CreateInContainer("session", "account", container, prefix);

        var searchState = state.WithSearchContext(searchQuery);

        Assert.Equal(expectedBreadcrumb, searchState.BreadcrumbPath);
        Assert.Equal(state.ContainerName, searchState.ContainerName);
        Assert.Equal(state.BlobPrefix, searchState.BlobPrefix);
    }

    [Fact]
    public void GetPathSegments_WithComplexPath_ReturnsAllSegments()
    {
        var state = NavigationState.CreateInContainer("session", "storage-account", "documents", "projects/2024/reports/");

        var segments = state.GetPathSegments();

        Assert.Equal(new[] { "storage-account", "documents", "projects", "2024", "reports" }, segments);
    }

    [Fact]
    public void NavigateToSegment_WithComplexPath_NavigatesCorrectly()
    {
        var state = NavigationState.CreateInContainer("session", "account", "container", "docs/2024/reports/");

        var result = state.NavigateToSegment(3); // Navigate to docs/2024 level

        Assert.Equal("account", result.StorageAccountName);
        Assert.Equal("container", result.ContainerName);
        Assert.Equal("docs/2024", result.BlobPrefix);
    }

    [Fact]
    public void IsDeeperThan_WithSameDepthDifferentPaths_ReturnsFalse()
    {
        var state1 = NavigationState.CreateInContainer("session", "account", "container", "docs/");
        var state2 = NavigationState.CreateInContainer("session", "account", "container", "images/");

        var result = state1.IsDeeperThan(state2);

        Assert.False(result);
    }

    [Fact]
    public void NavigateToBlobPath_WithRootLevelFile_NavigatesToContainerRoot()
    {
        var state = NavigationState.CreateInContainer("session", "account", "container", "docs/");

        var result = state.NavigateToBlobPath("readme.txt");

        Assert.Equal("container", result.ContainerName);
        Assert.Null(result.BlobPrefix);
    }
}