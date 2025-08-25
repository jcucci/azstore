namespace AzStore.Core.Models;

/// <summary>
/// Represents the state of a resumable download session.
/// </summary>
/// <param name="BlobName">The name of the blob being downloaded.</param>
/// <param name="ContainerName">The name of the container containing the blob.</param>
/// <param name="LocalFilePath">The local file path where the blob is being downloaded.</param>
/// <param name="TotalBytes">The total size of the blob in bytes.</param>
/// <param name="DownloadedBytes">The number of bytes already downloaded.</param>
/// <param name="StartOffset">The byte offset to start/resume downloading from.</param>
/// <param name="RetryCount">The number of retry attempts made.</param>
/// <param name="ExpectedChecksum">Expected MD5 hash of the complete blob, if available.</param>
/// <param name="CreatedAt">When this download session was created.</param>
/// <param name="LastUpdatedAt">When this download session was last updated.</param>
/// <param name="IsCompleted">Whether the download has been completed successfully.</param>
public record DownloadSession(
    string BlobName,
    string ContainerName,
    string LocalFilePath,
    long TotalBytes,
    long DownloadedBytes,
    long StartOffset,
    int RetryCount,
    string? ExpectedChecksum,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdatedAt,
    bool IsCompleted)
{
    /// <summary>
    /// Creates a new download session for a blob.
    /// </summary>
    /// <param name="blobName">The name of the blob to download.</param>
    /// <param name="containerName">The name of the container containing the blob.</param>
    /// <param name="localFilePath">The local file path for the download.</param>
    /// <param name="totalBytes">The total size of the blob.</param>
    /// <param name="expectedChecksum">Expected checksum of the blob.</param>
    /// <returns>A new download session.</returns>
    public static DownloadSession Create(string blobName, string containerName, string localFilePath, long totalBytes, string? expectedChecksum = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new DownloadSession(
            BlobName: blobName,
            ContainerName: containerName,
            LocalFilePath: localFilePath,
            TotalBytes: totalBytes,
            DownloadedBytes: 0,
            StartOffset: 0,
            RetryCount: 0,
            ExpectedChecksum: expectedChecksum,
            CreatedAt: now,
            LastUpdatedAt: now,
            IsCompleted: false);
    }

    /// <summary>
    /// Creates a resumed download session from an existing partial download.
    /// </summary>
    /// <param name="blobName">The name of the blob to download.</param>
    /// <param name="containerName">The name of the container containing the blob.</param>
    /// <param name="localFilePath">The local file path for the download.</param>
    /// <param name="totalBytes">The total size of the blob.</param>
    /// <param name="existingBytes">The number of bytes already downloaded.</param>
    /// <param name="expectedChecksum">Expected checksum of the blob.</param>
    /// <returns>A resumed download session.</returns>
    public static DownloadSession Resume(string blobName, string containerName, string localFilePath, long totalBytes, long existingBytes, string? expectedChecksum = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new DownloadSession(
            BlobName: blobName,
            ContainerName: containerName,
            LocalFilePath: localFilePath,
            TotalBytes: totalBytes,
            DownloadedBytes: existingBytes,
            StartOffset: existingBytes,
            RetryCount: 0,
            ExpectedChecksum: expectedChecksum,
            CreatedAt: now,
            LastUpdatedAt: now,
            IsCompleted: false);
    }

    /// <summary>
    /// Updates the download session with new progress.
    /// </summary>
    /// <param name="downloadedBytes">The total number of bytes downloaded.</param>
    /// <returns>Updated download session.</returns>
    public DownloadSession UpdateProgress(long downloadedBytes) =>
        this with 
        { 
            DownloadedBytes = downloadedBytes,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Increments the retry count for this download session.
    /// </summary>
    /// <returns>Updated download session with incremented retry count.</returns>
    public DownloadSession IncrementRetryCount() =>
        this with 
        { 
            RetryCount = RetryCount + 1,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Marks the download session as completed.
    /// </summary>
    /// <returns>Updated download session marked as completed.</returns>
    public DownloadSession MarkCompleted() =>
        this with 
        { 
            DownloadedBytes = TotalBytes,
            IsCompleted = true,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Gets the download progress percentage (0-100).
    /// </summary>
    public double ProgressPercentage => TotalBytes > 0 ? (double)DownloadedBytes / TotalBytes * 100 : 0;

    /// <summary>
    /// Gets the remaining bytes to download.
    /// </summary>
    public long RemainingBytes => TotalBytes - DownloadedBytes;

    /// <summary>
    /// Determines if this download can be resumed.
    /// </summary>
    public bool CanResume => !IsCompleted && DownloadedBytes > 0 && DownloadedBytes < TotalBytes;
}