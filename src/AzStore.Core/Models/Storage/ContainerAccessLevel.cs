namespace AzStore.Core.Models.Storage;

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

