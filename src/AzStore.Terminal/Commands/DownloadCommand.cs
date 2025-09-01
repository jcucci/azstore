using AzStore.Core.Models.Downloads;
using AzStore.Core.Models.Session;
using AzStore.Core.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace AzStore.Terminal.Commands;

public class DownloadCommand : ICommand
{
    private readonly ILogger<DownloadCommand> _logger;
    private readonly IStorageService _storageService;
    private readonly IPathService _pathService;
    private readonly ISessionManager? _sessionManager;

    public string Name => "download";
    public string[] Aliases => ["dl", "get"];
    public string Description => "Download blob(s) from Azure storage";

    public DownloadCommand(ILogger<DownloadCommand> logger, IStorageService storageService, IPathService pathService, ISessionManager? sessionManager = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        _sessionManager = sessionManager;
    }

    public async Task<CommandResult> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (args.Length < 2)
        {
            return CommandResult.Error("Usage: download <container> <blob-name-or-pattern> [local-path] [options]\n" +
                                     "Options:\n" +
                                     "  --overwrite     Overwrite existing files\n" +
                                     "  --skip          Skip existing files\n" +
                                     "  --rename        Rename conflicting files\n" +
                                     "  --no-verify     Skip checksum verification\n" +
                                     "  --limit <rate>  Bandwidth limit in MB/s");
        }

        var containerName = args[0];
        var blobPattern = args[1];
        var localPath = args.Length > 2 ? args[2] : ".";

        try
        {
            var options = ParseOptions(args.Skip(3));

            // Check if this is a pattern-based download or single blob
            if (blobPattern.Contains('*') || blobPattern.Contains('?'))
            {
                return await DownloadMultipleBlobsAsync(containerName, blobPattern, localPath, options, cancellationToken);
            }
            else
            {
                return await DownloadSingleBlobAsync(containerName, blobPattern, localPath, options, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute download command");
            return CommandResult.Error($"Download failed: {ex.Message}");
        }
    }

    private async Task<CommandResult> DownloadSingleBlobAsync(
        string containerName,
        string blobName,
        string localPath,
        DownloadOptions options,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Downloading {BlobName} from {ContainerName}", blobName, containerName);

        var targetPath = CalculateTargetPath(containerName, blobName, localPath);

        var progress = new Progress<BlobDownloadProgress>(DisplayProgress);

        var result = await _storageService.DownloadBlobWithProgressAsync(
            containerName: containerName,
            blobName: blobName,
            localFilePath: targetPath,
            options: options,
            progress: progress,
            cancellationToken: cancellationToken);

        if (result.Success)
        {
            Console.WriteLine();
            return CommandResult.Ok($"Successfully downloaded {blobName} ({result.BytesDownloaded:N0} bytes) to {result.LocalFilePath}");
        }
        else
        {
            Console.WriteLine();
            return CommandResult.Error($"Failed to download {blobName}: {result.Error}");
        }
    }

    private async Task<CommandResult> DownloadMultipleBlobsAsync(
        string containerName,
        string blobPattern,
        string localDirectory,
        DownloadOptions options,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Downloading blobs matching pattern {Pattern} from {ContainerName}", blobPattern, containerName);

        if (!Directory.Exists(localDirectory))
        {
            Directory.CreateDirectory(localDirectory);
        }

        var overallProgress = new Progress<DownloadProgress>(DisplayOverallProgress);

        var results = await _storageService.DownloadBlobsAsync(
            containerName: containerName,
            blobPattern: blobPattern,
            localDirectoryPath: localDirectory,
            options: options,
            progress: overallProgress,
            cancellationToken: cancellationToken);

        Console.WriteLine();

        var successful = results.Count(r => r.Success);
        var total = results.Count;
        var totalBytes = results.Sum(r => r.BytesDownloaded);

        if (successful == total)
        {
            return CommandResult.Ok($"Successfully downloaded {successful} blobs ({totalBytes:N0} bytes total)");
        }
        else
        {
            var failed = total - successful;
            var errors = results.Where(r => !r.Success).Select(r => $"{r.BlobName}: {r.Error}");
            return CommandResult.Error($"Downloaded {successful}/{total} blobs successfully. Failed: {failed}\n" +
                                     string.Join("\n", errors));
        }
    }

    private static DownloadOptions ParseOptions(IEnumerable<string> args)
    {
        var options = DownloadOptions.Default;
        var argList = args.ToList();

        for (int i = 0; i < argList.Count; i++)
        {
            switch (argList[i].ToLowerInvariant())
            {
                case "--overwrite":
                    options = options with { ConflictResolution = ConflictResolution.Overwrite };
                    break;
                case "--skip":
                    options = options with { ConflictResolution = ConflictResolution.Skip };
                    break;
                case "--rename":
                    options = options with { ConflictResolution = ConflictResolution.Rename };
                    break;
                case "--no-verify":
                    options = options with { VerifyChecksum = false };
                    break;
                case "--limit":
                    if (i + 1 < argList.Count && double.TryParse(argList[i + 1], out var limitMB))
                    {
                        var limitBytesPerSecond = (long)(limitMB * 1024 * 1024);
                        options = options with { BandwidthLimitBytesPerSecond = limitBytesPerSecond };
                        i++;
                    }
                    break;
            }
        }

        return options;
    }

    private static void DisplayProgress(BlobDownloadProgress progress)
    {
        var progressBar = CreateProgressBar(progress.ProgressPercentage, 40);
        var speed = TerminalUtils.FormatBytes(progress.BytesPerSecond);
        var downloaded = TerminalUtils.FormatBytes(progress.DownloadedBytes);
        var total = TerminalUtils.FormatBytes(progress.TotalBytes);
        var eta = progress.EstimatedTimeRemainingSeconds.HasValue
            ? TimeSpan.FromSeconds(progress.EstimatedTimeRemainingSeconds.Value).ToString(@"mm\:ss")
            : "--:--";

        var retryText = progress.RetryAttempt > 0 ? $" (retry {progress.RetryAttempt})" : "";
        var stageText = progress.Stage != DownloadStage.Downloading ? $" [{progress.Stage}]" : "";

        Console.Write($"\r{progressBar} {progress.ProgressPercentage:F1}% {downloaded}/{total} {speed}/s ETA: {eta}{retryText}{stageText}");
    }

    private static void DisplayOverallProgress(DownloadProgress progress)
    {
        var percentage = progress.TotalBlobs > 0
            ? (double)progress.CompletedBlobs / progress.TotalBlobs * 100
            : 0;

        var progressBar = CreateProgressBar(percentage, 40);
        var totalBytes = TerminalUtils.FormatBytes(progress.TotalBytesDownloaded);

        Console.Write($"\r{progressBar} {percentage:F1}% ({progress.CompletedBlobs}/{progress.TotalBlobs} files) {totalBytes} - {progress.CurrentBlobName}");
    }

    private static string CreateProgressBar(double percentage, int width)
    {
        var filled = (int)(percentage / 100 * width);
        var empty = width - filled;
        return $"[{new string('=', filled)}{new string('-', empty)}]";
    }


    /// <summary>
    /// Calculates the target path for a blob download, using session-aware paths when appropriate.
    /// </summary>
    private string CalculateTargetPath(string containerName, string blobName, string userSpecifiedPath)
    {
        if (userSpecifiedPath == "." || Directory.Exists(userSpecifiedPath) || userSpecifiedPath.EndsWith(Path.DirectorySeparatorChar))
        {
            var session = _sessionManager?.GetActiveSession();

            if (session != null)
            {
                return _pathService.CalculateBlobDownloadPath(session, containerName, blobName);
            }
            else
            {
                var localBlobPath = _pathService.PreserveVirtualDirectoryStructure(blobName);
                return Path.Combine(userSpecifiedPath, localBlobPath);
            }
        }
        else
        {
            return userSpecifiedPath;
        }
    }
}