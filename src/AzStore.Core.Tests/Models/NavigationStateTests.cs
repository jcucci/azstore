using AzStore.Core.Models;
using Xunit;

namespace AzStore.Core.Tests.Models;

public class NavigationStateTests
{
    [Fact]
    public void CreateAtRoot_ShouldReturnNavigationStateAtRoot()
    {
        var sessionName = "test-session";
        var storageAccountName = "mystorage";

        var state = NavigationState.CreateAtRoot(sessionName, storageAccountName);

        Assert.Equal(sessionName, state.SessionName);
        Assert.Equal(storageAccountName, state.StorageAccountName);
        Assert.Null(state.ContainerName);
        Assert.Null(state.BlobPrefix);
        Assert.Equal(storageAccountName, state.BreadcrumbPath);
        Assert.Equal(0, state.SelectedIndex);
    }

    [Fact]
    public void CreateInContainer_WithoutBlobPrefix_ShouldReturnNavigationStateInContainer()
    {
        var sessionName = "test-session";
        var storageAccountName = "mystorage";
        var containerName = "mycontainer";

        var state = NavigationState.CreateInContainer(sessionName, storageAccountName, containerName);

        Assert.Equal(sessionName, state.SessionName);
        Assert.Equal(storageAccountName, state.StorageAccountName);
        Assert.Equal(containerName, state.ContainerName);
        Assert.Null(state.BlobPrefix);
        Assert.Equal("mystorage/mycontainer", state.BreadcrumbPath);
        Assert.Equal(0, state.SelectedIndex);
    }

    [Fact]
    public void CreateInContainer_WithBlobPrefix_ShouldReturnNavigationStateWithPrefix()
    {
        var sessionName = "test-session";
        var storageAccountName = "mystorage";
        var containerName = "mycontainer";
        var blobPrefix = "documents/2024";

        var state = NavigationState.CreateInContainer(sessionName, storageAccountName, containerName, blobPrefix);

        Assert.Equal(sessionName, state.SessionName);
        Assert.Equal(storageAccountName, state.StorageAccountName);
        Assert.Equal(containerName, state.ContainerName);
        Assert.Equal(blobPrefix, state.BlobPrefix);
        Assert.Equal("mystorage/mycontainer/documents/2024", state.BreadcrumbPath);
        Assert.Equal(0, state.SelectedIndex);
    }

    [Fact]
    public void WithSelectedIndex_ShouldReturnStateWithUpdatedIndex()
    {
        var state = NavigationState.CreateAtRoot("session", "storage");
        var newIndex = 5;

        var updatedState = state.WithSelectedIndex(newIndex);

        Assert.Equal(newIndex, updatedState.SelectedIndex);
        Assert.Equal(state.SessionName, updatedState.SessionName);
        Assert.Equal(state.StorageAccountName, updatedState.StorageAccountName);
    }

    [Fact]
    public void WithSelectedIndex_WithNegativeIndex_ShouldReturnZero()
    {
        var state = NavigationState.CreateAtRoot("session", "storage");

        var updatedState = state.WithSelectedIndex(-1);

        Assert.Equal(0, updatedState.SelectedIndex);
    }

    [Fact]
    public void NavigateInto_FromRoot_ShouldNavigateToContainer()
    {
        var state = NavigationState.CreateAtRoot("session", "storage");
        var containerName = "mycontainer";

        var navigatedState = state.NavigateInto(containerName);

        Assert.Equal(containerName, navigatedState.ContainerName);
        Assert.Null(navigatedState.BlobPrefix);
        Assert.Equal("storage/mycontainer", navigatedState.BreadcrumbPath);
    }

    [Fact]
    public void NavigateInto_FromContainer_ShouldNavigateToPrefix()
    {
        var state = NavigationState.CreateInContainer("session", "storage", "container");
        var blobPrefix = "documents";

        var navigatedState = state.NavigateInto(blobPrefix: blobPrefix);

        Assert.Equal("container", navigatedState.ContainerName);
        Assert.Equal(blobPrefix, navigatedState.BlobPrefix);
        Assert.Equal("storage/container/documents", navigatedState.BreadcrumbPath);
    }

    [Fact]
    public void NavigateInto_FromPrefixToDeeper_ShouldAppendToPrefix()
    {
        var state = NavigationState.CreateInContainer("session", "storage", "container", "docs");
        var deeperPrefix = "2024";

        var navigatedState = state.NavigateInto(blobPrefix: deeperPrefix);

        Assert.Equal("docs/2024", navigatedState.BlobPrefix);
        Assert.Equal("storage/container/docs/2024", navigatedState.BreadcrumbPath);
    }

    [Fact]
    public void NavigateUp_FromPrefix_ShouldNavigateToParentPrefix()
    {
        var state = NavigationState.CreateInContainer("session", "storage", "container", "docs/2024");

        var navigatedState = state.NavigateUp();

        Assert.Equal("docs", navigatedState.BlobPrefix);
        Assert.Equal("storage/container/docs", navigatedState.BreadcrumbPath);
    }

    [Fact]
    public void NavigateUp_FromSinglePrefix_ShouldNavigateToContainer()
    {
        var state = NavigationState.CreateInContainer("session", "storage", "container", "docs");

        var navigatedState = state.NavigateUp();

        Assert.Equal("container", navigatedState.ContainerName);
        Assert.Null(navigatedState.BlobPrefix);
        Assert.Equal("storage/container", navigatedState.BreadcrumbPath);
    }

    [Fact]
    public void NavigateUp_FromContainer_ShouldNavigateToRoot()
    {
        var state = NavigationState.CreateInContainer("session", "storage", "container");

        var navigatedState = state.NavigateUp();

        Assert.Null(navigatedState.ContainerName);
        Assert.Equal("storage", navigatedState.BreadcrumbPath);
    }

    [Fact]
    public void NavigateUp_FromRoot_ShouldReturnSameState()
    {
        var state = NavigationState.CreateAtRoot("session", "storage");

        var navigatedState = state.NavigateUp();

        Assert.Equal(state, navigatedState);
    }

    [Theory]
    [InlineData(null, null, NavigationLevel.StorageAccount)]
    [InlineData("container", null, NavigationLevel.Container)]
    [InlineData("container", "prefix", NavigationLevel.BlobPrefix)]
    public void GetLevel_ShouldReturnCorrectLevel(string? containerName, string? blobPrefix, NavigationLevel expectedLevel)
    {
        var state = new NavigationState("session", "storage", containerName, blobPrefix, "breadcrumb", 0);

        var level = state.GetLevel();

        Assert.Equal(expectedLevel, level);
    }

    [Theory]
    [InlineData(null, null, false)]
    [InlineData("container", null, true)]
    [InlineData("container", "prefix", true)]
    public void CanNavigateUp_ShouldReturnCorrectValue(string? containerName, string? blobPrefix, bool expectedCanNavigateUp)
    {
        var state = new NavigationState("session", "storage", containerName, blobPrefix, "breadcrumb", 0);

        var canNavigateUp = state.CanNavigateUp();

        Assert.Equal(expectedCanNavigateUp, canNavigateUp);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        var state = NavigationState.CreateInContainer("session", "storage", "container");

        var result = state.ToString();

        Assert.Equal("Container: storage/container (item 0)", result);
    }
}