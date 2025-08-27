using AzStore.Core.Exceptions;
using AzStore.Core.Models;

namespace AzStore.Core;

/// <summary>
/// Provides cross-platform path calculation and directory management for Azure blob downloads.
/// </summary>
public interface IPathService
{
    /// <summary>
    /// Calculates the local file path for a blob download within a session.
    /// Creates the directory structure: {sessions_directory}/{session_name}/{container_name}/{blob_virtual_path}
    /// </summary>
    /// <param name="session">The session context for the download.</param>
    /// <param name="containerName">The name of the Azure blob container.</param>
    /// <param name="blobName">The name of the blob (including virtual directory path).</param>
    /// <returns>The sanitized local file path where the blob should be downloaded.</returns>
    /// <exception cref="PathTooLongException">Thrown when the resulting path exceeds platform limits.</exception>
    string CalculateBlobDownloadPath(Session session, string containerName, string blobName);

    /// <summary>
    /// Calculates the local directory path for a container within a session.
    /// Creates the directory structure: {sessions_directory}/{session_name}/{container_name}
    /// </summary>
    /// <param name="session">The session context.</param>
    /// <param name="containerName">The name of the Azure blob container.</param>
    /// <returns>The sanitized local directory path for the container.</returns>
    /// <exception cref="PathTooLongException">Thrown when the resulting path exceeds platform limits.</exception>
    string CalculateContainerDirectoryPath(Session session, string containerName);

    /// <summary>
    /// Sanitizes a path component for cross-platform compatibility.
    /// Removes or replaces invalid characters and handles reserved names.
    /// </summary>
    /// <param name="pathComponent">The path component to sanitize (file name or directory name).</param>
    /// <returns>The sanitized path component safe for all platforms.</returns>
    string SanitizePathComponent(string pathComponent);

    /// <summary>
    /// Creates the directory structure for a local file path if it doesn't exist.
    /// Handles permissions and provides detailed error information.
    /// </summary>
    /// <param name="filePath">The full file path whose directory structure should be created.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if the directory structure exists or was created successfully; false otherwise.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when lacking permissions to create directories.</exception>
    /// <exception cref="DirectoryServiceException">Thrown when directory creation fails for other reasons.</exception>
    Task<bool> EnsureDirectoryExistsAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up empty directories in the path hierarchy up to the session directory.
    /// Used to remove empty directories after failed downloads or deletions.
    /// </summary>
    /// <param name="filePath">The file path whose parent directories should be cleaned up.</param>
    /// <param name="sessionDirectory">The session directory (cleanup stops here).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if cleanup completed successfully; false if errors occurred.</returns>
    Task<bool> CleanupEmptyDirectoriesAsync(string filePath, string sessionDirectory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a path meets platform-specific requirements.
    /// Checks path length, reserved names, and character validity.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>True if the path is valid for the current platform; false otherwise.</returns>
    bool IsValidPath(string path);

    /// <summary>
    /// Gets the maximum path length supported by the current platform.
    /// </summary>
    /// <returns>The maximum path length in characters.</returns>
    int GetMaxPathLength();

    /// <summary>
    /// Preserves the virtual directory structure from blob names.
    /// Converts blob names with "/" separators into proper local directory paths.
    /// </summary>
    /// <param name="blobName">The blob name potentially containing "/" separators.</param>
    /// <returns>A local path preserving the virtual directory structure.</returns>
    string PreserveVirtualDirectoryStructure(string blobName);
}