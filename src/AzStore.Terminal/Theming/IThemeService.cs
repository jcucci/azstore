using System;

namespace AzStore.Terminal.Theming;

public interface IThemeService
{
    ConsoleColor ResolveForeground(ThemeToken token);
    void Write(string text, ThemeToken token);
    void WriteLine(string text, ThemeToken token);
    global::Terminal.Gui.Attribute ResolveTui(ThemeToken token);
    global::Terminal.Gui.ColorScheme GetListColorScheme();
    global::Terminal.Gui.ColorScheme GetLabelColorScheme(ThemeToken token);
    global::Terminal.Gui.Color ResolveBackground();
}
