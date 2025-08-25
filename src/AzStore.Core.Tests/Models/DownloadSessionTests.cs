using AzStore.Core.Models;
using Xunit;

namespace AzStore.Core.Tests.Models;

[Trait("Category", "Unit")]
public class DownloadSessionTests
{
    [Fact]
    public void Create_InitializesWithCorrectValues()
    {
        var session = DownloadSession.Create("test.txt", "container", "/path/test.txt", 1024, "checksum");

        Assert.Equal("test.txt", session.BlobName);
        Assert.Equal("container", session.ContainerName);
        Assert.Equal("/path/test.txt", session.LocalFilePath);
        Assert.Equal(1024, session.TotalBytes);
        Assert.Equal(0, session.DownloadedBytes);
        Assert.Equal(0, session.StartOffset);
        Assert.Equal(0, session.RetryCount);
        Assert.Equal("checksum", session.ExpectedChecksum);
        Assert.False(session.IsCompleted);
        Assert.True(session.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.True(session.LastUpdatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Resume_InitializesWithExistingBytes()
    {
        var session = DownloadSession.Resume("test.txt", "container", "/path/test.txt", 1024, 512, "checksum");

        Assert.Equal("test.txt", session.BlobName);
        Assert.Equal("container", session.ContainerName);
        Assert.Equal("/path/test.txt", session.LocalFilePath);
        Assert.Equal(1024, session.TotalBytes);
        Assert.Equal(512, session.DownloadedBytes);
        Assert.Equal(512, session.StartOffset);
        Assert.Equal(0, session.RetryCount);
        Assert.Equal("checksum", session.ExpectedChecksum);
        Assert.False(session.IsCompleted);
    }

    [Fact]
    public void UpdateProgress_UpdatesBytesAndTimestamp()
    {
        var session = DownloadSession.Create("test.txt", "container", "/path/test.txt", 1024);
        var originalTimestamp = session.LastUpdatedAt;

        Thread.Sleep(10); // Ensure timestamp difference
        var updated = session.UpdateProgress(256);

        Assert.Equal(256, updated.DownloadedBytes);
        Assert.True(updated.LastUpdatedAt > originalTimestamp);
        Assert.Equal(session.BlobName, updated.BlobName); // Other properties unchanged
    }

    [Fact]
    public void IncrementRetryCount_IncrementsCountAndTimestamp()
    {
        var session = DownloadSession.Create("test.txt", "container", "/path/test.txt", 1024);
        var originalTimestamp = session.LastUpdatedAt;

        Thread.Sleep(10);
        var updated = session.IncrementRetryCount();

        Assert.Equal(1, updated.RetryCount);
        Assert.True(updated.LastUpdatedAt > originalTimestamp);
    }

    [Fact]
    public void MarkCompleted_SetsCompletedStateCorrectly()
    {
        var session = DownloadSession.Create("test.txt", "container", "/path/test.txt", 1024);
        var completed = session.MarkCompleted();

        Assert.True(completed.IsCompleted);
        Assert.Equal(1024, completed.DownloadedBytes);
        Assert.True(completed.LastUpdatedAt >= session.LastUpdatedAt);
    }

    [Fact]
    public void ProgressPercentage_CalculatesCorrectly()
    {
        var session = DownloadSession.Create("test.txt", "container", "/path/test.txt", 1000);
        var updated = session.UpdateProgress(250);

        Assert.Equal(25.0, updated.ProgressPercentage);
    }

    [Fact]
    public void ProgressPercentage_WithZeroTotalBytes_ReturnsZero()
    {
        var session = DownloadSession.Create("test.txt", "container", "/path/test.txt", 0);

        Assert.Equal(0.0, session.ProgressPercentage);
    }

    [Fact]
    public void RemainingBytes_CalculatesCorrectly()
    {
        var session = DownloadSession.Create("test.txt", "container", "/path/test.txt", 1000);
        var updated = session.UpdateProgress(300);

        Assert.Equal(700, updated.RemainingBytes);
    }

    [Fact]
    public void CanResume_ReturnsTrueForPartialDownload()
    {
        var session = DownloadSession.Create("test.txt", "container", "/path/test.txt", 1000);
        var partial = session.UpdateProgress(500);

        Assert.True(partial.CanResume);
    }

    [Fact]
    public void CanResume_ReturnsFalseForCompletedDownload()
    {
        var session = DownloadSession.Create("test.txt", "container", "/path/test.txt", 1000);
        var completed = session.MarkCompleted();

        Assert.False(completed.CanResume);
    }

    [Fact]
    public void CanResume_ReturnsFalseForZeroProgress()
    {
        var session = DownloadSession.Create("test.txt", "container", "/path/test.txt", 1000);

        Assert.False(session.CanResume);
    }

    [Fact]
    public void CanResume_ReturnsFalseForFullProgress()
    {
        var session = DownloadSession.Create("test.txt", "container", "/path/test.txt", 1000);
        var full = session.UpdateProgress(1000);

        Assert.False(full.CanResume);
    }
}