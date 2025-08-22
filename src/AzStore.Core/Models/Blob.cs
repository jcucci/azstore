using System.ComponentModel.DataAnnotations;
using Azure.Storage.Blobs.Models;
using AzureBlobType = Azure.Storage.Blobs.Models.BlobType;

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
    public static Blob Create(string name, string path, string containerName, BlobType blobType = BlobType.BlockBlob, long? size = null) =>
        new()
        {
            Name = name,
            Path = path,
            ContainerName = containerName,
            BlobType = blobType,
            Size = size
        };

    /// <summary>
    /// Creates a Blob instance from an Azure SDK BlobItem.
    /// </summary>
    /// <param name="blobItem">The Azure SDK BlobItem.</param>
    /// <param name="containerName">The name of the container.</param>
    /// <returns>A new Blob instance.</returns>
    public static Blob FromBlobItem(BlobItem blobItem, string containerName) =>
        new()
        {
            Name = blobItem.Name,
            Path = blobItem.Name,
            ContainerName = containerName,
            BlobType = MapBlobType(blobItem.Properties.BlobType),
            Size = blobItem.Properties.ContentLength,
            LastModified = blobItem.Properties.LastModified,
            ETag = blobItem.Properties.ETag?.ToString(),
            ContentType = blobItem.Properties.ContentType,
            ContentHash = blobItem.Properties.ContentHash != null ? Convert.ToBase64String(blobItem.Properties.ContentHash) : null,
            Metadata = ConvertMetadata(blobItem.Metadata),
            AccessTier = MapAccessTier(blobItem.Properties.AccessTier?.ToString())
        };

    /// <summary>
    /// Creates a Blob instance from Azure SDK BlobProperties.
    /// </summary>
    /// <param name="blobName">The name of the blob.</param>
    /// <param name="properties">The Azure SDK BlobProperties.</param>
    /// <param name="containerName">The name of the container.</param>
    /// <returns>A new Blob instance.</returns>
    public static Blob FromBlobProperties(string blobName, BlobProperties properties, string containerName) =>
        new()
        {
            Name = blobName,
            Path = blobName,
            ContainerName = containerName,
            BlobType = MapBlobType(properties.BlobType),
            Size = properties.ContentLength,
            LastModified = properties.LastModified,
            ETag = properties.ETag.ToString(),
            ContentType = properties.ContentType,
            ContentHash = properties.ContentHash != null ? Convert.ToBase64String(properties.ContentHash) : null,
            Metadata = ConvertMetadata(properties.Metadata),
            AccessTier = MapAccessTier(properties.AccessTier?.ToString())
        };

    /// <summary>
    /// Gets the file extension of the blob based on its name.
    /// </summary>
    /// <returns>The file extension including the dot, or empty string if no extension.</returns>
    public string GetExtension() => System.IO.Path.GetExtension(Name);

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

    /// <summary>
    /// Maps Azure SDK BlobType to the application's BlobType enum.
    /// </summary>
    /// <param name="azureBlobType">The Azure SDK BlobType.</param>
    /// <returns>The corresponding application BlobType enum value.</returns>
    private static BlobType MapBlobType(AzureBlobType? azureBlobType) =>
        azureBlobType switch
        {
            AzureBlobType.Block => BlobType.BlockBlob,
            AzureBlobType.Page => BlobType.PageBlob,
            AzureBlobType.Append => BlobType.AppendBlob,
            _ => BlobType.BlockBlob // Default to BlockBlob for unknown types
        };

    /// <summary>
    /// Maps Azure SDK access tier string to the application's BlobAccessTier enum.
    /// </summary>
    /// <param name="accessTier">The access tier string from Azure SDK.</param>
    /// <returns>The corresponding BlobAccessTier enum value.</returns>
    private static BlobAccessTier MapAccessTier(string? accessTier) =>
        accessTier switch
        {
            "Hot" => BlobAccessTier.Hot,
            "Cool" => BlobAccessTier.Cool,
            "Archive" => BlobAccessTier.Archive,
            _ => BlobAccessTier.Unknown
        };

    /// <summary>
    /// Converts Azure metadata dictionary to a standard dictionary, handling null values.
    /// </summary>
    /// <param name="metadata">The metadata dictionary to convert.</param>
    /// <returns>A converted dictionary or empty dictionary if input is null.</returns>
    private static Dictionary<string, string> ConvertMetadata(IDictionary<string, string>? metadata)
        => metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? [];
}