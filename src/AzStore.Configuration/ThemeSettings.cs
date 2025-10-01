using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace AzStore.Configuration;

public class ThemeSettings
{
    private static readonly string[] ValidColors =
    {
        "Black", "DarkBlue", "DarkGreen", "DarkCyan", "DarkRed", "DarkMagenta",
        "DarkYellow", "Gray", "DarkGray", "Blue", "Green", "Cyan", "Red",
        "Magenta", "Yellow", "White"
    };

    private static readonly Regex HexColorRegex = new(@"^#?([0-9A-Fa-f]{6})$", RegexOptions.Compiled);

    [Required]
    [ValidConsoleColor]
    public string PromptColor { get; set; } = "#89dceb";  // Catppuccin Mocha Sky

    [Required]
    [ValidConsoleColor]
    public string SelectedItemColor { get; set; } = "#cba6f7";  // Catppuccin Mocha Mauve

    [Required]
    [ValidConsoleColor]
    public string StatusMessageColor { get; set; } = "#94e2d5";  // Catppuccin Mocha Teal

    [Required]
    [ValidConsoleColor]
    public string ErrorColor { get; set; } = "#f38ba8";  // Catppuccin Mocha Red

    // Extended palette for richer theming (Phase 5)
    [ValidConsoleColor]
    public string BreadcrumbColor { get; set; } = "#6c7086";  // Catppuccin Mocha Overlay0

    [ValidConsoleColor]
    public string ContainerColor { get; set; } = "#89b4fa";  // Catppuccin Mocha Blue

    [ValidConsoleColor]
    public string BlobColor { get; set; } = "#cdd6f4";  // Catppuccin Mocha Text

    [ValidConsoleColor]
    public string TitleColor { get; set; } = "#b4befe";  // Catppuccin Mocha Lavender

    [ValidConsoleColor]
    public string PagerInfoColor { get; set; } = "#585b70";  // Catppuccin Mocha Surface2

    [ValidConsoleColor]
    public string InputColor { get; set; } = "#cdd6f4";  // Catppuccin Mocha Text

    [ValidConsoleColor]
    public string BackgroundColor { get; set; } = "#1e1e2e";  // Catppuccin Mocha Base

    /// <summary>
    /// Global alpha/transparency value applied to all colors (0-255).
    /// 0 = fully transparent, 255 = fully opaque (default).
    /// </summary>
    [Range(0, 255)]
    public int Alpha { get; set; } = 255;

    public static bool IsValidColor(string colorName)
    {
        return ValidColors.Contains(colorName, StringComparer.OrdinalIgnoreCase) ||
               Enum.TryParse<ConsoleColor>(colorName, true, out _) ||
               IsHexColor(colorName);
    }

    public static bool IsHexColor(string colorName)
    {
        return !string.IsNullOrWhiteSpace(colorName) && HexColorRegex.IsMatch(colorName);
    }
}

 
