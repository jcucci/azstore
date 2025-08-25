namespace AzStore.Core.Models;

/// <summary>
/// Configuration options for blob download operations.
/// </summary>
/// <param name="ConflictResolution">How to handle file conflicts when downloading.</param>
/// <param name="CreateDirectories">Whether to create local directory structure mirroring blob paths.</param>
/// <param name="VerifyChecksum">Whether to verify download integrity using checksums.</param>
/// <param name="BandwidthLimitBytesPerSecond">Optional bandwidth limit in bytes per second. Null for no limit.</param>
/// <param name="MaxRetryAttempts">Maximum number of retry attempts for failed downloads.</param>
/// <param name="TimeoutSeconds">Timeout in seconds for individual download operations.</param>
/// <param name="EnableResumption">Whether to support resuming interrupted downloads.</param>
/// <param name="BufferSize">Buffer size in bytes for download operations.</param>
public record DownloadOptions(
    ConflictResolution ConflictResolution = ConflictResolution.Ask,
    bool CreateDirectories = true,
    bool VerifyChecksum = true,
    long? BandwidthLimitBytesPerSecond = null,
    int MaxRetryAttempts = 3,
    int TimeoutSeconds = 300,
    bool EnableResumption = true,
    int BufferSize = 8192)
{
    /// <summary>
    /// Gets the default download options.
    /// </summary>
    public static DownloadOptions Default => new();
}