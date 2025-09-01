namespace AzStore.Core.Models.Authentication;

/// <summary>
/// Represents an Azure subscription available to the authenticated user.
/// </summary>
/// <param name="Id">The unique identifier of the subscription.</param>
/// <param name="Name">The display name of the subscription.</param>
/// <param name="State">The current state of the subscription (Enabled, Disabled, etc.).</param>
/// <param name="IsDefault">Whether this is the default subscription for the user.</param>
/// <param name="TenantId">The Azure tenant ID that contains this subscription.</param>
public record AzureSubscription(
    Guid Id,
    string Name,
    string State,
    bool IsDefault,
    Guid TenantId)
{
    /// <summary>
    /// Returns a string representation of the Azure subscription.
    /// </summary>
    /// <returns>A formatted string containing subscription details.</returns>
    public override string ToString()
    {
        var defaultIndicator = IsDefault ? " (default)" : "";
        return $"{Name} ({Id}){defaultIndicator}";
    }
}