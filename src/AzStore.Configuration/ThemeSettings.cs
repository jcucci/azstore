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

    public static bool IsValidColor(string colorName)
    {
        return ValidColors.Contains(colorName, StringComparer.OrdinalIgnoreCase) ||
               Enum.TryParse<ConsoleColor>(colorName, true, out _);
    }
}

public class ValidConsoleColorAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is string colorName)
        {
            return ThemeSettings.IsValidColor(colorName);
        }
        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field must be a valid console color name.";
    }
}