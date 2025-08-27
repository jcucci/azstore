using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace AzStore.Core.IO;

/// <summary>
/// Provides cross-platform path utilities for sanitizing and validating file paths.
/// </summary>
public static class PathHelper
{
    /// <summary>
    /// Windows reserved file names that cannot be used as file or directory names.
    /// </summary>
    private static readonly HashSet<string> WindowsReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    /// <summary>
    /// Default replacement character for invalid characters.
    /// </summary>
    private const char DefaultReplacementChar = '_';

    /// <summary>
    /// Maximum path length on Windows (before long path support).
    /// </summary>
    private const int WindowsMaxPath = 260;

    /// <summary>
    /// Maximum path length on most Unix-like systems.
    /// </summary>
    private const int UnixMaxPath = 4096;

    /// <summary>
    /// Maximum filename length on most filesystems.
    /// </summary>
    private const int MaxFilenameLength = 255;

    /// <summary>
    /// Maximum path length for Windows long path support.
    /// </summary>
    private const int WindowsLongPathSupport = 32767;

    /// <summary>
    /// Regex pattern for valid Azure blob names (used for validation).
    /// </summary>
    private static readonly Regex ValidBlobNamePattern = new(@"^[a-zA-Z0-9\.\-_/]+$", RegexOptions.Compiled);

    /// <summary>
    /// Sanitizes a path component for cross-platform compatibility.
    /// Removes or replaces invalid characters and handles reserved names.
    /// </summary>
    /// <param name="pathComponent">The path component to sanitize.</param>
    /// <param name="replacementChar">Character to use for replacing invalid characters.</param>
    /// <returns>The sanitized path component.</returns>
    /// <exception cref="ArgumentException">Thrown when the result would be empty after sanitization.</exception>
    public static string SanitizePathComponent(string pathComponent, char replacementChar = DefaultReplacementChar)
    {
        if (string.IsNullOrWhiteSpace(pathComponent))
            throw new ArgumentException("Path component cannot be empty or whitespace", nameof(pathComponent));

        var sanitized = pathComponent.Trim();

        sanitized = ReplaceInvalidCharacters(sanitized, replacementChar);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            sanitized = HandleWindowsReservedNames(sanitized, replacementChar);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            sanitized = sanitized.TrimEnd('.', ' ');

        if (string.IsNullOrEmpty(sanitized))
            sanitized = "default";

        if (sanitized.Length > MaxFilenameLength)
            sanitized = TruncateWithExtension(sanitized, MaxFilenameLength);

        return sanitized;
    }

    /// <summary>
    /// Converts a blob name with forward slashes to a local path with proper directory separators.
    /// </summary>
    /// <param name="blobName">The blob name potentially containing forward slashes.</param>
    /// <returns>A local path with proper directory separators.</returns>
    public static string ConvertBlobPathToLocalPath(string blobName)
    {
        var pathComponents = blobName.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var sanitizedComponents = pathComponents.Select(component => SanitizePathComponent(component)).ToArray();

        return Path.Combine(sanitizedComponents);
    }

    /// <summary>
    /// Validates that a path meets platform-specific requirements.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>True if the path is valid; false otherwise.</returns>
    public static bool IsValidPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            if (path.Length > GetMaxPathLength())
                return false;

            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                return false;

            Path.GetFullPath(path);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the maximum path length supported by the current platform.
    /// </summary>
    /// <returns>The maximum path length in characters.</returns>
    public static int GetMaxPathLength() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? IsLongPathEnabled() ? WindowsLongPathSupport : WindowsMaxPath
            : UnixMaxPath;

    /// <summary>
    /// Determines if a path would be too long on the current platform.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path is too long; false otherwise.</returns>
    public static bool IsPathTooLong(string path) => !string.IsNullOrEmpty(path) && path.Length > GetMaxPathLength();

    /// <summary>
    /// Creates a safe directory name from a container name.
    /// </summary>
    /// <param name="containerName">The Azure container name.</param>
    /// <returns>A sanitized directory name.</returns>
    public static string CreateSafeDirectoryName(string containerName) => SanitizePathComponent(containerName);

    /// <summary>
    /// Extracts the filename from a blob name, handling virtual directory structures.
    /// </summary>
    /// <param name="blobName">The blob name.</param>
    /// <returns>Just the filename portion.</returns>
    public static string ExtractFilename(string blobName)
    {
        var lastSlash = blobName.LastIndexOf('/');
        var filename = lastSlash >= 0 ? blobName[(lastSlash + 1)..] : blobName;

        return SanitizePathComponent(filename);
    }

    /// <summary>
    /// Extracts the virtual directory path from a blob name.
    /// </summary>
    /// <param name="blobName">The blob name.</param>
    /// <returns>The virtual directory path, or null if none.</returns>
    public static string? ExtractVirtualDirectory(string blobName)
    {
        var lastSlash = blobName.LastIndexOf('/');
        if (lastSlash <= 0)
            return null;

        var virtualDir = blobName[..lastSlash];
        return ConvertBlobPathToLocalPath(virtualDir);
    }

    /// <summary>
    /// Combines path components safely, handling null or empty values.
    /// </summary>
    /// <param name="paths">Path components to combine.</param>
    /// <returns>The combined path.</returns>
    public static string SafeCombine(params string?[] paths)
    {
        var validPaths = paths.Where(p => !string.IsNullOrWhiteSpace(p)).Cast<string>().ToArray();
        return validPaths.Length == 0 ? string.Empty : Path.Combine(validPaths);
    }

    /// <summary>
    /// Replaces invalid characters in a path component.
    /// </summary>
    private static string ReplaceInvalidCharacters(string input, char replacementChar)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var result = new StringBuilder(input);

        foreach (var invalidChar in invalidChars)
        {
            result.Replace(invalidChar, replacementChar);
        }

        result.Replace(':', replacementChar);
        result.Replace('*', replacementChar);
        result.Replace('?', replacementChar);
        result.Replace('"', replacementChar);
        result.Replace('<', replacementChar);
        result.Replace('>', replacementChar);
        result.Replace('|', replacementChar);

        return result.ToString();
    }

    /// <summary>
    /// Handles Windows reserved names by appending a suffix if needed.
    /// </summary>
    private static string HandleWindowsReservedNames(string input, char replacementChar)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(input);
        if (WindowsReservedNames.Contains(nameWithoutExtension))
        {
            var extension = Path.GetExtension(input);
            return $"{nameWithoutExtension}{replacementChar}file{extension}";
        }

        return input;
    }

    /// <summary>
    /// Truncates a filename while preserving the extension.
    /// </summary>
    private static string TruncateWithExtension(string filename, int maxLength)
    {
        if (filename.Length <= maxLength)
            return filename;

        var extension = Path.GetExtension(filename);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(filename);

        var maxNameLength = maxLength - extension.Length;
        if (maxNameLength <= 0)
        {
            // Extension is too long, just truncate everything
            return filename[..maxLength];
        }

        var truncatedName = nameWithoutExtension[..maxNameLength];
        return $"{truncatedName}{extension}";
    }

    /// <summary>
    /// Checks if long path support is enabled on Windows.
    /// </summary>
    private static bool IsLongPathEnabled()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        // For simplicity, assume long paths are supported on Windows 10+
        var version = Environment.OSVersion.Version;
        return version.Major >= 10;
    }
}