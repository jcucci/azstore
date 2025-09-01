using Xunit;
using AzStore.Core.Models;
using AzStore.Core.Models.Paging;

namespace AzStore.Core.Tests;

[Trait("Category", "Unit")]
public class AzureStorageServiceTests
{
    [Fact]
    public void AzureStorageService_CanBeInstantiated()
    {
        var service = AzureStorageServiceFixture.CreateWithMockDependencies();

        AzureStorageServiceAssertions.InstanceCreated(service);
    }

    [Fact]
    public void GetConnectionStatus_WhenNotConnected_ReturnsFalse()
    {
        var service = AzureStorageServiceFixture.CreateWithMockDependencies();

        var status = service.GetConnectionStatus();

        AzureStorageServiceAssertions.ConnectionStatusIsFalse(status);
    }

    [Fact]
    public async Task ListContainersAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        var service = AzureStorageServiceFixture.CreateWithMockDependencies();
        var pageRequest = PageRequest.FirstPage();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ListContainersAsync(pageRequest));

        Assert.Contains("not connected", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListBlobsAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        var service = AzureStorageServiceFixture.CreateWithMockDependencies();
        var pageRequest = PageRequest.FirstPage();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ListBlobsAsync("test-container", null, pageRequest));

        Assert.Contains("not connected", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetBlobAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        var service = AzureStorageServiceFixture.CreateWithMockDependencies();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetBlobAsync("test-container", "test-blob"));

        Assert.Contains("not connected", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DownloadBlobAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        var service = AzureStorageServiceFixture.CreateWithMockDependencies();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DownloadBlobAsync("test-container", "test-blob", "/tmp/test-file"));

        Assert.Contains("not connected", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetContainerPropertiesAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        var service = AzureStorageServiceFixture.CreateWithMockDependencies();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetContainerPropertiesAsync("test-container"));

        Assert.Contains("not connected", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateContainerAccessAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        var service = AzureStorageServiceFixture.CreateWithMockDependencies();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ValidateContainerAccessAsync("test-container"));

        Assert.Contains("not connected", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PageRequest_FirstPage_CreatesRequestWithDefaultPageSize()
    {
        var pageRequest = PageRequest.FirstPage();

        Assert.Equal(100, pageRequest.PageSize);
        Assert.Null(pageRequest.ContinuationToken);
    }

    [Fact]
    public void PageRequest_FirstPageWithSize_CreatesRequestWithSpecifiedPageSize()
    {
        var pageRequest = PageRequest.FirstPage(50);

        Assert.Equal(50, pageRequest.PageSize);
        Assert.Null(pageRequest.ContinuationToken);
    }

    [Fact]
    public void PageRequest_NextPage_CreatesRequestWithContinuationToken()
    {
        var initialPage = PageRequest.FirstPage(25);
        var nextPage = initialPage.NextPage("token123");

        Assert.Equal(25, nextPage.PageSize);
        Assert.Equal("token123", nextPage.ContinuationToken);
    }

    [Fact]
    public void PagedResult_HasMore_ReturnsTrueWhenContinuationTokenExists()
    {
        var items = new List<string> { "item1", "item2" };
        var result = new PagedResult<string>(items, "token123");

        Assert.True(result.HasMore);
        Assert.Equal(2, result.Count);
        Assert.Equal("token123", result.ContinuationToken);
    }

    [Fact]
    public void PagedResult_HasMore_ReturnsFalseWhenNoContinuationToken()
    {
        var items = new List<string> { "item1", "item2" };
        var result = new PagedResult<string>(items, null);

        Assert.False(result.HasMore);
        Assert.Equal(2, result.Count);
        Assert.Null(result.ContinuationToken);
    }

    [Fact]
    public void PagedResult_Empty_CreatesEmptyResultWithNoToken()
    {
        var result = PagedResult<string>.Empty();

        Assert.False(result.HasMore);
        Assert.Equal(0, result.Count);
        Assert.Null(result.ContinuationToken);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task BrowseBlobsAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        var service = AzureStorageServiceFixture.CreateWithMockDependencies();
        var pageRequest = PageRequest.FirstPage();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BrowseBlobsAsync("test-container", null, pageRequest));

        Assert.Contains("not connected", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListVirtualDirectoriesAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        var service = AzureStorageServiceFixture.CreateWithMockDependencies();
        var pageRequest = PageRequest.FirstPage();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ListVirtualDirectoriesAsync("test-container", null, pageRequest));

        Assert.Contains("not connected", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NavigateToPathAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        var service = AzureStorageServiceFixture.CreateWithMockDependencies();
        var pageRequest = PageRequest.FirstPage();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.NavigateToPathAsync("test-container", "documents/", pageRequest));

        Assert.Contains("not connected", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchBlobsAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        var service = AzureStorageServiceFixture.CreateWithMockDependencies();
        var pageRequest = PageRequest.FirstPage();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SearchBlobsAsync("test-container", "*.txt", null, pageRequest));

        Assert.Contains("not connected", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

}