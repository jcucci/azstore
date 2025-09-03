namespace AzStore.Core.Models.Downloads;

/// <summary>
/// Information used to present and resolve a file conflict.
/// </summary>
public sealed record FileConflictInfo(
    bool LocalExists,
    long? LocalSize,
    DateTimeOffset? LocalLastModifiedUtc,
    string? LocalChecksumMd5,
    long RemoteSize,
    DateTimeOffset? RemoteLastModifiedUtc,
    string? RemoteChecksumMd5);

