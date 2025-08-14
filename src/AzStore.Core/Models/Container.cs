using System.ComponentModel.DataAnnotations;

namespace AzStore.Core.Models;

/// <summary>
/// Represents an Azure Blob Storage container.
/// </summary>
public class Container : StorageItem
{
    /// <summary>
    /// Gets the public access level of the container.
    /// </summary>
    public ContainerAccessLevel AccessLevel { get; init; }

    /// <summary>
    /// Gets a value indicating whether the container has an immutability policy set.
    /// </summary>
    public bool HasImmutabilityPolicy { get; init; }

    /// <summary>
    /// Gets a value indicating whether the container has a legal hold set.
    /// </summary>
    public bool HasLegalHold { get; init; }

    /// <summary>
    /// Gets the lease state of the container.
    /// </summary>
    public string? LeaseState { get; init; }

    /// <summary>
    /// Gets the lease status of the container.
    /// </summary>
    public string? LeaseStatus { get; init; }

    /// <summary>
    /// Gets the number of blobs in the container, if available.
    /// </summary>
    public int? BlobCount { get; init; }

    /// <summary>
    /// Creates a new Container instance.
    /// </summary>
    /// <param name="name">The name of the container.</param>
    /// <param name="path">The full path or URI of the container.</param>
    /// <param name="accessLevel">The public access level of the container.</param>
    /// <returns>A new Container instance.</returns>
    public static Container Create(string name, string path, ContainerAccessLevel accessLevel = ContainerAccessLevel.None)
    {
        return new Container
        {
            Name = name,
            Path = path,
            AccessLevel = accessLevel
        };
    }

    /// <summary>
    /// Returns a string representation of the container with access level information.
    /// </summary>
    /// <returns>A formatted string containing container details.</returns>
    public override string ToString()
    {
        var accessInfo = AccessLevel != ContainerAccessLevel.None ? $" [{AccessLevel}]" : "";
        var countInfo = BlobCount.HasValue ? $" ({BlobCount} blobs)" : "";
        return $"Container: {Name}{accessInfo}{countInfo}";
    }
}

/// <summary>
/// Defines the public access levels for Azure Blob Storage containers.
/// </summary>
public enum ContainerAccessLevel
{
    /// <summary>
    /// No public access. Authentication is required for all requests.
    /// </summary>
    None,

    /// <summary>
    /// Public read access for blobs only. Blob data can be read anonymously, but container metadata is not available.
    /// </summary>
    Blob,

    /// <summary>
    /// Public read access for containers and blobs. All container and blob data can be read anonymously.
    /// </summary>
    Container
}