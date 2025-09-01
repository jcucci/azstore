using AzStore.Core.Models.Authentication;
using AzStore.Core.Models.Downloads;
using AzStore.Core.Models.Navigation;
using AzStore.Core.Models.Paging;
using AzStore.Core.Models.Storage;

namespace AzStore.Core.Services.Abstractions;

/// <summary>
/// Provides operations for interacting with Azure Blob Storage.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Lists containers in the storage account with pagination support.
    /// </summary>
    /// <param name="pageRequest">Page request parameters (page size and continuation token).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A page of containers with continuation token for subsequent pages.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when authentication fails or access is denied.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the storage service is not properly configured.</exception>
    Task<PagedResult<Container>> ListContainersAsync(PageRequest pageRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed properties for a specific container.
    /// </summary>
    /// <param name="containerName">The name of the container to retrieve properties for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Detailed container properties, or null if the container does not exist.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when authentication fails or access is denied.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the storage service is not properly configured.</exception>
    Task<Container?> GetContainerPropertiesAsync(string containerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates access to a specific container.
    /// </summary>
    /// <param name="containerName">The name of the container to validate access for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>true if the container exists and is accessible; otherwise, false.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the storage service is not properly configured.</exception>
    Task<bool> ValidateContainerAccessAsync(string containerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists blobs in the specified container with pagination support, optionally filtered by prefix.
    /// </summary>
    /// <param name="containerName">The name of the container to list blobs from.</param>
    /// <param name="prefix">Optional prefix to filter blobs (for virtual directory navigation).</param>
    /// <param name="pageRequest">Page request parameters (page size and continuation token).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A page of blobs with continuation token for subsequent pages.</returns>
    /// <exception cref="ArgumentException">Thrown when containerName is null or empty.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when authentication fails or access is denied.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the container does not exist.</exception>
    Task<PagedResult<Blob>> ListBlobsAsync(string containerName, string? prefix, PageRequest pageRequest, CancellationToken cancellationToken = default);

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
    /// <exception cref="UnauthorizedAccessException">Thrown when authentication fails or access is denied.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the blob does not exist or cannot be downloaded.</exception>
    /// <exception cref="IOException">Thrown when there is an issue with the local file system.</exception>
    Task<long> DownloadBlobAsync(string containerName, string blobName, string localFilePath, bool overwrite = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a blob with advanced options and progress tracking.
    /// </summary>
    /// <param name="containerName">The name of the container containing the blob.</param>
    /// <param name="blobName">The name of the blob to download.</param>
    /// <param name="localFilePath">The local file path where the blob should be downloaded.</param>
    /// <param name="options">Download options including conflict resolution and retry settings.</param>
    /// <param name="progress">Optional progress callback for tracking download progress.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A download result with success status and details.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when authentication fails or access is denied.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the blob does not exist or cannot be downloaded.</exception>
    /// <exception cref="IOException">Thrown when there is an issue with the local file system.</exception>
    Task<DownloadResult> DownloadBlobWithProgressAsync(string containerName, string blobName, string localFilePath, DownloadOptions? options = null, IProgress<BlobDownloadProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads multiple blobs matching a pattern with progress tracking.
    /// </summary>
    /// <param name="containerName">The name of the container containing the blobs.</param>
    /// <param name="blobPattern">Pattern to match blob names (supports wildcards * and ?).</param>
    /// <param name="localDirectoryPath">The local directory path where blobs should be downloaded.</param>
    /// <param name="options">Download options including conflict resolution and retry settings.</param>
    /// <param name="progress">Optional progress callback for tracking overall download progress.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of download results for each blob.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when authentication fails or access is denied.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the container does not exist.</exception>
    /// <exception cref="IOException">Thrown when there is an issue with the local file system.</exception>
    Task<IReadOnlyList<DownloadResult>> DownloadBlobsAsync(string containerName, string blobPattern, string localDirectoryPath, DownloadOptions? options = null, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a previously interrupted download using the download session state.
    /// </summary>
    /// <param name="session">The download session containing the current state.</param>
    /// <param name="options">Download options including retry settings.</param>
    /// <param name="progress">Optional progress callback for tracking download progress.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A download result with success status and updated session state.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when authentication fails or access is denied.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the blob does not exist or cannot be downloaded.</exception>
    /// <exception cref="IOException">Thrown when there is an issue with the local file system.</exception>
    Task<DownloadResult> ResumeDownloadAsync(DownloadSession session, DownloadOptions? options = null, IProgress<BlobDownloadProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the integrity of a downloaded blob by comparing checksums.
    /// </summary>
    /// <param name="containerName">The name of the container containing the blob.</param>
    /// <param name="blobName">The name of the blob to verify.</param>
    /// <param name="localFilePath">The local file path of the downloaded blob.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if the file integrity is verified, false if checksums don't match.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the local file does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when authentication fails or access is denied.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the blob does not exist.</exception>
    Task<bool> VerifyDownloadIntegrityAsync(string containerName, string blobName, string localFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists blobs and virtual directories in the specified container using hierarchical listing.
    /// This method provides a file system-like view of blob storage using "/" as a delimiter.
    /// </summary>
    /// <param name="containerName">The name of the container to browse.</param>
    /// <param name="prefix">Optional prefix to filter results (virtual directory path).</param>
    /// <param name="pageRequest">Page request parameters (page size and continuation token).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A browsing result containing both virtual directories and blobs at the current level.</returns>
    /// <exception cref="ArgumentException">Thrown when containerName is null or empty.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when authentication fails or access is denied.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the container does not exist.</exception>
    Task<BrowsingResult> BrowseBlobsAsync(string containerName, string? prefix, PageRequest pageRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists only virtual directories in the specified container at the given prefix level.
    /// </summary>
    /// <param name="containerName">The name of the container to list directories from.</param>
    /// <param name="prefix">Optional prefix to filter directories (parent virtual directory path).</param>
    /// <param name="pageRequest">Page request parameters (page size and continuation token).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A page of virtual directories with continuation token for subsequent pages.</returns>
    /// <exception cref="ArgumentException">Thrown when containerName is null or empty.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when authentication fails or access is denied.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the container does not exist.</exception>
    Task<PagedResult<VirtualDirectory>> ListVirtualDirectoriesAsync(string containerName, string? prefix, PageRequest pageRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Navigates directly to a specific path within a container and returns the browsing result.
    /// The path is treated as a virtual directory prefix.
    /// </summary>
    /// <param name="containerName">The name of the container to navigate within.</param>
    /// <param name="path">The path to navigate to (e.g., "folder1/subfolder2").</param>
    /// <param name="pageRequest">Page request parameters (page size and continuation token).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A browsing result for the specified path.</returns>
    /// <exception cref="ArgumentException">Thrown when containerName is null or empty.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when authentication fails or access is denied.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the container does not exist.</exception>
    Task<BrowsingResult> NavigateToPathAsync(string containerName, string? path, PageRequest pageRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for blobs within the specified container using pattern matching.
    /// Supports wildcards (*) and can search within a specific prefix.
    /// </summary>
    /// <param name="containerName">The name of the container to search within.</param>
    /// <param name="searchPattern">The search pattern with wildcard support (* and ?).</param>
    /// <param name="prefix">Optional prefix to limit search scope (virtual directory path).</param>
    /// <param name="pageRequest">Page request parameters (page size and continuation token).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A page of blobs matching the search pattern.</returns>
    /// <exception cref="ArgumentException">Thrown when containerName or searchPattern is null or empty.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when authentication fails or access is denied.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the container does not exist.</exception>
    Task<PagedResult<Blob>> SearchBlobsAsync(string containerName, string searchPattern, string? prefix, PageRequest pageRequest, CancellationToken cancellationToken = default);


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