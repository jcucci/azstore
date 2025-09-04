using System.ComponentModel.DataAnnotations;

namespace AzStore.Configuration;

public class ThemeSettings
{
    private static readonly string[] ValidColors = 
    {
        "Black", "DarkBlue", "DarkGreen", "DarkCyan", "DarkRed", "DarkMagenta", 
        "DarkYellow", "Gray", "DarkGray", "Blue", "Green", "Cyan", "Red", 
        "Magenta", "Yellow", "White"
    };

    [Required]
    [ValidConsoleColor]
    public string PromptColor { get; set; } = nameof(ConsoleColor.Green);

    [Required]
    [ValidConsoleColor] 
    public string SelectedItemColor { get; set; } = nameof(ConsoleColor.Yellow);

    [Required]
    [ValidConsoleColor]
    public string StatusMessageColor { get; set; } = nameof(ConsoleColor.Cyan);

    [Required]
    [ValidConsoleColor]
    public string ErrorColor { get; set; } = nameof(ConsoleColor.Red);

    // Extended palette for richer theming (Phase 5)
    [ValidConsoleColor]
    public string BreadcrumbColor { get; set; } = nameof(ConsoleColor.Gray);

    [ValidConsoleColor]
    public string ContainerColor { get; set; } = nameof(ConsoleColor.Blue);

    [ValidConsoleColor]
    public string BlobColor { get; set; } = nameof(ConsoleColor.White);

    [ValidConsoleColor]
    public string TitleColor { get; set; } = nameof(ConsoleColor.White);

    [ValidConsoleColor]
    public string PagerInfoColor { get; set; } = nameof(ConsoleColor.DarkGray);

    [ValidConsoleColor]
    public string InputColor { get; set; } = nameof(ConsoleColor.White);

    public static bool IsValidColor(string colorName)
    {
        return ValidColors.Contains(colorName, StringComparer.OrdinalIgnoreCase) ||
               Enum.TryParse<ConsoleColor>(colorName, true, out _);
    }
}

 
