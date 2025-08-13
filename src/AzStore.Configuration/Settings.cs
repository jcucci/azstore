using System.ComponentModel.DataAnnotations;

namespace AzStore.Configuration;

public class AzStoreSettings
{
    public const string SectionName = "AzStore";
    private const string DefaultDirectoryName = "azstore";

    [Required]
    public string SessionsDirectory { get; set; } = GetDefaultSessionsDirectory();

    public string? DefaultStorageAccount { get; set; }

    public FileConflictBehavior OnFileConflict { get; set; } = FileConflictBehavior.Overwrite;

    public KeyBindings KeyBindings { get; set; } = new();

    public ThemeSettings Theme { get; set; } = new();

    public Dictionary<string, string> Aliases { get; set; } = new();

    public LoggingSettings Logging { get; set; } = new();

    private static string GetDefaultSessionsDirectory()
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(documentsPath, DefaultDirectoryName);
    }
}