namespace AzStore.Core.Models.Authentication;

/// <summary>
/// Represents the result of an authentication operation.
/// </summary>
/// <param name="Success">Whether the authentication was successful.</param>
/// <param name="AccessToken">The access token if authentication was successful.</param>
/// <param name="SubscriptionId">The Azure subscription ID if available.</param>
/// <param name="TenantId">The Azure tenant ID if available.</param>
/// <param name="AccountName">The authenticated user account name if available.</param>
/// <param name="ExpiresOn">When the authentication expires, if applicable.</param>
/// <param name="Error">Any error message if authentication failed.</param>
public record AuthenticationResult(
    bool Success,
    string? AccessToken = null,
    Guid? SubscriptionId = null,
    Guid? TenantId = null,
    string? AccountName = null,
    DateTime? ExpiresOn = null,
    string? Error = null)
{
    /// <summary>
    /// Creates a successful authentication result.
    /// </summary>
    /// <param name="accessToken">The access token.</param>
    /// <param name="subscriptionId">Optional subscription ID.</param>
    /// <param name="tenantId">Optional tenant ID.</param>
    /// <param name="accountName">Optional account name.</param>
    /// <param name="expiresOn">Optional expiration time.</param>
    /// <returns>A successful AuthenticationResult.</returns>
    public static AuthenticationResult Successful(
        string accessToken,
        Guid? subscriptionId = null,
        Guid? tenantId = null,
        string? accountName = null,
        DateTime? expiresOn = null)
    {
        return new AuthenticationResult(
            Success: true,
            AccessToken: accessToken,
            SubscriptionId: subscriptionId,
            TenantId: tenantId,
            AccountName: accountName,
            ExpiresOn: expiresOn);
    }

    /// <summary>
    /// Creates a failed authentication result.
    /// </summary>
    /// <param name="error">The error message describing the failure.</param>
    /// <returns>A failed AuthenticationResult.</returns>
    public static AuthenticationResult Failed(string error)
    {
        return new AuthenticationResult(Success: false, Error: error);
    }
}