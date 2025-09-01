namespace AzStore.Terminal.Navigation;

/// <summary>
/// Represents an item in the navigation interface for browsing storage accounts, containers, and blobs.
/// </summary>
public class NavigationItem
{
    /// <summary>
    /// Gets the name of the navigation item.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the type of the navigation item.
    /// </summary>
    public NavigationItemType Type { get; }

    /// <summary>
    /// Gets the size of the item in bytes, if applicable.
    /// </summary>
    public long? Size { get; }

    /// <summary>
    /// Gets the last modified date of the item, if applicable.
    /// </summary>
    public DateTimeOffset? LastModified { get; }

    /// <summary>
    /// Gets a value indicating whether the item has public access.
    /// </summary>
    public bool IsPublic { get; }

    /// <summary>
    /// Gets the full path of the item.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Initializes a new instance of the NavigationItem class.
    /// </summary>
    /// <param name="name">The name of the item.</param>
    /// <param name="type">The type of the item.</param>
    /// <param name="size">The size of the item in bytes.</param>
    /// <param name="lastModified">The last modified date of the item.</param>
    /// <param name="isPublic">Whether the item has public access.</param>
    /// <param name="path">The full path of the item.</param>
    public NavigationItem(string name, NavigationItemType type, long? size = null, 
        DateTimeOffset? lastModified = null, bool isPublic = false, string? path = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type;
        Size = size;
        LastModified = lastModified;
        IsPublic = isPublic;
        Path = path ?? name;
    }
}

/// <summary>
/// Defines the types of items that can be displayed in the navigation interface.
/// </summary>
public enum NavigationItemType
{
    /// <summary>
    /// A container in the storage account.
    /// </summary>
    Container,

    /// <summary>
    /// A blob file.
    /// </summary>
    BlobFile,

    /// <summary>
    /// A virtual directory (blob prefix).
    /// </summary>
    BlobPrefix
}