using AzStore.Core.Models.Downloads;
using Xunit;

namespace AzStore.Core.Tests.Models;

[Trait("Category", "Unit")]
public class DownloadOptionsTests
{
    [Fact]
    public void Default_ReturnsOptionsWithExpectedDefaults()
    {
        var options = DownloadOptions.Default;

        Assert.Equal(ConflictResolution.Ask, options.ConflictResolution);
        Assert.True(options.CreateDirectories);
        Assert.True(options.VerifyChecksum);
        Assert.Null(options.BandwidthLimitBytesPerSecond);
        Assert.Equal(3, options.MaxRetryAttempts);
        Assert.Equal(300, options.TimeoutSeconds);
        Assert.True(options.EnableResumption);
        Assert.Equal(8192, options.BufferSize);
    }

    [Fact]
    public void Constructor_AcceptsAllParameters()
    {
        var options = new DownloadOptions(
            ConflictResolution: ConflictResolution.Overwrite,
            CreateDirectories: false,
            VerifyChecksum: false,
            BandwidthLimitBytesPerSecond: 1024,
            MaxRetryAttempts: 5,
            TimeoutSeconds: 600,
            EnableResumption: false,
            BufferSize: 4096);

        Assert.Equal(ConflictResolution.Overwrite, options.ConflictResolution);
        Assert.False(options.CreateDirectories);
        Assert.False(options.VerifyChecksum);
        Assert.Equal(1024, options.BandwidthLimitBytesPerSecond);
        Assert.Equal(5, options.MaxRetryAttempts);
        Assert.Equal(600, options.TimeoutSeconds);
        Assert.False(options.EnableResumption);
        Assert.Equal(4096, options.BufferSize);
    }

    [Fact]
    public void WithExpressions_AllowModificationOfDefaults()
    {
        var original = DownloadOptions.Default;
        var modified = original with { ConflictResolution = ConflictResolution.Skip };

        Assert.Equal(ConflictResolution.Ask, original.ConflictResolution);
        Assert.Equal(ConflictResolution.Skip, modified.ConflictResolution);
        Assert.Equal(original.CreateDirectories, modified.CreateDirectories);
    }
}