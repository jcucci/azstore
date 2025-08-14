using AzStore.Core.Models;

namespace AzStore.Core;

/// <summary>
/// Provides operations for interacting with Azure Blob Storage.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Lists all containers in the storage account.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A collection of containers in the storage account.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when authentication fails or access is denied.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the storage service is not properly configured.</exception>
    Task<IEnumerable<Container>> ListContainersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists blobs in the specified container, optionally filtered by prefix.
    /// </summary>
    /// <param name="containerName">The name of the container to list blobs from.</param>
    /// <param name="prefix">Optional prefix to filter blobs (for virtual directory navigation).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A collection of blobs in the specified container.</returns>
    /// <exception cref="ArgumentException">Thrown when containerName is null or empty.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when authentication fails or access is denied.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the container does not exist.</exception>
    Task<IEnumerable<Blob>> ListBlobsAsync(string containerName, string? prefix = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a specific blob.
    /// </summary>
    /// <param name="containerName">The name of the container containing the blob.</param>
    /// <param name="blobName">The name of the blob to retrieve information for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Detailed information about the specified blob, or null if not found.</returns>
    /// <exception cref="ArgumentException">Thrown when containerName or blobName is null or empty.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when authentication fails or access is denied.</exception>
    Task<Blob?> GetBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a blob to the specified local file path.
    /// </summary>
    /// <param name="containerName">The name of the container containing the blob.</param>
    /// <param name="blobName">The name of the blob to download.</param>
    /// <param name="localFilePath">The local file path where the blob should be downloaded.</param>
    /// <param name="overwrite">Whether to overwrite the local file if it already exists.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of bytes downloaded.</returns>
    /// <exception cref="ArgumentException">Thrown when any parameter is null or empty.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when authentication fails or access is denied.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the blob does not exist or cannot be downloaded.</exception>
    /// <exception cref="IOException">Thrown when there is an issue with the local file system.</exception>
    Task<long> DownloadBlobAsync(string containerName, string blobName, string localFilePath, bool overwrite = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads multiple blobs from a container to a local directory, maintaining the blob hierarchy.
    /// </summary>
    /// <param name="containerName">The name of the container containing the blobs.</param>
    /// <param name="localDirectoryPath">The local directory path where blobs should be downloaded.</param>
    /// <param name="prefix">Optional prefix to filter which blobs to download.</param>
    /// <param name="overwrite">Whether to overwrite local files if they already exist.</param>
    /// <param name="progress">Optional progress reporter for tracking download progress.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A collection of download results with blob names and local file paths.</returns>
    /// <exception cref="ArgumentException">Thrown when containerName or localDirectoryPath is null or empty.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when authentication fails or access is denied.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the local directory does not exist and cannot be created.</exception>
    Task<IEnumerable<DownloadResult>> DownloadBlobsAsync(
        string containerName, 
        string localDirectoryPath, 
        string? prefix = null, 
        bool overwrite = false, 
        IProgress<DownloadProgress>? progress = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the storage service is properly authenticated and can access the storage account.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>true if the service is authenticated and can access the storage account; otherwise, false.</returns>
    Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about the storage account being accessed.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Information about the storage account, or null if not accessible.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when authentication fails or access is denied.</exception>
    Task<StorageAccountInfo?> GetStorageAccountInfoAsync(CancellationToken cancellationToken = default);
}