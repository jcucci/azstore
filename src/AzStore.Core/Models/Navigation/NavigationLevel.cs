namespace AzStore.Core.Models.Navigation;

/// <summary>
/// Defines the navigation levels within the Azure Blob Storage hierarchy.
/// </summary>
public enum NavigationLevel
{
   /// <summary>
   /// At the storage account level, viewing containers.
   /// </summary>
   StorageAccount,

   /// <summary>
   /// At the container level, viewing blobs and virtual directories.
   /// </summary>
   Container,

   /// <summary>
   /// At a blob prefix level (virtual directory), viewing nested blobs and directories.
   /// </summary>
   BlobPrefix
}