namespace AzStore.Core.Models;

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