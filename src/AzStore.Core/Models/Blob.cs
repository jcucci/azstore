using System.ComponentModel.DataAnnotations;

namespace AzStore.Core.Models;

/// <summary>
/// Represents an Azure Blob Storage blob (file).
/// </summary>
public class Blob : StorageItem
{
    /// <summary>
    /// Gets the type of the blob (Block, Page, or Append).
    /// </summary>
    [Required]
    public BlobType BlobType { get; init; }

    /// <summary>
    /// Gets the content type (MIME type) of the blob.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets the content encoding of the blob.
    /// </summary>
    public string? ContentEncoding { get; init; }

    /// <summary>
    /// Gets the content language of the blob.
    /// </summary>
    public string? ContentLanguage { get; init; }

    /// <summary>
    /// Gets the MD5 hash of the blob content.
    /// </summary>
    public string? ContentHash { get; init; }

    /// <summary>
    /// Gets the cache control header value for the blob.
    /// </summary>
    public string? CacheControl { get; init; }

    /// <summary>
    /// Gets the content disposition header value for the blob.
    /// </summary>
    public string? ContentDisposition { get; init; }

    /// <summary>
    /// Gets the access tier of the blob (Hot, Cool, Archive).
    /// </summary>
    public BlobAccessTier? AccessTier { get; init; }

    /// <summary>
    /// Gets a value indicating whether the access tier is inferred.
    /// </summary>
    public bool? AccessTierInferred { get; init; }

    /// <summary>
    /// Gets the time when the access tier was changed.
    /// </summary>
    public DateTime? AccessTierChangedOn { get; init; }

    /// <summary>
    /// Gets the lease state of the blob.
    /// </summary>
    public string? LeaseState { get; init; }

    /// <summary>
    /// Gets the lease status of the blob.
    /// </summary>
    public string? LeaseStatus { get; init; }

    /// <summary>
    /// Gets the name of the container that contains this blob.
    /// </summary>
    [Required, MinLength(1)]
    public required string ContainerName { get; init; }

    /// <summary>
    /// Creates a new Blob instance.
    /// </summary>
    /// <param name="name">The name of the blob.</param>
    /// <param name="path">The full path or URI of the blob.</param>
    /// <param name="containerName">The name of the container containing the blob.</param>
    /// <param name="blobType">The type of the blob.</param>
    /// <param name="size">The size of the blob in bytes.</param>
    /// <returns>A new Blob instance.</returns>
    public static Blob Create(string name, string path, string containerName, BlobType blobType = BlobType.BlockBlob, long? size = null)
    {
        return new Blob
        {
            Name = name,
            Path = path,
            ContainerName = containerName,
            BlobType = blobType,
            Size = size
        };
    }

    /// <summary>
    /// Gets the file extension of the blob based on its name.
    /// </summary>
    /// <returns>The file extension including the dot, or empty string if no extension.</returns>
    public string GetExtension()
    {
        return System.IO.Path.GetExtension(Name);
    }

    /// <summary>
    /// Gets a human-readable representation of the blob size.
    /// </summary>
    /// <returns>A formatted size string (e.g., "1.5 MB") or "Unknown" if size is not available.</returns>
    public string GetFormattedSize()
    {
        if (!Size.HasValue)
            return "Unknown";

        var size = Size.Value;
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        int unitIndex = 0;
        double sizeDouble = size;

        while (sizeDouble >= 1024 && unitIndex < units.Length - 1)
        {
            sizeDouble /= 1024;
            unitIndex++;
        }

        return $"{sizeDouble:F1} {units[unitIndex]}";
    }

    /// <summary>
    /// Returns a string representation of the blob with size and type information.
    /// </summary>
    /// <returns>A formatted string containing blob details.</returns>
    public override string ToString()
    {
        var sizeInfo = Size.HasValue ? $" ({GetFormattedSize()})" : "";
        var tierInfo = AccessTier.HasValue ? $" [{AccessTier}]" : "";
        return $"Blob: {Name}{sizeInfo}{tierInfo}";
    }
}

/// <summary>
/// Defines the types of blobs available in Azure Blob Storage.
/// </summary>
public enum BlobType
{
    /// <summary>
    /// A blob comprised of blocks, optimized for streaming and storing cloud objects.
    /// </summary>
    BlockBlob,

    /// <summary>
    /// A blob comprised of pages, optimized for random read/write operations.
    /// </summary>
    PageBlob,

    /// <summary>
    /// A blob optimized for append operations, ideal for logging scenarios.
    /// </summary>
    AppendBlob
}

/// <summary>
/// Defines the access tiers available for Azure Blob Storage.
/// </summary>
public enum BlobAccessTier
{
    /// <summary>
    /// Unknown or unspecified access tier.
    /// </summary>
    Unknown,

    /// <summary>
    /// Hot tier - optimized for frequent access of objects.
    /// </summary>
    Hot,

    /// <summary>
    /// Cool tier - optimized for storing data that is infrequently accessed and stored for at least 30 days.
    /// </summary>
    Cool,

    /// <summary>
    /// Archive tier - optimized for data that can tolerate several hours of retrieval latency and will remain in the Archive tier for at least 180 days.
    /// </summary>
    Archive
}