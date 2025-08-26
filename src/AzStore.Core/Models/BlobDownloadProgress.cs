namespace AzStore.Core.Models;

/// <summary>
/// Enhanced progress information for blob download operations with detailed metrics.
/// </summary>
/// <param name="BlobName">The name of the blob being downloaded.</param>
/// <param name="TotalBytes">The total size of the blob in bytes.</param>
/// <param name="DownloadedBytes">The number of bytes downloaded so far.</param>
/// <param name="ProgressPercentage">The progress percentage (0-100).</param>
/// <param name="BytesPerSecond">Current download speed in bytes per second.</param>
/// <param name="EstimatedTimeRemainingSeconds">Estimated time remaining in seconds, null if unknown.</param>
/// <param name="RetryAttempt">Current retry attempt number (0 for first attempt).</param>
/// <param name="Stage">Current stage of the download process.</param>
public record BlobDownloadProgress(
    string BlobName,
    long TotalBytes,
    long DownloadedBytes,
    double ProgressPercentage,
    long BytesPerSecond,
    int? EstimatedTimeRemainingSeconds,
    int RetryAttempt,
    DownloadStage Stage)
{
    /// <summary>
    /// Creates a starting progress for a blob download.
    /// </summary>
    /// <param name="blobName">The name of the blob being downloaded.</param>
    /// <param name="totalBytes">The total size of the blob.</param>
    /// <returns>Initial progress state.</returns>
    public static BlobDownloadProgress Starting(string blobName, long totalBytes) =>
        new(blobName, totalBytes, 0, 0, 0, null, 0, DownloadStage.Starting);

    /// <summary>
    /// Creates an updated progress for a blob download.
    /// </summary>
    /// <param name="blobName">The name of the blob being downloaded.</param>
    /// <param name="totalBytes">The total size of the blob.</param>
    /// <param name="downloadedBytes">Bytes downloaded so far.</param>
    /// <param name="bytesPerSecond">Current download speed.</param>
    /// <param name="retryAttempt">Current retry attempt.</param>
    /// <returns>Updated progress state.</returns>
    public static BlobDownloadProgress Update(string blobName, long totalBytes, long downloadedBytes, long bytesPerSecond, int retryAttempt = 0)
    {
        var progressPercentage = totalBytes > 0 ? (double)downloadedBytes / totalBytes * 100 : 0;
        var remainingBytes = totalBytes - downloadedBytes;
        int? eta = bytesPerSecond > 0 && remainingBytes > 0 ? (int)(remainingBytes / bytesPerSecond) : null;

        return new BlobDownloadProgress(
            blobName,
            totalBytes,
            downloadedBytes,
            progressPercentage,
            bytesPerSecond,
            eta,
            retryAttempt,
            DownloadStage.Downloading);
    }

    /// <summary>
    /// Creates a completed progress for a blob download.
    /// </summary>
    /// <param name="blobName">The name of the blob that was downloaded.</param>
    /// <param name="totalBytes">The total size of the blob.</param>
    /// <returns>Completed progress state.</returns>
    public static BlobDownloadProgress Completed(string blobName, long totalBytes) =>
        new(blobName, totalBytes, totalBytes, 100, 0, 0, 0, DownloadStage.Completed);

    /// <summary>
    /// Creates a failed progress for a blob download.
    /// </summary>
    /// <param name="blobName">The name of the blob that failed to download.</param>
    /// <param name="totalBytes">The total size of the blob.</param>
    /// <param name="downloadedBytes">Bytes downloaded before failure.</param>
    /// <param name="retryAttempt">The retry attempt that failed.</param>
    /// <returns>Failed progress state.</returns>
    public static BlobDownloadProgress Failed(string blobName, long totalBytes, long downloadedBytes, int retryAttempt) =>
        new(blobName, totalBytes, downloadedBytes, totalBytes > 0 ? (double)downloadedBytes / totalBytes * 100 : 0, 0, null, retryAttempt, DownloadStage.Failed);
}