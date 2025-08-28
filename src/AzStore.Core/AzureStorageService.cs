using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AzStore.Core.IO;
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
    private readonly IPathService _pathService;
    private readonly ISessionManager? _sessionManager;

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
    /// <param name="pathService">Service for path calculation and directory management.</param>
    /// <param name="sessionManager">Service for session management (optional).</param>
    public AzureStorageService(ILogger<AzureStorageService> logger, IAuthenticationService authenticationService, IPathService pathService, ISessionManager? sessionManager = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        _sessionManager = sessionManager;
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
        EnsureConnected();

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
    public async Task<DownloadResult> DownloadBlobWithProgressAsync(string containerName, string blobName, string localFilePath, DownloadOptions? options = null, IProgress<BlobDownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        options ??= DownloadOptions.Default;

        _logger.LogDebug("Downloading blob with progress: {BlobName} from container: {ContainerName} to: {LocalFilePath}", blobName, containerName, localFilePath);

        try
        {
            var containerClient = GetContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            var totalBytes = properties.Value.ContentLength;
            var expectedChecksum = properties.Value.ContentHash?.ToString();

            progress?.Report(BlobDownloadProgress.Starting(blobName, totalBytes));

            var resolvedFilePath = await ResolveFileConflictAsync(localFilePath, options.ConflictResolution);
            if (resolvedFilePath == null)
            {
                return new DownloadResult(
                    BlobName: blobName,
                    LocalFilePath: localFilePath,
                    BytesDownloaded: 0,
                    Success: false,
                    Error: "Download cancelled due to file conflict");
            }

            if (options.CreateDirectories)
            {
                var directoryCreated = _pathService.EnsureDirectoryExists(resolvedFilePath);
                if (!directoryCreated)
                {
                    return new DownloadResult(
                        BlobName: blobName,
                        LocalFilePath: resolvedFilePath,
                        BytesDownloaded: 0,
                        Success: false,
                        Error: "Failed to create directory structure");
                }
            }

            var downloadSession = DownloadSession.Create(blobName, containerName, resolvedFilePath, totalBytes, expectedChecksum);

            var result = await PerformDownloadWithRetryAsync(blobClient, downloadSession, options, progress, cancellationToken);

            if (result.Success && options.VerifyChecksum && !string.IsNullOrEmpty(expectedChecksum))
            {
                progress?.Report(new BlobDownloadProgress(blobName, totalBytes, totalBytes, 100, 0, 0, 0, DownloadStage.Verifying));

                var isValid = await VerifyDownloadIntegrityAsync(containerName, blobName, resolvedFilePath, cancellationToken);
                if (!isValid)
                {
                    return new DownloadResult(
                        BlobName: blobName,
                        LocalFilePath: resolvedFilePath,
                        BytesDownloaded: result.BytesDownloaded,
                        Success: false,
                        Error: "Download integrity verification failed");
                }
            }

            progress?.Report(BlobDownloadProgress.Completed(blobName, totalBytes));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download blob with progress: {BlobName} from container: {ContainerName}", blobName, containerName);
            return new DownloadResult(
                BlobName: blobName,
                LocalFilePath: localFilePath,
                BytesDownloaded: 0,
                Success: false,
                Error: ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DownloadResult>> DownloadBlobsAsync(string containerName, string blobPattern, string localDirectoryPath, DownloadOptions? options = null, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        options ??= DownloadOptions.Default;

        _logger.LogDebug("Downloading blobs matching pattern: {Pattern} from container: {ContainerName} to: {LocalDirectory}", blobPattern, containerName, localDirectoryPath);

        try
        {
            var pageRequest = new PageRequest(1000);
            var matchingBlobs = await SearchBlobsAsync(containerName, blobPattern, null, pageRequest, cancellationToken);

            var results = new List<DownloadResult>();
            var totalBlobs = matchingBlobs.Items.Count;
            var completedBlobs = 0;

            foreach (var blob in matchingBlobs.Items)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var session = _sessionManager?.GetActiveSession();
                var localFilePath = session != null 
                    ? _pathService.CalculateBlobDownloadPath(session, containerName, blob.Name)
                    : Path.Combine(localDirectoryPath, _pathService.PreserveVirtualDirectoryStructure(blob.Name));

                var blobProgress = new Progress<BlobDownloadProgress>(p =>
                {
                    var overallProgress = new DownloadProgress(
                        TotalBlobs: totalBlobs,
                        CompletedBlobs: completedBlobs,
                        CurrentBlobName: p.BlobName,
                        CurrentBlobProgress: p.ProgressPercentage / 100.0,
                        TotalBytesDownloaded: results.Sum(r => r.BytesDownloaded) + p.DownloadedBytes);

                    progress?.Report(overallProgress);
                });

                var result = await DownloadBlobWithProgressAsync(containerName, blob.Name, localFilePath, options, blobProgress, cancellationToken);
                results.Add(result);

                if (result.Success)
                {
                    completedBlobs++;
                }
            }

            return results.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download blobs matching pattern: {Pattern} from container: {ContainerName}", blobPattern, containerName);
            throw new InvalidOperationException($"Failed to download blobs matching pattern '{blobPattern}' from container '{containerName}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<DownloadResult> ResumeDownloadAsync(DownloadSession session, DownloadOptions? options = null, IProgress<BlobDownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        options ??= DownloadOptions.Default;

        _logger.LogDebug("Resuming download: {BlobName} from container: {ContainerName}", session.BlobName, session.ContainerName);

        try
        {
            var containerClient = GetContainerClient(session.ContainerName);
            var blobClient = containerClient.GetBlobClient(session.BlobName);

            // Validate local file state before attempting to resume
            var validatedSession = await ValidateAndRepairDownloadSessionAsync(session);
            if (validatedSession == null)
            {
                _logger.LogWarning("Download session validation failed, falling back to fresh download: {BlobName}", session.BlobName);
                
                var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
                var freshSession = DownloadSession.Create(
                    blobName: session.BlobName,
                    containerName: session.ContainerName,
                    localFilePath: session.LocalFilePath,
                    totalBytes: properties.Value.ContentLength,
                    expectedChecksum: properties.Value.ContentHash?.ToString());

                return await PerformDownloadWithRetryAsync(blobClient, freshSession, options, progress, cancellationToken);
            }

            return await PerformDownloadWithRetryAsync(blobClient, validatedSession, options, progress, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume download: {BlobName} from container: {ContainerName}", session.BlobName, session.ContainerName);
            return new DownloadResult(
                BlobName: session.BlobName,
                LocalFilePath: session.LocalFilePath,
                BytesDownloaded: session.DownloadedBytes,
                Success: false,
                Error: ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyDownloadIntegrityAsync(string containerName, string blobName, string localFilePath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Verifying download integrity: {BlobName} at {LocalFilePath}", blobName, localFilePath);

        try
        {
            if (!File.Exists(localFilePath))
            {
                throw new FileNotFoundException($"Local file not found: {localFilePath}");
            }

            var containerClient = GetContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            var expectedHash = properties.Value.ContentHash;

            if (expectedHash == null || expectedHash.Length == 0)
            {
                _logger.LogWarning("Blob {BlobName} has no content hash for verification", blobName);
                return true;
            }

            using var fileStream = File.OpenRead(localFilePath);
            using var md5 = System.Security.Cryptography.MD5.Create();
            var actualHash = await md5.ComputeHashAsync(fileStream, cancellationToken);

            var isValid = expectedHash.SequenceEqual(actualHash);

            _logger.LogDebug("Download integrity verification result: {IsValid} for {BlobName}", isValid, blobName);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify download integrity: {BlobName}", blobName);
            throw new InvalidOperationException($"Failed to verify download integrity for blob '{blobName}': {ex.Message}", ex);
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

            return BrowsingResult.Create(
                directories: virtualDirectories,
                blobs: blobs,
                containerName: containerName,
                currentPrefix: prefix,
                continuationToken: page.ContinuationToken);
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

        var pages = containerClient.GetBlobsByHierarchyAsync(delimiter: "/", prefix: prefix, cancellationToken: cancellationToken)
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

            _logger.LogDebug("Retrieved {DirectoryCount} virtual directories from container: {ContainerName}", directories.Count, containerName);

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

    /// <summary>
    /// Resolves file conflicts based on the specified resolution strategy.
    /// </summary>
    /// <param name="filePath">The intended file path.</param>
    /// <param name="resolution">The conflict resolution strategy.</param>
    /// <returns>The resolved file path, or null if the user chose to skip.</returns>
    private async Task<string?> ResolveFileConflictAsync(string filePath, ConflictResolution resolution)
    {
        if (!File.Exists(filePath))
            return filePath;

        return resolution switch
        {
            ConflictResolution.Overwrite => filePath,
            ConflictResolution.Skip => null,
            ConflictResolution.Rename => GenerateUniqueFileName(filePath),
            ConflictResolution.Ask => await HandleFileConflictWithRenameAsync(filePath),
            _ => throw new ArgumentOutOfRangeException(nameof(resolution), resolution, "Invalid conflict resolution strategy")
        };
    }

    /// <summary>
    /// Generates a unique file name by appending a number to avoid conflicts.
    /// </summary>
    /// <param name="originalPath">The original file path.</param>
    /// <returns>A unique file path.</returns>
    private static string GenerateUniqueFileName(string originalPath)
    {
        var directory = Path.GetDirectoryName(originalPath) ?? string.Empty;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
        var extension = Path.GetExtension(originalPath);

        var counter = 1;
        string newPath;
        do
        {
            var newFileName = $"{fileNameWithoutExtension}({counter}){extension}";
            newPath = Path.Combine(directory, newFileName);
            counter++;
        }
        while (File.Exists(newPath));

        return newPath;
    }

    /// <summary>
    /// Handles file conflicts by automatically renaming the file to avoid overwriting.
    /// This is a placeholder implementation until interactive user input is added.
    /// </summary>
    /// <param name="filePath">The file path that conflicts.</param>
    /// <returns>A unique file path with a renamed filename.</returns>
    private Task<string?> HandleFileConflictWithRenameAsync(string filePath)
    {
        _logger.LogWarning("File already exists: {FilePath}", filePath);
        _logger.LogInformation("Automatically renaming file to avoid conflict");
        return Task.FromResult<string?>(GenerateUniqueFileName(filePath));
    }

    /// <summary>
    /// Validates the local file state matches the download session and repairs if possible.
    /// </summary>
    /// <param name="session">The download session to validate.</param>
    /// <returns>A validated session, or null if validation failed and cannot be repaired.</returns>
    private Task<DownloadSession?> ValidateAndRepairDownloadSessionAsync(DownloadSession session)
    {
        try
        {
            // Check if local file exists
            if (!File.Exists(session.LocalFilePath))
            {
                _logger.LogDebug("Local file does not exist for resume: {FilePath}", session.LocalFilePath);
                return Task.FromResult<DownloadSession?>(null);
            }

            var fileInfo = new FileInfo(session.LocalFilePath);
            var actualFileSize = fileInfo.Length;

            // Validate file size matches session expectation
            if (actualFileSize != session.DownloadedBytes)
            {
                _logger.LogWarning("Local file size mismatch. Expected: {Expected}, Actual: {Actual} for file: {FilePath}",
                    session.DownloadedBytes, actualFileSize, session.LocalFilePath);

                if (actualFileSize < session.TotalBytes)
                {
                    _logger.LogDebug("Repairing download session with actual file size: {ActualSize}", actualFileSize);
                    return Task.FromResult<DownloadSession?>(session.UpdateProgress(actualFileSize));
                }

                _logger.LogWarning("Cannot repair session - file is larger than expected");
                return Task.FromResult<DownloadSession?>(null);
            }

            if (fileInfo.LastWriteTimeUtc > session.LastUpdatedAt.UtcDateTime.AddMinutes(1))
            {
                _logger.LogWarning("Local file was modified after session was last updated. File: {FilePath}, " +
                    "File modified: {FileModified}, Session updated: {SessionUpdated}",
                    session.LocalFilePath, fileInfo.LastWriteTimeUtc, session.LastUpdatedAt.UtcDateTime);
                
                if (actualFileSize < session.TotalBytes)
                {
                    return Task.FromResult<DownloadSession?>(session.UpdateProgress(actualFileSize));
                }
                return Task.FromResult<DownloadSession?>(null);
            }

            _logger.LogDebug("Download session validation passed for: {BlobName}", session.BlobName);
            return Task.FromResult<DownloadSession?>(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate download session for: {BlobName}", session.BlobName);
            return Task.FromResult<DownloadSession?>(null);
        }
    }


    /// <summary>
    /// Performs the actual download with retry logic and progress tracking.
    /// </summary>
    /// <param name="blobClient">The blob client to download from.</param>
    /// <param name="session">The download session containing state.</param>
    /// <param name="options">Download options.</param>
    /// <param name="progress">Progress callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The download result.</returns>
    private async Task<DownloadResult> PerformDownloadWithRetryAsync(
        BlobClient blobClient,
        DownloadSession session,
        DownloadOptions options,
        IProgress<BlobDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var currentSession = session;

        for (int attempt = 0; attempt <= options.MaxRetryAttempts; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                    _logger.LogInformation("Retrying download attempt {Attempt} after {Delay}s delay", attempt + 1, delay.TotalSeconds);
                    await Task.Delay(delay, cancellationToken);

                    progress?.Report(BlobDownloadProgress.Update(
                        currentSession.BlobName,
                        currentSession.TotalBytes,
                        currentSession.DownloadedBytes,
                        0,
                        attempt));
                }

                var result = await PerformSingleDownloadAsync(blobClient, currentSession, options, progress, cancellationToken);

                if (result.Success)
                {
                    return result;
                }

                if (options.EnableResumption && File.Exists(currentSession.LocalFilePath))
                {
                    var fileInfo = new FileInfo(currentSession.LocalFilePath);
                    currentSession = currentSession.UpdateProgress(fileInfo.Length).IncrementRetryCount();
                }
                else
                {
                    currentSession = currentSession.IncrementRetryCount();
                }

                if (attempt == options.MaxRetryAttempts)
                {
                    return result;
                }
            }
            catch (OperationCanceledException)
            {
                return new DownloadResult(
                    BlobName: currentSession.BlobName,
                    LocalFilePath: currentSession.LocalFilePath,
                    BytesDownloaded: currentSession.DownloadedBytes,
                    Success: false,
                    Error: "Download was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Download attempt {Attempt} failed for blob: {BlobName}", attempt + 1, currentSession.BlobName);

                if (attempt == options.MaxRetryAttempts)
                {
                    return new DownloadResult(
                        BlobName: currentSession.BlobName,
                        LocalFilePath: currentSession.LocalFilePath,
                        BytesDownloaded: currentSession.DownloadedBytes,
                        Success: false,
                        Error: ex.Message);
                }
            }
        }

        return new DownloadResult(
            BlobName: currentSession.BlobName,
            LocalFilePath: currentSession.LocalFilePath,
            BytesDownloaded: currentSession.DownloadedBytes,
            Success: false,
            Error: "Maximum retry attempts exceeded");
    }

    /// <summary>
    /// Performs a single download attempt with progress tracking and bandwidth throttling.
    /// </summary>
    private async Task<DownloadResult> PerformSingleDownloadAsync(
        BlobClient blobClient,
        DownloadSession session,
        DownloadOptions options,
        IProgress<BlobDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var startTime = DateTimeOffset.UtcNow;
        var lastProgressReport = startTime;
        var startOffset = session.StartOffset;

        try
        {
            using var fileStream = startOffset > 0
                ? new FileStream(path: session.LocalFilePath, mode: FileMode.Open, access: FileAccess.Write, share: FileShare.None)
                : new FileStream(path: session.LocalFilePath, mode: FileMode.Create, access: FileAccess.Write, share: FileShare.None);

            if (startOffset > 0)
            {
                fileStream.Seek(startOffset, SeekOrigin.Begin);
            }

            Stream targetStream = options.BandwidthLimitBytesPerSecond.HasValue
                ? new ThrottledStream(fileStream, options.BandwidthLimitBytesPerSecond.Value)
                : fileStream;

            var progressStream = new ProgressTrackingStream(targetStream, totalBytes =>
            {
                var now = DateTimeOffset.UtcNow;
                var elapsed = now - lastProgressReport;

                if (elapsed.TotalMilliseconds >= 250)
                {
                    var currentBytes = startOffset + totalBytes;
                    var elapsedSinceStart = now - startTime;
                    var bytesPerSecond = elapsedSinceStart.TotalSeconds > 0
                        ? (long)(totalBytes / elapsedSinceStart.TotalSeconds)
                        : 0;

                    progress?.Report(BlobDownloadProgress.Update(
                        session.BlobName,
                        session.TotalBytes,
                        currentBytes,
                        bytesPerSecond,
                        session.RetryCount));

                    lastProgressReport = now;
                }
            });

            if (startOffset > 0)
            {
                var downloadOptions = new BlobDownloadOptions
                {
                    Range = new Azure.HttpRange(offset: startOffset, length: session.TotalBytes - startOffset)
                };
                var response = await blobClient.DownloadStreamingAsync(downloadOptions, cancellationToken);
                await response.Value.Content.CopyToAsync(progressStream, cancellationToken);
            }
            else
            {
                var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
                await response.Value.Content.CopyToAsync(progressStream, cancellationToken);
            }

            var finalFileInfo = new FileInfo(session.LocalFilePath);
            var totalBytesDownloaded = finalFileInfo.Length;

            _logger.LogInformation("Successfully downloaded blob: {BlobName} ({BytesDownloaded} bytes)",
                session.BlobName, totalBytesDownloaded);

            return new DownloadResult(
                BlobName: session.BlobName,
                LocalFilePath: session.LocalFilePath,
                BytesDownloaded: totalBytesDownloaded,
                Success: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download blob: {BlobName}", session.BlobName);

            var currentBytes = File.Exists(session.LocalFilePath)
                ? new FileInfo(session.LocalFilePath).Length
                : 0;

            return new DownloadResult(
                BlobName: session.BlobName,
                LocalFilePath: session.LocalFilePath,
                BytesDownloaded: currentBytes,
                Success: false,
                Error: ex.Message);
        }
    }
}