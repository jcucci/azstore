using System.ComponentModel.DataAnnotations;

namespace AzStore.Configuration;

public class AzStoreSettings
{
    public const string SectionName = "AzStore";
    private const string DefaultDirectoryName = "azstore";

    [Required]
    [MinLength(1, ErrorMessage = "Sessions directory path cannot be empty")]
    public string SessionsDirectory { get; set; } = GetDefaultSessionsDirectory();

    [StringLength(100, ErrorMessage = "Storage account name cannot exceed 100 characters")]
    public string? DefaultStorageAccount { get; set; }

    [Required]
    public FileConflictBehavior OnFileConflict { get; set; } = FileConflictBehavior.Overwrite;

    [Required]
    public KeyBindings KeyBindings { get; set; } = new();

    [Required]
    public ThemeSettings Theme { get; set; } = new();

    public Dictionary<string, string> Aliases { get; set; } = new();

    [Required]
    public LoggingSettings Logging { get; set; } = new();

    private static string GetDefaultSessionsDirectory()
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(documentsPath, DefaultDirectoryName);
    }

    public bool IsValid()
    {
        try
        {
            Path.GetFullPath(SessionsDirectory);
            return true;
        }
        catch
        {
            return false;
        }
    }
}