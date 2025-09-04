using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using AzStore.Core.Models.Authentication;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AzStore.Core.Services.Abstractions;
using Azure.ResourceManager.Resources;
using Azure;

namespace AzStore.Core.Services.Implementations;

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

            var cliDefault = await TryGetCliDefaultSubscriptionIdAsync(cancellationToken);
            SubscriptionResource subscription;
            if (cliDefault.HasValue && cliDefault.Value != Guid.Empty)
            {
                _logger.LogDebug("Using Azure CLI default subscription {SubscriptionId}", cliDefault);
                var resourceIdentifier = new ResourceIdentifier($"/subscriptions/{cliDefault.Value}");
                subscription = armClient.GetSubscriptionResource(resourceIdentifier);
            }
            else
            {
                _logger.LogDebug("Falling back to ARM default subscription");
                subscription = await armClient.GetDefaultSubscriptionAsync(cancellationToken);
            }
            var subResponse = await subscription.GetAsync(cancellationToken);
            var subData = subResponse.Value.Data;

            var tenantIdText = subData.TenantId?.ToString();
            var result = AuthenticationResult.Successful(
                accessToken: "*** (hidden for security)",
                subscriptionId: subResponse.Value.Id.SubscriptionId != null ? Guid.Parse(subResponse.Value.Id.SubscriptionId) : null,
                tenantId: tenantIdText != null ? Guid.Parse(tenantIdText) : null,
                accountName: subData.DisplayName,
                expiresOn: DateTime.UtcNow.AddHours(1)
            );

            lock (_lock)
            {
                _cachedResult = result;
            }

            _logger.LogInformation("Successfully authenticated. Subscription: {SubscriptionName} ({SubscriptionId})",
                subData.DisplayName, subData.SubscriptionId);

            return result;
        }
        catch (RequestFailedException ex) when (ex.Status == 401 || ex.Status == 403)
        {
            var errorMsg = "Azure CLI authentication failed. Please run 'az login' to authenticate";
            _logger.LogError(ex, "Authentication failed: {Error}", errorMsg);
            return AuthenticationResult.Failed(errorMsg);
        }
        catch (CredentialUnavailableException ex)
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
            var subResponse = await subscription.GetAsync(cancellationToken);
            var subData = subResponse.Value.Data;

            var tenantIdText = subData.TenantId?.ToString();
            var result = AuthenticationResult.Successful(
                accessToken: "*** (hidden for security)",
                subscriptionId: subscriptionId,
                tenantId: tenantIdText != null ? Guid.Parse(tenantIdText) : null,
                accountName: subData.DisplayName,
                expiresOn: DateTime.UtcNow.AddHours(1)
            );

            lock (_lock)
            {
                _cachedResult = result;
            }

            _logger.LogInformation("Successfully authenticated for subscription: {SubscriptionName} ({SubscriptionId})",
                subData.DisplayName, subscriptionId);

            return result;
        }
        catch (RequestFailedException ex) when (ex.Status == 401 || ex.Status == 403)
        {
            var errorMsg = $"Access denied to subscription {subscriptionId}. Please run 'az login' and ensure you have access to this subscription";
            _logger.LogError(ex, "Authentication failed for subscription {SubscriptionId}: {Error}", subscriptionId, errorMsg);
            return AuthenticationResult.Failed(errorMsg);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            var errorMsg = $"Subscription {subscriptionId} not found or not accessible";
            _logger.LogError(ex, "Subscription not found {SubscriptionId}: {Error}", subscriptionId, errorMsg);
            return AuthenticationResult.Failed(errorMsg);
        }
        catch (CredentialUnavailableException ex)
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

        AuthenticationResult? cached;
        lock (_lock)
        {
            cached = _cachedResult;
        }

        // If we have a cached result, ensure it still matches the Azure CLI current default subscription.
        if (cached?.Success == true && cached.ExpiresOn.HasValue && cached.ExpiresOn.Value > DateTime.UtcNow.AddMinutes(5))
        {
            try
            {
                var cliDefault = await TryGetCliDefaultSubscriptionIdAsync(cancellationToken);
                if (cliDefault.HasValue && cliDefault.Value != Guid.Empty && cached.SubscriptionId != cliDefault.Value)
                {
                    _logger.LogInformation("CLI default subscription changed from {Old} to {New}; refreshing authentication", cached.SubscriptionId, cliDefault);
                }
                else
                {
                    _logger.LogDebug("Returning cached authentication result (matches CLI default)");
                    return cached;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to check CLI default subscription; proceeding to refresh auth");
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
                var tenantIdText = subscription.Data.TenantId?.ToString();
                var tenantId = tenantIdText != null ? Guid.Parse(tenantIdText) : Guid.Empty;

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

            // Determine default subscription using Azure CLI default for accuracy
            try
            {
                var cliDefault = await TryGetCliDefaultSubscriptionIdAsync(cancellationToken);
                Guid defaultId;
                if (cliDefault.HasValue && cliDefault.Value != Guid.Empty)
                {
                    defaultId = cliDefault.Value;
                }
                else
                {
                    var defaultSubscription = await armClient.GetDefaultSubscriptionAsync(cancellationToken);
                    defaultId = defaultSubscription.Id.SubscriptionId != null ? Guid.Parse(defaultSubscription.Id.SubscriptionId) : Guid.Empty;
                }

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
        catch (CredentialUnavailableException ex)
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
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogError(ex, "Subscription {SubscriptionId} not found", subscriptionId);
            throw new UnauthorizedAccessException($"Subscription {subscriptionId} not found or not accessible", ex);
        }
        catch (RequestFailedException ex) when (ex.Status == 403)
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
    /// Attempts to read the current Azure CLI default subscription id (`az account show`).
    /// Returns null if unable to determine or CLI is unavailable/unauthenticated.
    /// </summary>
    private async Task<Guid?> TryGetCliDefaultSubscriptionIdAsync(CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "az",
                Arguments = "account show --query id -o tsv",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                return null;
            }

            var text = (output ?? string.Empty).Trim();
            if (Guid.TryParse(text, out var id))
            {
                return id;
            }
        }
        catch
        {
            // Swallow and return null; caller will fallback
        }

        return null;
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
