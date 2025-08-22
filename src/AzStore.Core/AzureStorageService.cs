using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AzStore.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace AzStore.Core;

/// <summary>
/// Provides Azure Blob Storage operations using the Azure SDK.
/// </summary>
public class AzureStorageService : IStorageService
{
    private readonly ILogger<AzureStorageService> _logger;
    private readonly IAuthenticationService _authenticationService;

    private readonly Lock _lock = new();
    private BlobServiceClient? _blobServiceClient;
    private string? _currentStorageAccountName;
    private bool _isConnected;

    private const int SearchPageSizeMultiplier = 5;

    /// <summary>
    /// Initializes a new instance of the AzureStorageService.
    /// </summary>
    /// <param name="logger">Logger instance for this service.</param>
    /// <param name="authenticationService">Service for Azure authentication.</param>
    public AzureStorageService(ILogger<AzureStorageService> logger, IAuthenticationService authenticationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
    }

    /// <summary>
    /// Connects to an Azure Storage Account by name using the current Azure CLI authentication.
    /// </summary>
    /// <param name="accountName">The name of the storage account to connect to.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if connection succeeded, false otherwise.</returns>
    public async Task<bool> ConnectToStorageAccountAsync(string accountName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Connecting to storage account: {AccountName}", accountName);

        try
        {
            var authResult = await _authenticationService.GetCurrentAuthenticationAsync(cancellationToken);
            if (authResult == null || !authResult.Success)
            {
                _logger.LogWarning("Cannot connect to storage account: not authenticated with Azure");
                return false;
            }

            var storageUri = new Uri($"https://{accountName}.blob.core.windows.net");

            var credential = _authenticationService.GetCredential();
            if (credential == null)
            {
                _logger.LogWarning("Cannot connect to storage account: no valid credential available");
                return false;
            }

            var blobServiceClient = new BlobServiceClient(storageUri, credential);

            await blobServiceClient.GetAccountInfoAsync(cancellationToken);

            lock (_lock)
            {
                _blobServiceClient = blobServiceClient;
                _currentStorageAccountName = accountName;
                _isConnected = true;
            }

            _logger.LogInformation("Successfully connected to storage account: {AccountName}", accountName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to storage account: {AccountName}", accountName);

            lock (_lock)
            {
                _blobServiceClient = null;
                _currentStorageAccountName = null;
                _isConnected = false;
            }

            return false;
        }
    }

    /// <summary>
    /// Validates that the current connection to the storage account is working.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if the connection is valid, false otherwise.</returns>
    public async Task<bool> ValidateStorageAccountAccessAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_isConnected || _blobServiceClient == null)
            {
                _logger.LogDebug("Storage account validation failed: not connected");
                return false;
            }
        }

        try
        {
            _logger.LogDebug("Validating storage account access");
            await _blobServiceClient.GetAccountInfoAsync(cancellationToken);
            _logger.LogDebug("Storage account validation successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Storage account validation failed");

            lock (_lock)
            {
                _isConnected = false;
            }

            return false;
        }
    }

    /// <summary>
    /// Gets the current connection status.
    /// </summary>
    /// <returns>True if connected to a storage account, false otherwise.</returns>
    public bool GetConnectionStatus()
    {
        lock (_lock)
        {
            return _isConnected && _blobServiceClient != null && !string.IsNullOrEmpty(_currentStorageAccountName);
        }
    }

    /// <inheritdoc/>
    public async Task<PagedResult<Container>> ListContainersAsync(PageRequest pageRequest, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        _logger.LogDebug("Listing containers in storage account: {AccountName} (Page size: {PageSize}, Continuation: {HasContinuation})",
            _currentStorageAccountName, pageRequest.PageSize, !string.IsNullOrEmpty(pageRequest.ContinuationToken));

        var containers = new List<Container>();
        var pages = _blobServiceClient!.GetBlobContainersAsync(cancellationToken: cancellationToken)
            .AsPages(pageRequest.ContinuationToken, pageRequest.PageSize);

        await foreach (var page in pages)
        {
            foreach (var containerItem in page.Values)
            {
                var container = new Container
                {
                    Name = containerItem.Name,
                    Path = containerItem.Name,
                    LastModified = containerItem.Properties.LastModified,
                    ETag = containerItem.Properties.ETag.ToString(),
                    Metadata = ConvertMetadata(containerItem.Properties.Metadata),
                    AccessLevel = await GetContainerAccessLevelAsync(containerItem.Name, cancellationToken),
                    HasImmutabilityPolicy = containerItem.Properties.HasImmutabilityPolicy,
                    HasLegalHold = containerItem.Properties.HasLegalHold
                };

                containers.Add(container);
            }

            _logger.LogDebug("Retrieved {ContainerCount} containers, continuation token: {HasContinuation}",
                containers.Count, !string.IsNullOrEmpty(page.ContinuationToken));

            return new PagedResult<Container>(containers, page.ContinuationToken);
        }

        _logger.LogDebug("No containers found");
        return PagedResult<Container>.Empty();
    }

    /// <inheritdoc/>
    public async Task<Container?> GetContainerPropertiesAsync(string containerName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting container properties: {ContainerName}", containerName);

        try
        {
            var containerClient = GetContainerClient(containerName);

            var exists = await containerClient.ExistsAsync(cancellationToken);
            if (!exists.Value)
            {
                _logger.LogDebug("Container not found: {ContainerName}", containerName);
                return null;
            }

            var properties = await containerClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            var container = new Container
            {
                Name = containerName,
                Path = containerName,
                LastModified = properties.Value.LastModified,
                ETag = properties.Value.ETag.ToString(),
                Metadata = ConvertMetadata(properties.Value.Metadata),
                AccessLevel = await GetContainerAccessLevelAsync(containerName, cancellationToken),
                HasImmutabilityPolicy = properties.Value.HasImmutabilityPolicy,
                HasLegalHold = properties.Value.HasLegalHold,
                LeaseState = properties.Value.LeaseState?.ToString(),
                LeaseStatus = properties.Value.LeaseStatus?.ToString()
            };

            _logger.LogDebug("Successfully retrieved container properties: {ContainerName}", containerName);
            return container;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get container properties: {ContainerName}", containerName);
            throw new InvalidOperationException($"Failed to get container properties for '{containerName}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateContainerAccessAsync(string containerName, CancellationToken cancellationToken = default)
    {
        EnsureConnected(); // Explicit connection check that throws for infrastructure issues

        _logger.LogDebug("Validating container access: {ContainerName}", containerName);

        try
        {
            var containerClient = _blobServiceClient!.GetBlobContainerClient(containerName);
            var exists = await containerClient.ExistsAsync(cancellationToken);

            if (!exists.Value)
            {
                _logger.LogDebug("Container does not exist: {ContainerName}", containerName);
                return false;
            }

            await containerClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            _logger.LogDebug("Container access validated successfully: {ContainerName}", containerName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Container access validation failed: {ContainerName}", containerName);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<PagedResult<Blob>> ListBlobsAsync(string containerName, string? prefix, PageRequest pageRequest, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing blobs in container: {ContainerName} with prefix: {Prefix} (Page size: {PageSize}, Continuation: {HasContinuation})",
            containerName, prefix ?? "(none)", pageRequest.PageSize, !string.IsNullOrEmpty(pageRequest.ContinuationToken));

        var containerClient = GetContainerClient(containerName);
        var blobs = new List<Blob>();

        var pages = containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken)
            .AsPages(pageRequest.ContinuationToken, pageRequest.PageSize);

        await foreach (var page in pages)
        {
            foreach (var blobItem in page.Values)
            {
                var blob = Blob.FromBlobItem(blobItem, containerName);
                blobs.Add(blob);
            }

            _logger.LogDebug("Retrieved {BlobCount} blobs from container: {ContainerName}, continuation token: {HasContinuation}",
                blobs.Count, containerName, !string.IsNullOrEmpty(page.ContinuationToken));

            return new PagedResult<Blob>(blobs, page.ContinuationToken);
        }

        _logger.LogDebug("No blobs found in container: {ContainerName}", containerName);
        return PagedResult<Blob>.Empty();
    }

    /// <inheritdoc/>
    public async Task<Blob?> GetBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting blob details: {BlobName} in container: {ContainerName}", blobName, containerName);

        try
        {
            var containerClient = GetContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var exists = await blobClient.ExistsAsync(cancellationToken);
            if (!exists.Value)
            {
                _logger.LogDebug("Blob not found: {BlobName} in container: {ContainerName}", blobName, containerName);
                return null;
            }

            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            var blob = Blob.FromBlobProperties(blobName, properties.Value, containerName);

            _logger.LogDebug("Successfully retrieved blob details: {BlobName}", blobName);
            return blob;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get blob: {BlobName} in container: {ContainerName}", blobName, containerName);
            throw new InvalidOperationException($"Failed to get blob '{blobName}' in container '{containerName}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<long> DownloadBlobAsync(string containerName, string blobName, string localFilePath, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Downloading blob: {BlobName} from container: {ContainerName} to: {LocalFilePath}", blobName, containerName, localFilePath);

        try
        {
            var containerClient = GetContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var exists = await blobClient.ExistsAsync(cancellationToken);
            if (!exists.Value)
            {
                throw new InvalidOperationException($"Blob '{blobName}' does not exist in container '{containerName}'");
            }

            if (File.Exists(localFilePath) && !overwrite)
            {
                throw new IOException($"File '{localFilePath}' already exists and overwrite is disabled");
            }

            var directory = Path.GetDirectoryName(localFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogDebug("Created directory: {Directory}", directory);
            }

            var downloadResponse = await blobClient.DownloadToAsync(localFilePath, cancellationToken);
            var fileInfo = new FileInfo(localFilePath);
            var bytesDownloaded = fileInfo.Length;

            _logger.LogInformation("Successfully downloaded blob: {BlobName} ({BytesDownloaded} bytes) to: {LocalFilePath}",
                blobName, bytesDownloaded, localFilePath);

            return bytesDownloaded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download blob: {BlobName} from container: {ContainerName}", blobName, containerName);
            throw new InvalidOperationException($"Failed to download blob '{blobName}' from container '{containerName}': {ex.Message}", ex);
        }
    }


    /// <inheritdoc/>
    public async Task<BrowsingResult> BrowseBlobsAsync(string containerName, string? prefix, PageRequest pageRequest, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Browsing blobs hierarchically in container: {ContainerName} with prefix: {Prefix} (Page size: {PageSize})",
            containerName, prefix ?? "(none)", pageRequest.PageSize);

        var containerClient = GetContainerClient(containerName);
        var virtualDirectories = new List<VirtualDirectory>();
        var blobs = new List<Blob>();

        var pages = containerClient.GetBlobsByHierarchyAsync(
                delimiter: "/",
                prefix: prefix,
                cancellationToken: cancellationToken)
            .AsPages(pageRequest.ContinuationToken, pageRequest.PageSize);

        await foreach (var page in pages)
        {
            foreach (var item in page.Values)
            {
                if (item.IsPrefix)
                {
                    var virtualDir = VirtualDirectory.Create(item.Prefix, containerName);
                    virtualDirectories.Add(virtualDir);
                }
                else
                {
                    var blob = Blob.FromBlobItem(item.Blob, containerName);
                    blobs.Add(blob);
                }
            }

            _logger.LogDebug("Retrieved {DirectoryCount} directories and {BlobCount} blobs from container: {ContainerName}",
                virtualDirectories.Count, blobs.Count, containerName);

            return BrowsingResult.Create(virtualDirectories, blobs, containerName, prefix, page.ContinuationToken);
        }

        _logger.LogDebug("No items found in container: {ContainerName}", containerName);
        return BrowsingResult.Empty(containerName, prefix);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<VirtualDirectory>> ListVirtualDirectoriesAsync(string containerName, string? prefix, PageRequest pageRequest, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing virtual directories in container: {ContainerName} with prefix: {Prefix}",
            containerName, prefix ?? "(none)");

        var containerClient = GetContainerClient(containerName);
        var directories = new List<VirtualDirectory>();

        var pages = containerClient.GetBlobsByHierarchyAsync(
                delimiter: "/",
                prefix: prefix,
                cancellationToken: cancellationToken)
            .AsPages(pageRequest.ContinuationToken, pageRequest.PageSize);

        await foreach (var page in pages)
        {
            foreach (var item in page.Values)
            {
                if (item.IsPrefix)
                {
                    var virtualDir = VirtualDirectory.Create(item.Prefix, containerName);
                    directories.Add(virtualDir);
                }
            }

            _logger.LogDebug("Retrieved {DirectoryCount} virtual directories from container: {ContainerName}",
                directories.Count, containerName);

            return new PagedResult<VirtualDirectory>(directories, page.ContinuationToken);
        }

        _logger.LogDebug("No virtual directories found in container: {ContainerName}", containerName);
        return PagedResult<VirtualDirectory>.Empty();
    }

    /// <inheritdoc/>
    public async Task<BrowsingResult> NavigateToPathAsync(string containerName, string? path, PageRequest pageRequest, CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePath(path);
        _logger.LogDebug("Navigating to path: {Path} in container: {ContainerName}", normalizedPath ?? "(root)", containerName);

        return await BrowseBlobsAsync(containerName, normalizedPath, pageRequest, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<Blob>> SearchBlobsAsync(string containerName, string searchPattern, string? prefix, PageRequest pageRequest, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching blobs in container: {ContainerName} with pattern: {Pattern} and prefix: {Prefix}",
            containerName, searchPattern, prefix ?? "(none)");

        var allBlobs = new List<Blob>();
        var containerClient = GetContainerClient(containerName);

        var regexPattern = "^" + Regex.Escape(searchPattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        var pages = containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken)
            .AsPages(pageRequest.ContinuationToken, pageRequest.PageSize * SearchPageSizeMultiplier); // Get more items to filter

        await foreach (var page in pages)
        {
            foreach (var blobItem in page.Values)
            {
                var blobName = Path.GetFileName(blobItem.Name);
                if (regex.IsMatch(blobName))
                {
                    var blob = Blob.FromBlobItem(blobItem, containerName);
                    allBlobs.Add(blob);

                    if (allBlobs.Count >= pageRequest.PageSize)
                    {
                        break;
                    }
                }
            }

            if (allBlobs.Count >= pageRequest.PageSize)
            {
                _logger.LogDebug("Found {BlobCount} matching blobs (reached page size limit)", allBlobs.Count);
                return new PagedResult<Blob>([.. allBlobs.Take(pageRequest.PageSize)], page.ContinuationToken);
            }
        }

        _logger.LogDebug("Search completed. Found {BlobCount} matching blobs", allBlobs.Count);
        return new PagedResult<Blob>(allBlobs, null);
    }

    /// <inheritdoc/>
    public async Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        var authResult = await _authenticationService.GetCurrentAuthenticationAsync(cancellationToken);
        return authResult?.Success == true && GetConnectionStatus();
    }

    /// <inheritdoc/>
    public async Task<StorageAccountInfo?> GetStorageAccountInfoAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_isConnected || _blobServiceClient == null || string.IsNullOrEmpty(_currentStorageAccountName))
            {
                return null;
            }
        }

        try
        {
            var accountInfo = await _blobServiceClient.GetAccountInfoAsync(cancellationToken);
            var properties = await _blobServiceClient.GetPropertiesAsync(cancellationToken);

            return new StorageAccountInfo(
                AccountName: _currentStorageAccountName,
                AccountKind: accountInfo.Value.AccountKind.ToString(),
                SubscriptionId: null, // TODO: Get from authentication service
                ResourceGroupName: null, // TODO: Extract from ARM
                PrimaryEndpoint: _blobServiceClient.Uri
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get storage account info");
            return null;
        }
    }


    /// <summary>
    /// Normalizes a path by ensuring it ends with "/" if not null or empty, and handles null/empty cases.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path or null if input was null or empty.</returns>
    private static string? NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var trimmed = path.Trim();
        return trimmed.EndsWith('/') ? trimmed : trimmed + "/";
    }

    /// <summary>
    /// Ensures the service is connected to a storage account, throwing an exception if not.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when not connected to a storage account.</exception>
    private void EnsureConnected()
    {
        lock (_lock)
        {
            if (!_isConnected || _blobServiceClient == null)
            {
                throw new InvalidOperationException("Not connected to a storage account. Call ConnectToStorageAccountAsync first.");
            }
        }
    }


    /// <summary>
    /// Converts Azure metadata dictionary to a standard dictionary, handling null values.
    /// </summary>
    /// <param name="metadata">The metadata dictionary to convert.</param>
    /// <returns>A converted dictionary or empty dictionary if input is null.</returns>
    private static Dictionary<string, string> ConvertMetadata(IDictionary<string, string>? metadata) =>
        metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? [];

    /// <summary>
    /// Gets a container client for the specified container name.
    /// </summary>
    /// <param name="containerName">The name of the container.</param>
    /// <returns>A BlobContainerClient for the specified container.</returns>
    /// <exception cref="InvalidOperationException">Thrown when not connected to a storage account.</exception>
    private BlobContainerClient GetContainerClient(string containerName)
    {
        EnsureConnected();
        return _blobServiceClient!.GetBlobContainerClient(containerName);
    }

    /// <summary>
    /// Gets the public access level for a container.
    /// </summary>
    /// <param name="containerName">The name of the container.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The container's public access level.</returns>
    private async Task<ContainerAccessLevel> GetContainerAccessLevelAsync(string containerName, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = GetContainerClient(containerName);
            var accessPolicy = await containerClient.GetAccessPolicyAsync(cancellationToken: cancellationToken);
            return accessPolicy.Value.BlobPublicAccess switch
            {
                PublicAccessType.BlobContainer => ContainerAccessLevel.Container,
                PublicAccessType.Blob => ContainerAccessLevel.Blob,
                _ => ContainerAccessLevel.None
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not retrieve access policy for container: {ContainerName}", containerName);
            return ContainerAccessLevel.None;
        }
    }
}