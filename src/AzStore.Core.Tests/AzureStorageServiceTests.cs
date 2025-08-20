using Xunit;

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
        
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var container in service.ListContainersAsync())
            {
                // Should throw before yielding any items
            }
        });
        
        Assert.Contains("not connected", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListBlobsAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        var service = AzureStorageServiceFixture.CreateWithMockDependencies();
        
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var blob in service.ListBlobsAsync("test-container"))
            {
                // Should throw before yielding any items
            }
        });
        
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

}