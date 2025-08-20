using System.ComponentModel.DataAnnotations;

namespace AzStore.Core.Models;

/// <summary>
/// Abstract base class representing an item in Azure Blob Storage.
/// </summary>
public abstract class StorageItem
{
    /// <summary>
    /// Gets the name of the storage item.
    /// </summary>
    [Required, MinLength(1)]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the full path or URI of the storage item.
    /// </summary>
    [Required, MinLength(1)]
    public required string Path { get; init; }

    /// <summary>
    /// Gets the date and time when the storage item was last modified.
    /// </summary>
    public DateTimeOffset? LastModified { get; init; }

    /// <summary>
    /// Gets the size of the storage item in bytes, if applicable.
    /// </summary>
    public long? Size { get; init; }

    /// <summary>
    /// Gets the ETag of the storage item for optimistic concurrency control.
    /// </summary>
    public string? ETag { get; init; }

    /// <summary>
    /// Gets additional metadata associated with the storage item.
    /// </summary>
    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Returns a string representation of the storage item.
    /// </summary>
    /// <returns>A string containing the item's name and path.</returns>
    public override string ToString()
    {
        return $"{GetType().Name}: {Name} ({Path})";
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current storage item.
    /// </summary>
    /// <param name="obj">The object to compare with the current storage item.</param>
    /// <returns>true if the specified object is equal to the current storage item; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is StorageItem other)
        {
            return Path.Equals(other.Path, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(ETag, other.ETag, StringComparison.Ordinal);
        }
        return false;
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code for the current storage item.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            Path.GetHashCode(StringComparison.OrdinalIgnoreCase), 
            ETag?.GetHashCode(StringComparison.Ordinal) ?? 0);
    }
}