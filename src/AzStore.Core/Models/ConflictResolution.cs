namespace AzStore.Core.Models;

/// <summary>
/// Specifies how to handle file conflicts during download operations.
/// </summary>
public enum ConflictResolution
{
    /// <summary>
    /// Overwrite existing files without prompting.
    /// </summary>
    Overwrite,
    
    /// <summary>
    /// Skip files that already exist.
    /// </summary>
    Skip,
    
    /// <summary>
    /// Rename the downloaded file to avoid conflicts (e.g., file.txt becomes file(1).txt).
    /// </summary>
    Rename,
    
    /// <summary>
    /// Ask the user interactively how to handle each conflict.
    /// </summary>
    Ask
}