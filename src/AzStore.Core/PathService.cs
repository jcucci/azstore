using AzStore.Core.Exceptions;
using AzStore.Core.IO;
using AzStore.Core.Models;
using Microsoft.Extensions.Logging;

namespace AzStore.Core;

/// <summary>
/// Provides cross-platform path calculation and directory management for Azure blob downloads.
/// </summary>
public class PathService : IPathService
{
    private readonly ILogger<PathService> _logger;

    /// <summary>
    /// Initializes a new instance of the PathService class.
    /// </summary>
    /// <param name="logger">Logger instance for this service.</param>
    public PathService(ILogger<PathService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public string CalculateBlobDownloadPath(Session session, string containerName, string blobName)
    {
        _logger.LogDebug("Calculating blob download path for session: {SessionName}, container: {ContainerName}, blob: {BlobName}", session.Name, containerName, blobName);

        try
        {
            var sessionDirectory = PathHelper.CreateSafeDirectoryName(session.Name);
            var containerDirectory = PathHelper.CreateSafeDirectoryName(containerName);
            var blobLocalPath = PathHelper.ConvertBlobPathToLocalPath(blobName);

            var fullPath = PathHelper.SafeCombine(session.Directory, sessionDirectory, containerDirectory, blobLocalPath);

            if (PathHelper.IsPathTooLong(fullPath))
            {
                _logger.LogWarning("Generated path is too long: {PathLength} characters. Path: {Path}", fullPath.Length, fullPath);
                throw new PathTooLongException($"The generated path is too long ({fullPath.Length} characters): {fullPath}");
            }

            if (!IsValidPath(fullPath))
            {
                _logger.LogWarning("Generated path is invalid: {Path}", fullPath);
                throw new ArgumentException($"The generated path is invalid: {fullPath}");
            }

            _logger.LogDebug("Generated blob download path: {Path}", fullPath);
            return fullPath;
        }
        catch (Exception ex) when (!(ex is ArgumentException || ex is ArgumentNullException || ex is PathTooLongException))
        {
            _logger.LogError(ex, "Failed to calculate blob download path for session: {SessionName}, container: {ContainerName}, blob: {BlobName}", session.Name, containerName, blobName);
            throw new DirectoryServiceException($"Failed to calculate blob download path: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public string CalculateContainerDirectoryPath(Session session, string containerName)
    {
        _logger.LogDebug("Calculating container directory path for session: {SessionName}, container: {ContainerName}", session.Name, containerName);

        try
        {
            var sessionDirectory = PathHelper.CreateSafeDirectoryName(session.Name);
            var containerDirectory = PathHelper.CreateSafeDirectoryName(containerName);
            var fullPath = PathHelper.SafeCombine(session.Directory, sessionDirectory, containerDirectory);

            if (PathHelper.IsPathTooLong(fullPath))
            {
                _logger.LogWarning("Generated container path is too long: {PathLength} characters. Path: {Path}", fullPath.Length, fullPath);
                throw new PathTooLongException($"The generated container path is too long ({fullPath.Length} characters): {fullPath}");
            }

            if (!IsValidPath(fullPath))
            {
                _logger.LogWarning("Generated container path is invalid: {Path}", fullPath);
                throw new ArgumentException($"The generated container path is invalid: {fullPath}");
            }

            _logger.LogDebug("Generated container directory path: {Path}", fullPath);
            return fullPath;
        }
        catch (Exception ex) when (!(ex is ArgumentException || ex is ArgumentNullException || ex is PathTooLongException))
        {
            _logger.LogError(ex, "Failed to calculate container directory path for session: {SessionName}, container: {ContainerName}", session.Name, containerName);
            throw new DirectoryServiceException($"Failed to calculate container directory path: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public string SanitizePathComponent(string pathComponent)
    {
        try
        {
            return PathHelper.SanitizePathComponent(pathComponent);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sanitize path component: {PathComponent}", pathComponent);
            throw new DirectoryServiceException($"Failed to sanitize path component: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> EnsureDirectoryExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var directoryPath = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directoryPath))
        {
            _logger.LogDebug("No directory to create for file path: {FilePath}", filePath);
            return true;
        }

        _logger.LogDebug("Ensuring directory exists: {DirectoryPath}", directoryPath);

        try
        {
            if (Directory.Exists(directoryPath))
            {
                _logger.LogDebug("Directory already exists: {DirectoryPath}", directoryPath);
                return true;
            }

            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                Directory.CreateDirectory(directoryPath);
            }, cancellationToken);

            _logger.LogDebug("Successfully created directory: {DirectoryPath}", directoryPath);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Directory creation cancelled for: {DirectoryPath}", directoryPath);
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied while creating directory: {DirectoryPath}", directoryPath);
            throw new UnauthorizedAccessException($"Access denied while creating directory '{directoryPath}': {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create directory: {DirectoryPath}", directoryPath);
            throw new DirectoryServiceException($"Failed to create directory '{directoryPath}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CleanupEmptyDirectoriesAsync(string filePath, string sessionDirectory, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Cleaning up empty directories from: {FilePath}, stopping at: {SessionDirectory}", filePath, sessionDirectory);

        bool IsDirectoryEmpty(string path)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        void DeleteDirectory(string path)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.Delete(path);
        }

        try
        {
            var currentDirectory = Path.GetDirectoryName(filePath);
            var stopDirectory = Path.GetFullPath(sessionDirectory);

            while (!string.IsNullOrEmpty(currentDirectory))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentFullPath = Path.GetFullPath(currentDirectory);

                if (string.Equals(currentFullPath, stopDirectory, StringComparison.OrdinalIgnoreCase) || !currentFullPath.StartsWith(stopDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Reached stop directory or went beyond: {CurrentDirectory}", currentFullPath);
                    break;
                }

                if (Directory.Exists(currentFullPath))
                {
                    var isEmpty = await Task.Run(() => IsDirectoryEmpty(currentFullPath), cancellationToken);
                    if (isEmpty)
                    {
                        await Task.Run(() => DeleteDirectory(currentFullPath), cancellationToken);
                        _logger.LogDebug("Deleted empty directory: {Directory}", currentFullPath);
                    }
                    else
                    {
                        _logger.LogDebug("Directory is not empty, stopping cleanup: {Directory}", currentFullPath);
                        break;
                    }
                }

                currentDirectory = Path.GetDirectoryName(currentDirectory);
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Directory cleanup cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup empty directories from: {FilePath}", filePath);
            return false;
        }
    }

    /// <inheritdoc/>
    public bool IsValidPath(string path) => !string.IsNullOrWhiteSpace(path) && PathHelper.IsValidPath(path);

    /// <inheritdoc/>
    public int GetMaxPathLength() => PathHelper.GetMaxPathLength();

    /// <inheritdoc/>
    public string PreserveVirtualDirectoryStructure(string blobName) =>
        blobName != null
            ? PathHelper.ConvertBlobPathToLocalPath(blobName)
            : throw new ArgumentNullException(nameof(blobName));
}