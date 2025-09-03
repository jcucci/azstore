namespace AzStore.Terminal.Navigation;

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

