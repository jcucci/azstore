using Azure.Storage.Blobs;
using AzStore.Core.Models;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace AzStore.Core;

/// <summary>
/// Provides Azure Blob Storage operations using the Azure SDK.
/// </summary>
public class AzureStorageService : IStorageService
{
    private readonly ILogger<AzureStorageService> _logger;
    private readonly IAuthenticationService _authenticationService;
    private readonly object _lock = new();
    private BlobServiceClient? _blobServiceClient;
    private string? _currentStorageAccountName;
    private bool _isConnected;

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
        if (string.IsNullOrWhiteSpace(accountName))
        {
            throw new ArgumentException("Storage account name cannot be null or empty", nameof(accountName));
        }

        _logger.LogDebug("Connecting to storage account: {AccountName}", accountName);

        try
        {
            // Get authentication credential from authentication service
            var authResult = await _authenticationService.GetCurrentAuthenticationAsync(cancellationToken);
            if (authResult == null || !authResult.Success)
            {
                _logger.LogWarning("Cannot connect to storage account: not authenticated with Azure");
                return false;
            }

            // Build storage account URI
            var storageUri = new Uri($"https://{accountName}.blob.core.windows.net");
            
            // Get credential from authentication service
            var credential = _authenticationService.GetCredential();
            if (credential == null)
            {
                _logger.LogWarning("Cannot connect to storage account: no valid credential available");
                return false;
            }
            
            // Create BlobServiceClient
            var blobServiceClient = new BlobServiceClient(storageUri, credential);

            // Validate access by attempting to get account info
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
    public async IAsyncEnumerable<Container> ListContainersAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_isConnected || _blobServiceClient == null)
            {
                throw new InvalidOperationException("Not connected to a storage account. Call ConnectToStorageAccountAsync first.");
            }
        }

        _logger.LogDebug("Listing containers in storage account: {AccountName}", _currentStorageAccountName);

        await foreach (var containerItem in _blobServiceClient.GetBlobContainersAsync(cancellationToken: cancellationToken))
        {
            var container = new Container
            {
                Name = containerItem.Name,
                Path = containerItem.Name,
                LastModified = containerItem.Properties.LastModified,
                ETag = containerItem.Properties.ETag.ToString(),
                Metadata = containerItem.Properties.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>(),
                AccessLevel = ContainerAccessLevel.None, // We'd need to check the actual access level from properties
                HasImmutabilityPolicy = containerItem.Properties.HasImmutabilityPolicy ?? false,
                HasLegalHold = containerItem.Properties.HasLegalHold ?? false
            };
            
            yield return container;
        }

        _logger.LogDebug("Completed listing containers");
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<Blob> ListBlobsAsync(string containerName, string? prefix = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("Container name cannot be null or empty", nameof(containerName));
        }

        lock (_lock)
        {
            if (!_isConnected || _blobServiceClient == null)
            {
                throw new InvalidOperationException("Not connected to a storage account. Call ConnectToStorageAccountAsync first.");
            }
        }

        _logger.LogDebug("Listing blobs in container: {ContainerName} with prefix: {Prefix}", containerName, prefix ?? "(none)");

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        
        await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
        {
            var blob = new Blob
            {
                Name = blobItem.Name,
                Path = blobItem.Name,
                ContainerName = containerName,
                BlobType = BlobType.BlockBlob, // Default, we'd need to check the actual type
                Size = blobItem.Properties.ContentLength,
                LastModified = blobItem.Properties.LastModified,
                ETag = blobItem.Properties.ETag?.ToString(),
                ContentType = blobItem.Properties.ContentType,
                ContentHash = blobItem.Properties.ContentHash != null ? Convert.ToBase64String(blobItem.Properties.ContentHash) : null,
                Metadata = blobItem.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>(),
                AccessTier = blobItem.Properties.AccessTier?.ToString() switch
                {
                    "Hot" => BlobAccessTier.Hot,
                    "Cool" => BlobAccessTier.Cool,
                    "Archive" => BlobAccessTier.Archive,
                    _ => BlobAccessTier.Unknown
                }
            };
            
            yield return blob;
        }

        _logger.LogDebug("Completed listing blobs in container: {ContainerName}", containerName);
    }

    /// <inheritdoc/>
    public async Task<Blob?> GetBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("Container name cannot be null or empty", nameof(containerName));
        }

        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new ArgumentException("Blob name cannot be null or empty", nameof(blobName));
        }

        lock (_lock)
        {
            if (!_isConnected || _blobServiceClient == null)
            {
                throw new InvalidOperationException("Not connected to a storage account. Call ConnectToStorageAccountAsync first.");
            }
        }

        _logger.LogDebug("Getting blob details: {BlobName} in container: {ContainerName}", blobName, containerName);

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Check if blob exists
            var exists = await blobClient.ExistsAsync(cancellationToken);
            if (!exists.Value)
            {
                _logger.LogDebug("Blob not found: {BlobName} in container: {ContainerName}", blobName, containerName);
                return null;
            }

            // Get blob properties
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            var blob = new Blob
            {
                Name = blobName,
                Path = blobName,
                ContainerName = containerName,
                BlobType = BlobType.BlockBlob, // Default, we'd need to check the actual type
                Size = properties.Value.ContentLength,
                LastModified = properties.Value.LastModified,
                ETag = properties.Value.ETag.ToString(),
                ContentType = properties.Value.ContentType,
                ContentHash = properties.Value.ContentHash != null ? Convert.ToBase64String(properties.Value.ContentHash) : null,
                Metadata = properties.Value.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>(),
                AccessTier = properties.Value.AccessTier?.ToString() switch
                {
                    "Hot" => BlobAccessTier.Hot,
                    "Cool" => BlobAccessTier.Cool,
                    "Archive" => BlobAccessTier.Archive,
                    _ => BlobAccessTier.Unknown
                }
            };

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
        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("Container name cannot be null or empty", nameof(containerName));
        }

        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new ArgumentException("Blob name cannot be null or empty", nameof(blobName));
        }

        if (string.IsNullOrWhiteSpace(localFilePath))
        {
            throw new ArgumentException("Local file path cannot be null or empty", nameof(localFilePath));
        }

        lock (_lock)
        {
            if (!_isConnected || _blobServiceClient == null)
            {
                throw new InvalidOperationException("Not connected to a storage account. Call ConnectToStorageAccountAsync first.");
            }
        }

        _logger.LogDebug("Downloading blob: {BlobName} from container: {ContainerName} to: {LocalFilePath}", blobName, containerName, localFilePath);

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Check if blob exists
            var exists = await blobClient.ExistsAsync(cancellationToken);
            if (!exists.Value)
            {
                throw new InvalidOperationException($"Blob '{blobName}' does not exist in container '{containerName}'");
            }

            // Check if local file exists and handle overwrite
            if (File.Exists(localFilePath) && !overwrite)
            {
                throw new IOException($"File '{localFilePath}' already exists and overwrite is disabled");
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(localFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogDebug("Created directory: {Directory}", directory);
            }

            // Download the blob
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
    public async IAsyncEnumerable<DownloadResult> DownloadBlobsAsync(string containerName, string localDirectoryPath, string? prefix = null, bool overwrite = false, IProgress<DownloadProgress>? progress = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("Container name cannot be null or empty", nameof(containerName));
        }

        if (string.IsNullOrWhiteSpace(localDirectoryPath))
        {
            throw new ArgumentException("Local directory path cannot be null or empty", nameof(localDirectoryPath));
        }

        lock (_lock)
        {
            if (!_isConnected || _blobServiceClient == null)
            {
                throw new InvalidOperationException("Not connected to a storage account. Call ConnectToStorageAccountAsync first.");
            }
        }

        _logger.LogDebug("Downloading blobs from container: {ContainerName} with prefix: {Prefix} to directory: {LocalDirectoryPath}", 
            containerName, prefix ?? "(none)", localDirectoryPath);

        // Ensure local directory exists
        if (!Directory.Exists(localDirectoryPath))
        {
            Directory.CreateDirectory(localDirectoryPath);
            _logger.LogDebug("Created directory: {Directory}", localDirectoryPath);
        }

        var completedBlobs = 0;
        var totalBytesDownloaded = 0L;

        await foreach (var blob in ListBlobsAsync(containerName, prefix, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            DownloadResult result;
            try
            {
                // Build local file path maintaining blob hierarchy
                var relativePath = blob.Name;
                if (!string.IsNullOrEmpty(prefix) && blob.Name.StartsWith(prefix))
                {
                    relativePath = blob.Name[prefix.Length..].TrimStart('/');
                }

                var localFilePath = Path.Combine(localDirectoryPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
                
                // Skip if file exists and overwrite is disabled
                if (File.Exists(localFilePath) && !overwrite)
                {
                    _logger.LogDebug("Skipping existing file: {LocalFilePath}", localFilePath);
                    result = new DownloadResult(blob.Name, localFilePath, 0, false, "File already exists and overwrite is disabled");
                }
                else
                {
                    // Download the blob
                    var bytesDownloaded = await DownloadBlobAsync(containerName, blob.Name, localFilePath, overwrite, cancellationToken);
                    totalBytesDownloaded += bytesDownloaded;
                    
                    result = new DownloadResult(blob.Name, localFilePath, bytesDownloaded, true);
                    _logger.LogDebug("Downloaded blob: {BlobName} ({BytesDownloaded} bytes)", blob.Name, bytesDownloaded);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to download blob '{blob.Name}': {ex.Message}";
                _logger.LogWarning(ex, "Download failed for blob: {BlobName}", blob.Name);
                result = new DownloadResult(blob.Name, string.Empty, 0, false, errorMessage);
            }

            completedBlobs++;
            progress?.Report(new DownloadProgress(0, completedBlobs, blob.Name, 1.0, totalBytesDownloaded)); // We can't know totals with streaming

            yield return result;
        }

        _logger.LogInformation("Batch download completed: {CompletedBlobs} blobs processed ({TotalBytesDownloaded} bytes)", 
            completedBlobs, totalBytesDownloaded);
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
                SubscriptionId: null, // TODO: Get from authentication service if needed
                ResourceGroupName: null, // TODO: Extract from ARM if needed
                PrimaryEndpoint: _blobServiceClient.Uri
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get storage account info");
            return null;
        }
    }
}