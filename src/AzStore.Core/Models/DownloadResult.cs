namespace AzStore.Core.Models;

/// <summary>
/// Represents the result of a blob download operation.
/// </summary>
/// <param name="BlobName">The name of the blob that was downloaded.</param>
/// <param name="LocalFilePath">The local file path where the blob was saved.</param>
/// <param name="BytesDownloaded">The number of bytes that were downloaded.</param>
/// <param name="Success">Whether the download was successful.</param>
/// <param name="Error">Any error that occurred during download, if unsuccessful.</param>
public record DownloadResult(
    string BlobName,
    string LocalFilePath,
    long BytesDownloaded,
    bool Success,
    string? Error = null);