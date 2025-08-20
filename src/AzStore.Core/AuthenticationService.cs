using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using AzStore.Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading;

namespace AzStore.Core;

/// <summary>
/// Provides Azure authentication functionality using Azure CLI integration.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;
    private readonly Lock _lock = new();
    private DefaultAzureCredential? _credential;
    private AuthenticationResult? _cachedResult;
    private ArmClient? _armClient;
    private readonly DateTime _cacheExpiry = DateTime.UtcNow.AddMinutes(30);

    /// <summary>
    /// Initializes a new instance of the AuthenticationService.
    /// </summary>
    /// <param name="logger">Logger instance for this service.</param>
    public AuthenticationService(ILogger<AuthenticationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<AuthenticationResult> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting authentication with Azure CLI");

        try
        {
            await EnsureAzureCliAvailableAsync(cancellationToken);

            var credential = GetCredential();
            var armClient = GetArmClient(credential);

            var subscription = await armClient.GetDefaultSubscriptionAsync(cancellationToken);
            await subscription.GetAsync(cancellationToken);

            var result = AuthenticationResult.Successful(
                accessToken: "*** (hidden for security)",
                subscriptionId: subscription.Id.SubscriptionId != null ? Guid.Parse(subscription.Id.SubscriptionId) : null,
                tenantId: subscription.Data.TenantId?.ToString() != null ? Guid.Parse(subscription.Data.TenantId.ToString()!) : null,
                accountName: subscription.Data.DisplayName,
                expiresOn: DateTime.UtcNow.AddHours(1)
            );

            lock (_lock)
            {
                _cachedResult = result;
            }

            _logger.LogInformation("Successfully authenticated with Azure CLI. Subscription: {SubscriptionName} ({SubscriptionId})",
                subscription.Data.DisplayName, subscription.Data.SubscriptionId);

            return result;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 401 || ex.Status == 403)
        {
            var errorMsg = "Azure CLI authentication failed. Please run 'az login' to authenticate";
            _logger.LogError(ex, "Authentication failed: {Error}", errorMsg);
            return AuthenticationResult.Failed(errorMsg);
        }
        catch (Azure.Identity.CredentialUnavailableException ex)
        {
            var errorMsg = "Azure CLI is not authenticated. Please run 'az login' to authenticate";
            _logger.LogError(ex, "Credential unavailable: {Error}", errorMsg);
            return AuthenticationResult.Failed(errorMsg);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Unexpected error during authentication: {ex.Message}";
            _logger.LogError(ex, "Unexpected authentication error: {Error}", errorMsg);
            return AuthenticationResult.Failed(errorMsg);
        }
    }

    /// <inheritdoc/>
    public async Task<AuthenticationResult> AuthenticateAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        if (subscriptionId == Guid.Empty)
        {
            throw new ArgumentException("Subscription ID cannot be empty", nameof(subscriptionId));
        }

        _logger.LogDebug("Starting authentication with Azure CLI for subscription {SubscriptionId}", subscriptionId);

        try
        {
            await EnsureAzureCliAvailableAsync(cancellationToken);

            var credential = GetCredential();
            var armClient = GetArmClient(credential);

            var resourceIdentifier = new ResourceIdentifier($"/subscriptions/{subscriptionId}");
            var subscription = armClient.GetSubscriptionResource(resourceIdentifier);
            await subscription.GetAsync(cancellationToken);

            var result = AuthenticationResult.Successful(
                accessToken: "*** (hidden for security)",
                subscriptionId: subscriptionId,
                tenantId: subscription.Data.TenantId?.ToString() != null ? Guid.Parse(subscription.Data.TenantId.ToString()!) : null,
                accountName: subscription.Data.DisplayName,
                expiresOn: DateTime.UtcNow.AddHours(1)
            );

            lock (_lock)
            {
                _cachedResult = result;
            }

            _logger.LogInformation("Successfully authenticated with Azure CLI for subscription: {SubscriptionName} ({SubscriptionId})",
                subscription.Data.DisplayName, subscriptionId);

            return result;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 401 || ex.Status == 403)
        {
            var errorMsg = $"Access denied to subscription {subscriptionId}. Please run 'az login' and ensure you have access to this subscription";
            _logger.LogError(ex, "Authentication failed for subscription {SubscriptionId}: {Error}", subscriptionId, errorMsg);
            return AuthenticationResult.Failed(errorMsg);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            var errorMsg = $"Subscription {subscriptionId} not found or not accessible";
            _logger.LogError(ex, "Subscription not found {SubscriptionId}: {Error}", subscriptionId, errorMsg);
            return AuthenticationResult.Failed(errorMsg);
        }
        catch (Azure.Identity.CredentialUnavailableException ex)
        {
            var errorMsg = "Azure CLI is not authenticated. Please run 'az login' to authenticate";
            _logger.LogError(ex, "Credential unavailable for subscription {SubscriptionId}: {Error}", subscriptionId, errorMsg);
            return AuthenticationResult.Failed(errorMsg);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Unexpected error during authentication for subscription {subscriptionId}: {ex.Message}";
            _logger.LogError(ex, "Unexpected authentication error for subscription {SubscriptionId}: {Error}", subscriptionId, errorMsg);
            return AuthenticationResult.Failed(errorMsg);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking authentication status");

        try
        {
            lock (_lock)
            {
                if (_cachedResult?.Success == true &&
                    _cachedResult.ExpiresOn.HasValue &&
                    _cachedResult.ExpiresOn.Value > DateTime.UtcNow.AddMinutes(5))
                {
                    _logger.LogDebug("Using cached authentication result");
                    return true;
                }
            }

            var credential = GetCredential();
            var armClient = GetArmClient(credential);
            var subscription = await armClient.GetDefaultSubscriptionAsync(cancellationToken);
            await subscription.GetAsync(cancellationToken);

            _logger.LogDebug("Authentication check successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Authentication check failed: {Error}", ex.Message);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<AuthenticationResult?> GetCurrentAuthenticationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting current authentication information");

        lock (_lock)
        {
            if (_cachedResult?.Success == true &&
                _cachedResult.ExpiresOn.HasValue &&
                _cachedResult.ExpiresOn.Value > DateTime.UtcNow.AddMinutes(5))
            {
                _logger.LogDebug("Returning cached authentication result");
                return _cachedResult;
            }
        }

        var isAuthenticated = await IsAuthenticatedAsync(cancellationToken);
        if (!isAuthenticated)
        {
            _logger.LogDebug("No current authentication available");
            return null;
        }

        // Perform fresh authentication to get current details
        return await AuthenticateAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AzureSubscription>> GetAvailableSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting available Azure subscriptions");

        try
        {
            var credential = GetCredential();
            var armClient = GetArmClient(credential);

            var subscriptions = new List<AzureSubscription>();

            await foreach (var subscription in armClient.GetSubscriptions().GetAllAsync(cancellationToken))
            {
                var subscriptionId = subscription.Id.SubscriptionId != null ? Guid.Parse(subscription.Id.SubscriptionId) : Guid.Empty;
                var tenantId = subscription.Data.TenantId?.ToString() != null ? Guid.Parse(subscription.Data.TenantId.ToString()!) : Guid.Empty;

                if (subscriptionId != Guid.Empty)
                {
                    subscriptions.Add(new AzureSubscription(
                        Id: subscriptionId,
                        Name: subscription.Data.DisplayName ?? "Unknown",
                        State: subscription.Data.State?.ToString() ?? "Unknown",
                        IsDefault: false, // We'll determine this separately if needed
                        TenantId: tenantId
                    ));
                }
            }

            // Try to determine which is the default subscription
            try
            {
                var defaultSubscription = await armClient.GetDefaultSubscriptionAsync(cancellationToken);
                var defaultId = defaultSubscription.Id.SubscriptionId != null ? Guid.Parse(defaultSubscription.Id.SubscriptionId) : Guid.Empty;

                for (int i = 0; i < subscriptions.Count; i++)
                {
                    if (subscriptions[i].Id == defaultId)
                    {
                        subscriptions[i] = subscriptions[i] with { IsDefault = true };
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not determine default subscription");
            }

            _logger.LogInformation("Found {Count} available subscriptions", subscriptions.Count);
            return subscriptions;
        }
        catch (Azure.Identity.CredentialUnavailableException ex)
        {
            _logger.LogError(ex, "Azure CLI credentials unavailable when getting subscriptions");
            throw new UnauthorizedAccessException("Not authenticated with Azure CLI. Please run 'az login'", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available subscriptions: {Error}", ex.Message);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<StorageAccountInfo>> GetStorageAccountsAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        if (subscriptionId == Guid.Empty)
        {
            throw new ArgumentException("Subscription ID cannot be empty", nameof(subscriptionId));
        }

        _logger.LogDebug("Getting storage accounts for subscription {SubscriptionId}", subscriptionId);

        try
        {
            var credential = GetCredential();
            var armClient = GetArmClient(credential);

            var resourceIdentifier = new ResourceIdentifier($"/subscriptions/{subscriptionId}");
            var subscription = armClient.GetSubscriptionResource(resourceIdentifier);

            var storageAccounts = new List<StorageAccountInfo>();

            await foreach (var storageAccount in subscription.GetStorageAccountsAsync(cancellationToken))
            {
                var endpoints = storageAccount.Data.PrimaryEndpoints;
                var blobEndpoint = endpoints?.BlobUri;

                if (blobEndpoint != null)
                {
                    storageAccounts.Add(new StorageAccountInfo(
                        AccountName: storageAccount.Data.Name,
                        AccountKind: storageAccount.Data.Kind?.ToString(),
                        SubscriptionId: subscriptionId,
                        ResourceGroupName: storageAccount.Id.ResourceGroupName,
                        PrimaryEndpoint: blobEndpoint
                    ));
                }
            }

            _logger.LogInformation("Found {Count} storage accounts in subscription {SubscriptionId}",
                storageAccounts.Count, subscriptionId);

            return storageAccounts;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogError(ex, "Subscription {SubscriptionId} not found", subscriptionId);
            throw new UnauthorizedAccessException($"Subscription {subscriptionId} not found or not accessible", ex);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 403)
        {
            _logger.LogError(ex, "Access denied to subscription {SubscriptionId}", subscriptionId);
            throw new UnauthorizedAccessException($"Access denied to subscription {subscriptionId}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting storage accounts for subscription {SubscriptionId}: {Error}", subscriptionId, ex.Message);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<AuthenticationResult?> RefreshAuthenticationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Refreshing authentication");

        // Clear cached credential to force refresh
        lock (_lock)
        {
            _credential = null;
            _cachedResult = null;
        }

        // Perform fresh authentication
        var result = await AuthenticateAsync(cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation("Authentication refreshed successfully");
            return result;
        }
        else
        {
            _logger.LogWarning("Authentication refresh failed: {Error}", result.Error);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task ClearAuthenticationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Clearing authentication cache");

        lock (_lock)
        {
            _credential = null;
            _cachedResult = null;
            _armClient = null;
        }

        _logger.LogInformation("Authentication cache cleared");
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<bool> IsAzureCliAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "az",
                Arguments = "--version",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogDebug("Could not start Azure CLI process");
                return false;
            }

            await process.WaitForExitAsync(cancellationToken);
            var isAvailable = process.ExitCode == 0;

            _logger.LogDebug("Azure CLI availability check: {IsAvailable}", isAvailable);
            return isAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Azure CLI availability check failed: {Error}", ex.Message);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetAzureCliVersionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "az",
                Arguments = "--version",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogDebug("Could not start Azure CLI process for version check");
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
            {
                // Extract version from first line (typically "azure-cli 2.x.x")
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 0)
                {
                    var version = lines[0].Trim();
                    _logger.LogDebug("Azure CLI version: {Version}", version);
                    return version;
                }
            }

            _logger.LogDebug("Could not parse Azure CLI version from output");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Azure CLI version check failed: {Error}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Gets or creates the DefaultAzureCredential configured to use only AzureCliCredential.
    /// </summary>
    /// <returns>The configured credential instance.</returns>
    /// <inheritdoc/>
    public TokenCredential GetCredential()
    {
        if (_credential == null)
        {
            lock (_lock)
            {
                if (_credential == null)
                {
                    var options = new DefaultAzureCredentialOptions
                    {
                        ExcludeEnvironmentCredential = true,
                        ExcludeWorkloadIdentityCredential = true,
                        ExcludeManagedIdentityCredential = true,
                        ExcludeVisualStudioCredential = true,
                        ExcludeVisualStudioCodeCredential = true,
                        ExcludeAzurePowerShellCredential = true,
                        ExcludeAzureDeveloperCliCredential = true,
                        ExcludeInteractiveBrowserCredential = true
                        // Only AzureCliCredential remains enabled
                    };

                    _credential = new DefaultAzureCredential(options);
                    _logger.LogDebug("Created DefaultAzureCredential configured for Azure CLI only");
                }
            }
        }

        return _credential;
    }

    /// <summary>
    /// Gets or creates the ArmClient for Azure Resource Manager operations.
    /// </summary>
    /// <param name="credential">The credential to use for authentication.</param>
    /// <returns>The configured ArmClient instance.</returns>
    private ArmClient GetArmClient(TokenCredential credential)
    {
        if (_armClient == null)
        {
            lock (_lock)
            {
                if (_armClient == null)
                {
                    _armClient = new ArmClient(credential);
                    _logger.LogDebug("Created ArmClient for Azure Resource Manager operations");
                }
            }
        }

        return _armClient;
    }

    /// <summary>
    /// Ensures Azure CLI is available and throws an appropriate exception if not.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the availability check.</returns>
    /// <exception cref="InvalidOperationException">Thrown when Azure CLI is not available.</exception>
    private async Task EnsureAzureCliAvailableAsync(CancellationToken cancellationToken)
    {
        var isAvailable = await IsAzureCliAvailableAsync(cancellationToken);
        if (!isAvailable)
        {
            var errorMsg = "Azure CLI is not installed or not available in PATH. Please install Azure CLI and ensure it's accessible.";
            _logger.LogError("Azure CLI availability check failed: {Error}", errorMsg);
            throw new InvalidOperationException(errorMsg);
        }
    }

    /// <summary>
    /// Disposes of the authentication service and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            _credential = null;
            _cachedResult = null;
            _armClient = null;
        }
    }
}