namespace AzStore.Core.Models.Storage;

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