using AzStore.Configuration;
using Microsoft.Extensions.Options;
using Tui = Terminal.Gui;

namespace AzStore.Terminal.Theming;

public class ThemeService : IThemeService
{
    private readonly IOptionsMonitor<AzStoreSettings> _settings;

    public ThemeService(IOptionsMonitor<AzStoreSettings> settings)
    {
        _settings = settings;
    }

    public ConsoleColor ResolveForeground(ThemeToken token)
    {
        var theme = _settings.CurrentValue.Theme;
        var colorName = token switch
        {
            ThemeToken.Background => theme.BackgroundColor,
            ThemeToken.Prompt => theme.PromptColor,
            ThemeToken.Status => theme.StatusMessageColor,
            ThemeToken.Error => theme.ErrorColor,
            ThemeToken.Selection => theme.SelectedItemColor,
            ThemeToken.Title => theme.TitleColor,
            ThemeToken.Breadcrumb => theme.BreadcrumbColor,
            ThemeToken.ItemContainer => theme.ContainerColor,
            ThemeToken.ItemBlob => theme.BlobColor,
            ThemeToken.PagerInfo => theme.PagerInfoColor,
            ThemeToken.Input => theme.InputColor,
            _ => nameof(ConsoleColor.White)
        };

        return Enum.TryParse<ConsoleColor>(colorName, true, out var color)
            ? color
            : ConsoleColor.White;
    }

    private Tui.Color ParseColor(string colorString, bool isBackground = false)
    {
        var theme = _settings.CurrentValue.Theme;
        var alpha = isBackground ? theme.Alpha : 255;

        // Check if it's a hex color
        if (ThemeSettings.IsHexColor(colorString))
        {
            return ParseHexColor(colorString, alpha);
        }

        // Try to parse as ConsoleColor name
        if (Enum.TryParse<ConsoleColor>(colorString, true, out var consoleColor))
        {
            var tuiColor = MapConsoleToTui(consoleColor);
            // Apply alpha to the color by reconstructing it with the alpha value
            return ApplyAlpha(tuiColor, alpha);
        }

        // Default fallback
        return new Tui.Color(255, 255, 255, alpha);
    }

    private static Tui.Color ParseHexColor(string hex, int alpha)
    {
        hex = hex.TrimStart('#');

        if (hex.Length != 6)
        {
            return new Tui.Color(255, 255, 255, alpha);
        }

        try
        {
            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);

            return new Tui.Color(r, g, b, alpha);
        }
        catch
        {
            return new Tui.Color(255, 255, 255, alpha);
        }
    }

    private static Tui.Color ApplyAlpha(Tui.Color color, int alpha)
    {
        return new Tui.Color(color.R, color.G, color.B, alpha);
    }

    public void Write(string text, ThemeToken token)
    {
        var original = Console.ForegroundColor;
        Console.ForegroundColor = ResolveForeground(token);
        Console.Write(text);
        Console.ForegroundColor = original;
    }

    public void WriteLine(string text, ThemeToken token)
    {
        var original = Console.ForegroundColor;
        Console.ForegroundColor = ResolveForeground(token);
        Console.WriteLine(text);
        Console.ForegroundColor = original;
    }

    public Tui.Attribute ResolveTui(ThemeToken token)
    {
        var theme = _settings.CurrentValue.Theme;
        var colorString = token switch
        {
            ThemeToken.Background => theme.BackgroundColor,
            ThemeToken.Prompt => theme.PromptColor,
            ThemeToken.Status => theme.StatusMessageColor,
            ThemeToken.Error => theme.ErrorColor,
            ThemeToken.Selection => theme.SelectedItemColor,
            ThemeToken.Title => theme.TitleColor,
            ThemeToken.Breadcrumb => theme.BreadcrumbColor,
            ThemeToken.ItemContainer => theme.ContainerColor,
            ThemeToken.ItemBlob => theme.BlobColor,
            ThemeToken.PagerInfo => theme.PagerInfoColor,
            ThemeToken.Input => theme.InputColor,
            _ => nameof(ConsoleColor.White)
        };

        var fg = ParseColor(colorString, isBackground: false);
        var bg = ParseColor(theme.BackgroundColor, isBackground: true);
        return new Tui.Attribute(fg, bg);
    }

    public Tui.ColorScheme GetListColorScheme()
    {
        // Selection as background highlight with contrasting foreground
        var theme = _settings.CurrentValue.Theme;
        var selBg = ParseColor(theme.SelectedItemColor, isBackground: false);
        var selFg = GetContrastingForeground(selBg);

        return new Tui.ColorScheme
        {
            Normal = ResolveTui(ThemeToken.ItemBlob),
            Focus = new Tui.Attribute(selFg, selBg),
            HotNormal = ResolveTui(ThemeToken.ItemContainer),
            HotFocus = new Tui.Attribute(selFg, selBg),
            Disabled = new Tui.Attribute(new Tui.Color(169, 169, 169, 255), ParseColor(theme.BackgroundColor, isBackground: true))
        };
    }

    public Tui.ColorScheme GetLabelColorScheme(ThemeToken token)
    {
        return new Tui.ColorScheme
        {
            Normal = ResolveTui(token),
            Focus = ResolveTui(token),
            HotNormal = ResolveTui(token),
            HotFocus = ResolveTui(token),
            Disabled = new Tui.Attribute(Tui.Color.DarkGray, ResolveBackground())
        };
    }

    public Tui.Color ResolveBackground()
    {
        var theme = _settings.CurrentValue.Theme;
        var name = string.IsNullOrWhiteSpace(theme.BackgroundColor)
            ? nameof(ConsoleColor.Black)
            : theme.BackgroundColor;

        return ParseColor(name, isBackground: true);
    }

    private static Tui.Color MapConsoleToTui(ConsoleColor color) => color switch
    {
        ConsoleColor.Black => Tui.Color.Black,
        ConsoleColor.DarkBlue => Tui.Color.Blue,
        ConsoleColor.DarkGreen => Tui.Color.Green,
        ConsoleColor.DarkCyan => Tui.Color.Cyan,
        ConsoleColor.DarkRed => Tui.Color.Red,
        ConsoleColor.DarkMagenta => Tui.Color.Magenta,
        ConsoleColor.DarkYellow => Tui.Color.Yellow,
        ConsoleColor.Gray => Tui.Color.Gray,
        ConsoleColor.DarkGray => Tui.Color.DarkGray,
        ConsoleColor.Blue => Tui.Color.Blue,
        ConsoleColor.Green => Tui.Color.Green,
        ConsoleColor.Cyan => Tui.Color.Cyan,
        ConsoleColor.Red => Tui.Color.Red,
        ConsoleColor.Magenta => Tui.Color.Magenta,
        ConsoleColor.Yellow => Tui.Color.Yellow,
        ConsoleColor.White => Tui.Color.White,
        _ => Tui.Color.White
    };

    private static Tui.Color GetContrastingForeground(Tui.Color background)
    {
        if (background == Tui.Color.White) return Tui.Color.Black;
        if (background == Tui.Color.Yellow) return Tui.Color.Black;
        if (background == Tui.Color.Gray) return Tui.Color.Black;
        if (background == Tui.Color.Cyan) return Tui.Color.Black;
        return Tui.Color.White;
    }
}
