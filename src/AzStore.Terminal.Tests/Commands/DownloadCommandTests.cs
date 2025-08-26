using AzStore.Core;
using AzStore.Core.Models;
using AzStore.Terminal.Commands;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AzStore.Terminal.Tests.Commands;

[Trait("Category", "Unit")]
public class DownloadCommandTests
{
    private readonly ILogger<DownloadCommand> _logger;
    private readonly IStorageService _storageService;
    private readonly DownloadCommand _command;

    public DownloadCommandTests()
    {
        _logger = Substitute.For<ILogger<DownloadCommand>>();
        _storageService = Substitute.For<IStorageService>();
        _command = new DownloadCommand(_logger, _storageService);
    }

    [Fact]
    public void Properties_ReturnExpectedValues()
    {
        Assert.Equal("download", _command.Name);
        Assert.Equal(new[] { "dl", "get" }, _command.Aliases);
        Assert.Equal("Download blob(s) from Azure storage", _command.Description);
    }

    [Fact]
    public async Task ExecuteAsync_WithInsufficientArgs_ReturnsError()
    {
        var args = new[] { "container" };

        var result = await _command.ExecuteAsync(args);

        Assert.False(result.Success);
        Assert.Contains("Usage: download", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithSingleBlob_CallsDownloadBlobWithProgress()
    {
        var args = new[] { "container", "blob.txt", "/local/path" };
        var expectedResult = new DownloadResult("blob.txt", "/local/path", 1024, true);
        
        _storageService.DownloadBlobWithProgressAsync(
            "container", 
            "blob.txt", 
            "/local/path", 
            Arg.Any<DownloadOptions>(), 
            Arg.Any<IProgress<BlobDownloadProgress>>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var result = await _command.ExecuteAsync(args);

        Assert.True(result.Success);
        Assert.Contains("Successfully downloaded", result.Message);
        
        await _storageService.Received(1).DownloadBlobWithProgressAsync(
            "container", 
            "blob.txt", 
            "/local/path", 
            Arg.Any<DownloadOptions>(), 
            Arg.Any<IProgress<BlobDownloadProgress>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithWildcardPattern_CallsDownloadBlobs()
    {
        var tempPath = Path.GetTempPath();
        var args = new[] { "container", "*.txt", tempPath };
        var expectedResults = new[]
        {
            new DownloadResult("blob1.txt", Path.Combine(tempPath, "blob1.txt"), 1024, true),
            new DownloadResult("blob2.txt", Path.Combine(tempPath, "blob2.txt"), 512, true)
        };
        
        _storageService.DownloadBlobsAsync(
            "container", 
            "*.txt", 
            tempPath, 
            Arg.Any<DownloadOptions>(), 
            Arg.Any<IProgress<DownloadProgress>>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResults);

        var result = await _command.ExecuteAsync(args);

        Assert.True(result.Success);
        Assert.Contains("Successfully downloaded 2 blobs", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithOverwriteOption_UsesOverwriteConflictResolution()
    {
        var args = new[] { "container", "blob.txt", "/local/path", "--overwrite" };
        var expectedResult = new DownloadResult("blob.txt", "/local/path/blob.txt", 1024, true);
        
        _storageService.DownloadBlobWithProgressAsync(
            Arg.Any<string>(), 
            Arg.Any<string>(), 
            Arg.Any<string>(), 
            Arg.Is<DownloadOptions>(o => o.ConflictResolution == ConflictResolution.Overwrite), 
            Arg.Any<IProgress<BlobDownloadProgress>>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var result = await _command.ExecuteAsync(args);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithSkipOption_UsesSkipConflictResolution()
    {
        var args = new[] { "container", "blob.txt", "/local/path", "--skip" };
        var expectedResult = new DownloadResult("blob.txt", "/local/path/blob.txt", 1024, true);
        
        _storageService.DownloadBlobWithProgressAsync(
            Arg.Any<string>(), 
            Arg.Any<string>(), 
            Arg.Any<string>(), 
            Arg.Is<DownloadOptions>(o => o.ConflictResolution == ConflictResolution.Skip), 
            Arg.Any<IProgress<BlobDownloadProgress>>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var result = await _command.ExecuteAsync(args);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithRenameOption_UsesRenameConflictResolution()
    {
        var args = new[] { "container", "blob.txt", "/local/path", "--rename" };
        var expectedResult = new DownloadResult("blob.txt", "/local/path/blob.txt", 1024, true);
        
        _storageService.DownloadBlobWithProgressAsync(
            Arg.Any<string>(), 
            Arg.Any<string>(), 
            Arg.Any<string>(), 
            Arg.Is<DownloadOptions>(o => o.ConflictResolution == ConflictResolution.Rename), 
            Arg.Any<IProgress<BlobDownloadProgress>>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var result = await _command.ExecuteAsync(args);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoVerifyOption_DisablesChecksumVerification()
    {
        var args = new[] { "container", "blob.txt", "/local/path", "--no-verify" };
        var expectedResult = new DownloadResult("blob.txt", "/local/path/blob.txt", 1024, true);
        
        _storageService.DownloadBlobWithProgressAsync(
            Arg.Any<string>(), 
            Arg.Any<string>(), 
            Arg.Any<string>(), 
            Arg.Is<DownloadOptions>(o => !o.VerifyChecksum), 
            Arg.Any<IProgress<BlobDownloadProgress>>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var result = await _command.ExecuteAsync(args);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithLimitOption_SetsBandwidthLimit()
    {
        var args = new[] { "container", "blob.txt", "/local/path", "--limit", "10" };
        var expectedResult = new DownloadResult("blob.txt", "/local/path/blob.txt", 1024, true);
        var expectedLimit = 10 * 1024 * 1024; // 10 MB/s in bytes/s
        
        _storageService.DownloadBlobWithProgressAsync(
            Arg.Any<string>(), 
            Arg.Any<string>(), 
            Arg.Any<string>(), 
            Arg.Is<DownloadOptions>(o => o.BandwidthLimitBytesPerSecond == expectedLimit), 
            Arg.Any<IProgress<BlobDownloadProgress>>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var result = await _command.ExecuteAsync(args);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailedDownload_ReturnsError()
    {
        var args = new[] { "container", "blob.txt", "/local/path" };
        var expectedResult = new DownloadResult("blob.txt", "/local/path/blob.txt", 0, false, "Network error");
        
        _storageService.DownloadBlobWithProgressAsync(
            Arg.Any<string>(), 
            Arg.Any<string>(), 
            Arg.Any<string>(), 
            Arg.Any<DownloadOptions>(), 
            Arg.Any<IProgress<BlobDownloadProgress>>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var result = await _command.ExecuteAsync(args);

        Assert.False(result.Success);
        Assert.Contains("Failed to download blob.txt: Network error", result.Message);
    }
}