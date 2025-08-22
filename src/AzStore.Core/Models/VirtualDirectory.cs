using System.ComponentModel.DataAnnotations;

namespace AzStore.Core.Models;

/// <summary>
/// Represents a virtual directory in Azure Blob Storage created through blob prefixes.
/// Virtual directories don't exist as actual objects but are simulated using blob naming conventions with delimiters.
/// </summary>
public class VirtualDirectory : StorageItem
{
    /// <summary>
    /// Gets the blob prefix that defines this virtual directory.
    /// </summary>
    [Required, MinLength(1)]
    public required string Prefix { get; init; }

    /// <summary>
    /// Gets the name of the container that contains this virtual directory.
    /// </summary>
    [Required, MinLength(1)]
    public required string ContainerName { get; init; }

    /// <summary>
    /// Gets the estimated number of items (blobs and subdirectories) within this virtual directory.
    /// This may be null if the count is not available or not calculated.
    /// </summary>
    public int? ItemCount { get; init; }

    /// <summary>
    /// Gets the depth level of this virtual directory within the container.
    /// Root level directories have depth 1, subdirectories have depth 2, etc.
    /// </summary>
    public int Depth { get; init; }

    /// <summary>
    /// Creates a new VirtualDirectory instance from a blob prefix.
    /// </summary>
    /// <param name="prefix">The blob prefix that defines this virtual directory.</param>
    /// <param name="containerName">The name of the container containing this virtual directory.</param>
    /// <param name="itemCount">Optional count of items within this directory.</param>
    /// <returns>A new VirtualDirectory instance.</returns>
    public static VirtualDirectory Create(string prefix, string containerName, int? itemCount = null)
    {
        var trimmedPrefix = prefix.TrimEnd('/');
        var name = trimmedPrefix.Contains('/') 
            ? trimmedPrefix.Split('/').Last()
            : trimmedPrefix;
            
        var depth = trimmedPrefix.Split('/').Length;
        var path = $"{containerName}/{prefix}";

        return new VirtualDirectory
        {
            Name = name,
            Path = path,
            Prefix = prefix,
            ContainerName = containerName,
            ItemCount = itemCount,
            Depth = depth,
            LastModified = null, // Virtual directories don't have modification dates
            Size = null // Virtual directories don't have sizes
        };
    }

    /// <summary>
    /// Gets the parent directory prefix, or null if this is a root-level directory.
    /// </summary>
    /// <returns>The parent directory prefix or null.</returns>
    public string? GetParentPrefix()
    {
        var trimmedPrefix = Prefix.TrimEnd('/');
        var lastSlashIndex = trimmedPrefix.LastIndexOf('/');
        
        return lastSlashIndex > 0 ? trimmedPrefix[..lastSlashIndex] + "/" : null;
    }

    /// <summary>
    /// Gets the full path segments from the container root to this directory.
    /// </summary>
    /// <returns>An array of path segments.</returns>
    public string[] GetPathSegments()
    {
        var segments = new List<string> { ContainerName };
        var trimmedPrefix = Prefix.TrimEnd('/');
        
        if (!string.IsNullOrEmpty(trimmedPrefix))
        {
            segments.AddRange(trimmedPrefix.Split('/'));
        }
        
        return [.. segments];
    }

    /// <summary>
    /// Determines if this virtual directory is a direct child of the specified prefix.
    /// </summary>
    /// <param name="parentPrefix">The potential parent prefix to check.</param>
    /// <returns>true if this directory is a direct child of the specified prefix.</returns>
    public bool IsDirectChildOf(string? parentPrefix)
    {
        if (string.IsNullOrEmpty(parentPrefix))
        {
            return Depth == 1; // Root level directory
        }

        var normalizedParent = parentPrefix.TrimEnd('/') + "/";
        return Prefix.StartsWith(normalizedParent, StringComparison.OrdinalIgnoreCase) &&
               CountSlashes(Prefix) == CountSlashes(normalizedParent) + 1;
    }

    /// <summary>
    /// Returns a string representation of the virtual directory with item count information.
    /// </summary>
    /// <returns>A formatted string containing directory details.</returns>
    public override string ToString()
    {
        var itemInfo = ItemCount.HasValue ? $" ({ItemCount} items)" : "";
        return $"Directory: {Name}{itemInfo}";
    }

    private static int CountSlashes(string text)
    {
        var count = 0;
        foreach (char c in text)
        {
            if (c == '/')
                count++;
        }
        return count;
    }
}