using Xunit;
using AzStore.Core.Models.Paging;

namespace AzStore.Core.Tests;

/// <summary>
/// Integration tests for AzureStorageService that verify behavior with actual Azure Storage.
/// These tests will only pass meaningful assertions when Azure CLI is authenticated and storage accounts are accessible.
/// </summary>
[Trait("Category", "Integration")]
public class AzureStorageServiceIntegrationTests
{
    [Fact]
    public void GetConnectionStatus_WhenNotConnected_ReturnsFalse()
    {
        var service = AzureStorageServiceFixture.CreateWithMockDependencies();

        var status = service.GetConnectionStatus();

        Assert.False(status);
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