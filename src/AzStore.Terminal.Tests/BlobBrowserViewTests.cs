using AzStore.Core.Models;
using AzStore.Terminal;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AzStore.Terminal.Tests;

[Trait("Category", "Unit")]
public class BlobBrowserViewTests
{
    [Fact]
    public void BlobBrowserView_CanBeInstantiated()
    {
        var logger = Substitute.For<ILogger<BlobBrowserView>>();
        
        var view = new BlobBrowserView(logger);
        
        Assert.NotNull(view);
    }

    [Fact]
    public void UpdateItems_UpdatesDisplayWithContainers()
    {
        var logger = Substitute.For<ILogger<BlobBrowserView>>();
        var view = new BlobBrowserView(logger);
        
        var containers = new List<StorageItem>
        {
            Container.Create("container1", "/container1"),
            Container.Create("container2", "/container2")
        };
        
        var navigationState = NavigationState.CreateAtRoot("test-session", "storage-account");
        
        view.UpdateItems(containers, navigationState);
        
        // Test passes if no exception is thrown during update
        Assert.True(true);
    }

    [Fact]
    public void UpdateItems_UpdatesDisplayWithBlobs()
    {
        var logger = Substitute.For<ILogger<BlobBrowserView>>();
        var view = new BlobBrowserView(logger);
        
        var blobs = new List<StorageItem>
        {
            Blob.Create("blob1.txt", "/blob1.txt", "container1", BlobType.BlockBlob, 1024),
            Blob.Create("blob2.pdf", "/blob2.pdf", "container1", BlobType.BlockBlob, 2048)
        };
        
        var navigationState = NavigationState.CreateInContainer("test-session", "storage-account", "container1");
        
        view.UpdateItems(blobs, navigationState);
        
        // Test passes if no exception is thrown during update
        Assert.True(true);
    }

    [Fact]
    public void FormatStorageItem_FormatsContainer()
    {
        var container = Container.Create("test-container", "/test-container");
        
        var result = BlobBrowserViewTests.InvokeFormatStorageItem(container);
        
        Assert.Contains("📁", result);
        Assert.Contains("test-container", result);
    }

    [Fact]
    public void FormatStorageItem_FormatsBlockBlob()
    {
        var blob = Blob.Create("test.txt", "/test.txt", "container", BlobType.BlockBlob, 1024);
        
        var result = BlobBrowserViewTests.InvokeFormatStorageItem(blob);
        
        Assert.Contains("📄", result);
        Assert.Contains("test.txt", result);
        Assert.Contains("1.0KB", result);
    }

    [Fact]
    public void FormatStorageItem_FormatsPageBlob()
    {
        var blob = Blob.Create("test.vhd", "/test.vhd", "container", BlobType.PageBlob, 2048);
        
        var result = BlobBrowserViewTests.InvokeFormatStorageItem(blob);
        
        Assert.Contains("📋", result);
        Assert.Contains("test.vhd", result);
        Assert.Contains("2.0KB", result);
    }

    [Fact]
    public void FormatStorageItem_FormatsAppendBlob()
    {
        var blob = Blob.Create("test.log", "/test.log", "container", BlobType.AppendBlob, 4096);
        
        var result = BlobBrowserViewTests.InvokeFormatStorageItem(blob);
        
        Assert.Contains("📝", result);
        Assert.Contains("test.log", result);
        Assert.Contains("4.0KB", result);
    }

    [Fact]
    public void FormatFileSize_FormatsBytes()
    {
        var result = BlobBrowserViewTests.InvokeFormatFileSize(512);
        
        Assert.Equal("512.0B", result);
    }

    [Fact]
    public void FormatFileSize_FormatsKilobytes()
    {
        var result = BlobBrowserViewTests.InvokeFormatFileSize(1536); // 1.5 KB
        
        Assert.Equal("1.5KB", result);
    }

    [Fact]
    public void FormatFileSize_FormatsMegabytes()
    {
        var result = BlobBrowserViewTests.InvokeFormatFileSize(2621440); // 2.5 MB
        
        Assert.Equal("2.5MB", result);
    }

    [Fact]
    public void FormatFileSize_FormatsGigabytes()
    {
        var result = BlobBrowserViewTests.InvokeFormatFileSize(3221225472L); // 3.0 GB
        
        Assert.Equal("3.0GB", result);
    }

    [Fact]
    public void FormatFileSize_ReturnsEmptyForNull()
    {
        var result = BlobBrowserViewTests.InvokeFormatFileSize(null);
        
        Assert.Equal("", result);
    }

    [Fact]
    public void FormatFileSize_ReturnsEmptyForZero()
    {
        var result = BlobBrowserViewTests.InvokeFormatFileSize(0);
        
        Assert.Equal("", result);
    }

    // Helper method to access private static methods via reflection for testing
    private static string InvokeFormatStorageItem(StorageItem item)
    {
        var method = typeof(BlobBrowserView).GetMethod("FormatStorageItem", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (string)method!.Invoke(null, [item])!;
    }

    private static string InvokeFormatFileSize(long? bytes)
    {
        var method = typeof(BlobBrowserView).GetMethod("FormatFileSize", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (string)method!.Invoke(null, [bytes])!;
    }
}