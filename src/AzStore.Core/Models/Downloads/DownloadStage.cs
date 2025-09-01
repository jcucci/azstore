namespace AzStore.Core.Models.Downloads;

/// <summary>
/// Represents the current stage of a blob download operation.
/// </summary>
public enum DownloadStage
{
    /// <summary>
    /// Download is starting, preparing resources.
    /// </summary>
    Starting,
    
    /// <summary>
    /// Actively downloading blob data.
    /// </summary>
    Downloading,
    
    /// <summary>
    /// Verifying downloaded data integrity.
    /// </summary>
    Verifying,
    
    /// <summary>
    /// Download completed successfully.
    /// </summary>
    Completed,
    
    /// <summary>
    /// Download failed and is being retried.
    /// </summary>
    Retrying,
    
    /// <summary>
    /// Download failed permanently.
    /// </summary>
    Failed
}