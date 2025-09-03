using AzStore.Core.Models.Downloads;

namespace AzStore.Core.Services.Abstractions;

/// <summary>
/// Resolves file conflicts during downloads when a local file already exists.
/// </summary>
public interface IFileConflictResolver
{
    /// <summary>
    /// Resolve a conflict for the given local path and remote blob metadata.
    /// </summary>
    /// <param name="desiredPath">The intended local file path for the download.</param>
    /// <param name="mode">The preferred resolution mode (Overwrite, Skip, Rename, Ask).</param>
    /// <param name="info">Details for local and remote files to aid resolution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A decision with final path or an instruction to skip.</returns>
    Task<FileConflictDecision> ResolveAsync(string desiredPath, ConflictResolution mode, FileConflictInfo info, CancellationToken cancellationToken = default);
}

