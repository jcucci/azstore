using AzStore.Core.Models.Downloads;
using Xunit;

namespace AzStore.Core.Tests.Models;

[Trait("Category", "Unit")]
public class BlobDownloadProgressTests
{
    [Fact]
    public void Starting_CreatesProgressWithInitialValues()
    {
        var progress = BlobDownloadProgress.Starting("test.txt", 1024);

        Assert.Equal("test.txt", progress.BlobName);
        Assert.Equal(1024, progress.TotalBytes);
        Assert.Equal(0, progress.DownloadedBytes);
        Assert.Equal(0, progress.ProgressPercentage);
        Assert.Equal(0, progress.BytesPerSecond);
        Assert.Null(progress.EstimatedTimeRemainingSeconds);
        Assert.Equal(0, progress.RetryAttempt);
        Assert.Equal(DownloadStage.Starting, progress.Stage);
    }

    [Fact]
    public void Update_CalculatesProgressPercentageCorrectly()
    {
        var progress = BlobDownloadProgress.Update("test.txt", 1000, 250, 1024);

        Assert.Equal("test.txt", progress.BlobName);
        Assert.Equal(1000, progress.TotalBytes);
        Assert.Equal(250, progress.DownloadedBytes);
        Assert.Equal(25.0, progress.ProgressPercentage);
        Assert.Equal(1024, progress.BytesPerSecond);
        Assert.Equal(DownloadStage.Downloading, progress.Stage);
    }

    [Fact]
    public void Update_CalculatesEstimatedTimeRemaining()
    {
        var progress = BlobDownloadProgress.Update("test.txt", 1000, 250, 100);

        var expectedEta = (1000 - 250) / 100; // (remaining bytes) / (bytes per second)
        Assert.Equal(expectedEta, progress.EstimatedTimeRemainingSeconds);
    }

    [Fact]
    public void Update_WithZeroBytesPerSecond_SetsEtaToNull()
    {
        var progress = BlobDownloadProgress.Update("test.txt", 1000, 250, 0);

        Assert.Null(progress.EstimatedTimeRemainingSeconds);
    }

    [Fact]
    public void Update_WithZeroTotalBytes_SetsProgressToZero()
    {
        var progress = BlobDownloadProgress.Update("test.txt", 0, 0, 100);

        Assert.Equal(0, progress.ProgressPercentage);
    }

    [Fact]
    public void Completed_CreatesProgressWith100Percent()
    {
        var progress = BlobDownloadProgress.Completed("test.txt", 1024);

        Assert.Equal("test.txt", progress.BlobName);
        Assert.Equal(1024, progress.TotalBytes);
        Assert.Equal(1024, progress.DownloadedBytes);
        Assert.Equal(100, progress.ProgressPercentage);
        Assert.Equal(0, progress.BytesPerSecond);
        Assert.Equal(0, progress.EstimatedTimeRemainingSeconds);
        Assert.Equal(0, progress.RetryAttempt);
        Assert.Equal(DownloadStage.Completed, progress.Stage);
    }

    [Fact]
    public void Failed_CreatesProgressWithFailureStage()
    {
        var progress = BlobDownloadProgress.Failed("test.txt", 1000, 250, 2);

        Assert.Equal("test.txt", progress.BlobName);
        Assert.Equal(1000, progress.TotalBytes);
        Assert.Equal(250, progress.DownloadedBytes);
        Assert.Equal(25.0, progress.ProgressPercentage);
        Assert.Equal(2, progress.RetryAttempt);
        Assert.Equal(DownloadStage.Failed, progress.Stage);
    }
}