using AzStore.Core.Models;
using AzStore.Terminal;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Diagnostics;
using Xunit;

namespace AzStore.Terminal.Tests;

[Trait("Category", "Performance")]
public class PerformanceTests
{
    [Fact]
    public void BlobBrowserView_HandlesLargeItemLists_WithinPerformanceThreshold()
    {
        var logger = Substitute.For<ILogger<BlobBrowserView>>();
        var view = new BlobBrowserView(logger);
        
        var items = GenerateLargeItemList(1000);
        var navigationState = NavigationState.CreateInContainer("test-session", "storage-account", "container");
        
        var stopwatch = Stopwatch.StartNew();
        
        view.UpdateItems(items, navigationState);
        
        stopwatch.Stop();
        
        // Performance threshold: Should complete in under 2 seconds for 1000 items (relaxed for Terminal.Gui)
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, 
            $"Performance test failed. Elapsed: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void BlobBrowserView_HandlesVeryLargeItemLists_WithinExtendedThreshold()
    {
        var logger = Substitute.For<ILogger<BlobBrowserView>>();
        var view = new BlobBrowserView(logger);
        
        var items = GenerateLargeItemList(5000);
        var navigationState = NavigationState.CreateInContainer("test-session", "storage-account", "container");
        
        var stopwatch = Stopwatch.StartNew();
        
        view.UpdateItems(items, navigationState);
        
        stopwatch.Stop();
        
        // Extended threshold: Should complete in under 15 seconds for 5000 items (relaxed for Terminal.Gui)
        Assert.True(stopwatch.ElapsedMilliseconds < 15000, 
            $"Extended performance test failed. Elapsed: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void FormatStorageItem_PerformanceWithLargeDataSet()
    {
        var items = GenerateLargeItemList(1000);
        
        var stopwatch = Stopwatch.StartNew();
        
        foreach (var item in items)
        {
            _ = InvokeFormatStorageItem(item);
        }
        
        stopwatch.Stop();
        
        // Should format 1000 items in under 100ms
        Assert.True(stopwatch.ElapsedMilliseconds < 100, 
            $"Format performance test failed. Elapsed: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void FormatFileSize_PerformanceWithVariousSizes()
    {
        var sizes = GenerateFileSizes(1000);
        
        var stopwatch = Stopwatch.StartNew();
        
        foreach (var size in sizes)
        {
            _ = InvokeFormatFileSize(size);
        }
        
        stopwatch.Stop();
        
        // Should format 1000 file sizes in under 50ms
        Assert.True(stopwatch.ElapsedMilliseconds < 50, 
            $"File size format performance test failed. Elapsed: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void BlobBrowserView_MemoryUsage_WithinReasonableLimits()
    {
        var logger = Substitute.For<ILogger<BlobBrowserView>>();
        var view = new BlobBrowserView(logger);
        
        var beforeMemory = GC.GetTotalMemory(true);
        
        var items = GenerateLargeItemList(1000);
        var navigationState = NavigationState.CreateInContainer("test-session", "storage-account", "container");
        
        view.UpdateItems(items, navigationState);
        
        var afterMemory = GC.GetTotalMemory(false);
        var memoryUsed = afterMemory - beforeMemory;
        
        // Memory usage should be reasonable (less than 50MB for 1000 items with Terminal.Gui overhead)
        Assert.True(memoryUsed < 50 * 1024 * 1024, 
            $"Memory usage test failed. Used: {memoryUsed / 1024}KB");
    }

    [Fact]
    public void BlobBrowserView_MultipleUpdates_Performance()
    {
        var logger = Substitute.For<ILogger<BlobBrowserView>>();
        var view = new BlobBrowserView(logger);
        
        var items = GenerateLargeItemList(500);
        var navigationState = NavigationState.CreateInContainer("test-session", "storage-account", "container");
        
        var stopwatch = Stopwatch.StartNew();
        
        // Simulate multiple rapid updates (like paging through results)
        for (int i = 0; i < 10; i++)
        {
            navigationState = navigationState.WithSelectedIndex(i * 10);
            view.UpdateItems(items, navigationState);
        }
        
        stopwatch.Stop();
        
        // Multiple updates should complete within 3 seconds for Terminal.Gui
        Assert.True(stopwatch.ElapsedMilliseconds < 3000, 
            $"Multiple updates performance test failed. Elapsed: {stopwatch.ElapsedMilliseconds}ms");
    }

    private static List<StorageItem> GenerateLargeItemList(int count)
    {
        var items = new List<StorageItem>(count);
        var random = new Random(42); // Deterministic seed for consistent tests
        
        for (int i = 0; i < count; i++)
        {
            if (i % 10 == 0) // 10% containers
            {
                items.Add(Container.Create($"container-{i:D6}", $"/container-{i:D6}"));
            }
            else // 90% blobs
            {
                var blobTypes = new[] { BlobType.BlockBlob, BlobType.PageBlob, BlobType.AppendBlob };
                var extensions = new[] { ".txt", ".pdf", ".jpg", ".log", ".json", ".xml", ".csv" };
                
                items.Add(Blob.Create(
                    $"file-{i:D6}{extensions[i % extensions.Length]}",
                    $"/file-{i:D6}{extensions[i % extensions.Length]}",
                    "test-container",
                    blobTypes[i % blobTypes.Length],
                    random.Next(1024, 100 * 1024 * 1024))); // 1KB to 100MB
            }
        }
        
        return items;
    }

    private static List<long> GenerateFileSizes(int count)
    {
        var sizes = new List<long>(count);
        var random = new Random(42); // Deterministic seed
        
        for (int i = 0; i < count; i++)
        {
            // Generate various file sizes from bytes to terabytes
            var power = random.Next(0, 13); // 0 to 12 (up to TB range)
            var multiplier = random.NextDouble() * 10;
            sizes.Add((long)(multiplier * Math.Pow(1024, power / 3.0)));
        }
        
        return sizes;
    }

    // Helper methods to access private static methods via reflection
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