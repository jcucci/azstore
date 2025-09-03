using System.ComponentModel.DataAnnotations;

namespace AzStore.Configuration;

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

