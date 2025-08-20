using Azure.Core;
using AzStore.Core.Models;

namespace AzStore.Core;

/// <summary>
/// Provides Azure authentication functionality using Azure CLI integration.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Attempts to authenticate using the current Azure CLI session.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An authentication result indicating success or failure.</returns>
    /// <exception cref="InvalidOperationException">Thrown when Azure CLI is not installed or not in PATH.</exception>
    Task<AuthenticationResult> AuthenticateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to authenticate for a specific Azure subscription.
    /// </summary>
    /// <param name="subscriptionId">The Azure subscription ID to authenticate for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An authentication result indicating success or failure.</returns>
    /// <exception cref="ArgumentException">Thrown when subscriptionId is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when Azure CLI is not installed or not in PATH.</exception>
    Task<AuthenticationResult> AuthenticateAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current authentication is still valid.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>true if authentication is valid; otherwise, false.</returns>
    Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently authenticated account information.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The current authentication result, or null if not authenticated.</returns>
    Task<AuthenticationResult?> GetCurrentAuthenticationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of available Azure subscriptions for the authenticated user.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A collection of available Azure subscriptions.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when not authenticated or authentication has expired.</exception>
    Task<IEnumerable<AzureSubscription>> GetAvailableSubscriptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of storage accounts available in the specified subscription.
    /// </summary>
    /// <param name="subscriptionId">The Azure subscription ID to query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A collection of storage accounts in the subscription.</returns>
    /// <exception cref="ArgumentException">Thrown when subscriptionId is empty.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when not authenticated or access is denied.</exception>
    Task<IEnumerable<StorageAccountInfo>> GetStorageAccountsAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the current authentication token if it's close to expiring.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An updated authentication result, or null if refresh is not needed.</returns>
    Task<AuthenticationResult?> RefreshAuthenticationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears any cached authentication information and forces re-authentication.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task ClearAuthenticationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if Azure CLI is installed and available.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>true if Azure CLI is available; otherwise, false.</returns>
    Task<bool> IsAzureCliAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the version of the installed Azure CLI.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The Azure CLI version string, or null if not available.</returns>
    Task<string?> GetAzureCliVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the Azure credential for direct use with Azure SDK clients.
    /// </summary>
    /// <returns>The TokenCredential for authentication, or null if not authenticated.</returns>
    /// <exception cref="InvalidOperationException">Thrown when authentication is not available.</exception>
    TokenCredential? GetCredential();
}