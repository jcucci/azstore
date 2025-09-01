using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AzStore.Core.Models.Session;

/// <summary>
/// Represents a user session with Azure Blob Storage context and local directory information.
/// </summary>
/// <param name="Name">The name of the session for user identification.</param>
/// <param name="Directory">The local directory path for downloading files in this session.</param>
/// <param name="StorageAccountName">The Azure Storage account name for this session.</param>
/// <param name="SubscriptionId">The Azure subscription ID associated with this session.</param>
/// <param name="CreatedAt">The timestamp when this session was created.</param>
/// <param name="LastAccessedAt">The timestamp when this session was last accessed.</param>
public record Session(
    [property: Required, MinLength(1)] string Name,
    [property: Required, MinLength(1)] string Directory,
    [property: Required, MinLength(1)] string StorageAccountName,
    [property: Required] Guid SubscriptionId,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt,
    [property: JsonPropertyName("lastAccessedAt")] DateTime LastAccessedAt)
{
    /// <summary>
    /// Creates a new session with the current timestamp for both creation and last access.
    /// </summary>
    /// <param name="name">The name of the session.</param>
    /// <param name="directory">The local directory path.</param>
    /// <param name="storageAccountName">The Azure Storage account name.</param>
    /// <param name="subscriptionId">The Azure subscription ID.</param>
    /// <returns>A new Session instance with current timestamps.</returns>
    public static Session Create(string name, string directory, string storageAccountName, Guid subscriptionId)
    {
        var now = DateTime.UtcNow;
        return new Session(name, directory, storageAccountName, subscriptionId, now, now);
    }

    /// <summary>
    /// Creates a copy of this session with an updated last accessed timestamp.
    /// </summary>
    /// <returns>A new Session instance with the current timestamp as LastAccessedAt.</returns>
    public Session Touch()
    {
        return this with { LastAccessedAt = DateTime.UtcNow };
    }

    /// <summary>
    /// Returns a string representation of this session with key information.
    /// </summary>
    /// <returns>A formatted string containing session details.</returns>
    public override string ToString()
    {
        return $"Session '{Name}' -> {StorageAccountName} ({Directory})";
    }
}