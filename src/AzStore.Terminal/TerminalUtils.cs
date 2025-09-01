namespace AzStore.Terminal;

/// <summary>
/// Utility functions for terminal display operations.
/// </summary>
public static class TerminalUtils
{
    /// <summary>
    /// Formats bytes into human-readable format with fixed decimal places.
    /// </summary>
    /// <param name="bytes">Number of bytes to format.</param>
    /// <returns>Formatted byte string (e.g., "1.5 MB").</returns>
    public static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        
        return $"{len:F1} {sizes[order]}";
    }

    /// <summary>
    /// Formats bytes into human-readable format with automatic decimal precision.
    /// </summary>
    /// <param name="bytes">Number of bytes to format.</param>
    /// <returns>Formatted byte string (e.g., "1.5 MB" or "1 MB").</returns>
    public static string FormatSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        
        return $"{len:0.##} {sizes[order]}";
    }
}