namespace AzStore.Core.Models.Downloads;

/// <summary>
/// Represents progress information for blob download operations.
/// </summary>
/// <param name="TotalBlobs">The total number of blobs to download.</param>
/// <param name="CompletedBlobs">The number of blobs that have been completed.</param>
/// <param name="CurrentBlobName">The name of the blob currently being downloaded.</param>
/// <param name="CurrentBlobProgress">The progress of the current blob download (0-1).</param>
/// <param name="TotalBytesDownloaded">The total number of bytes downloaded across all blobs.</param>
public record DownloadProgress(
    int TotalBlobs,
    int CompletedBlobs,
    string CurrentBlobName,
    double CurrentBlobProgress,
    long TotalBytesDownloaded);