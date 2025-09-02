using System.Text.RegularExpressions;

namespace AzStore.Terminal.UI;

/// <summary>
/// Simple console-based pager for long text with VIM-like navigation and search.
/// </summary>
public static class TerminalPager
{
    private const string StatusHint = "j/k or ↑/↓ scroll • PgUp/PgDn • gg/G • /search • n next • q/Esc quit";

    /// <summary>
    /// Shows the text in a scrollable console pager with basic search.
    /// </summary>
    /// <param name="title">Optional title displayed at the top.</param>
    /// <param name="content">The content to display.</param>
    public static void Show(string? title, string content)
    {
        var lines = SplitLines(content);
        var top = 0;
        var (width, height) = GetConsoleSize();
        var bodyHeight = Math.Max(1, height - 2); // one line for title, one for status

        int? lastMatchLine = null;
        string? lastSearch = null;

        Console.TreatControlCAsInput = true;

        while (true)
        {
            (width, height) = GetConsoleSize();
            bodyHeight = Math.Max(1, height - 2);
            top = Math.Clamp(top, 0, Math.Max(0, lines.Length - bodyHeight));

            Console.Clear();

            // Title
            if (!string.IsNullOrWhiteSpace(title))
            {
                Console.WriteLine(Truncate(title, width));
            }
            else
            {
                Console.WriteLine(new string('=', Math.Min(width, 40)));
            }

            // Body
            for (var i = 0; i < bodyHeight; i++)
            {
                var idx = top + i;
                if (idx >= 0 && idx < lines.Length)
                {
                    Console.WriteLine(Truncate(lines[idx], width));
                }
                else
                {
                    Console.WriteLine();
                }
            }

            // Status
            var rangeInfo = lines.Length == 0
                ? "0/0"
                : $"{Math.Min(lines.Length, top + bodyHeight)}/{lines.Length}";
            var statusLeft = StatusHint;
            var statusRight = rangeInfo;
            var pad = Math.Max(0, width - statusLeft.Length - statusRight.Length - 1);
            Console.WriteLine(Truncate(statusLeft + new string(' ', pad) + statusRight, width));

            var key = Console.ReadKey(true);

            // Quit
            if (key.Key == ConsoleKey.Q || key.Key == ConsoleKey.Escape)
                break;

            switch (key.Key)
            {
                case ConsoleKey.DownArrow:
                case ConsoleKey.J:
                    top = Math.Min(top + 1, Math.Max(0, lines.Length - bodyHeight));
                    break;
                case ConsoleKey.UpArrow:
                case ConsoleKey.K:
                    top = Math.Max(top - 1, 0);
                    break;
                case ConsoleKey.PageDown:
                    top = Math.Min(top + bodyHeight, Math.Max(0, lines.Length - bodyHeight));
                    break;
                case ConsoleKey.PageUp:
                    top = Math.Max(top - bodyHeight, 0);
                    break;
                default:
                    // Handle multi-key sequences: gg (top), G (bottom), /search, n (next)
                    if (key.KeyChar == 'g')
                    {
                        // Wait briefly to see if next char is 'g'
                        if (TryReadCharWithin(250, out var next) && next == 'g')
                            top = 0;
                        break;
                    }
                    if (key.KeyChar == 'G')
                    {
                        top = Math.Max(0, lines.Length - bodyHeight);
                        break;
                    }
                    if (key.KeyChar == '/')
                    {
                        lastSearch = ReadLineWithPrompt("/", width - 1);
                        if (!string.IsNullOrEmpty(lastSearch))
                        {
                            lastMatchLine = FindMatch(lines, lastSearch, startFrom: top + 1);
                            if (lastMatchLine.HasValue)
                            {
                                // Show match near top
                                top = Math.Clamp(lastMatchLine.Value, 0, Math.Max(0, lines.Length - bodyHeight));
                            }
                        }
                        break;
                    }
                    if (key.KeyChar == 'n' && !string.IsNullOrEmpty(lastSearch))
                    {
                        var next = FindMatch(lines, lastSearch, startFrom: (lastMatchLine ?? top) + 1);
                        if (next.HasValue)
                        {
                            lastMatchLine = next;
                            top = Math.Clamp(next.Value, 0, Math.Max(0, lines.Length - bodyHeight));
                        }
                        break;
                    }
                    break;
            }
        }
    }

    private static int? FindMatch(string[] lines, string pattern, int startFrom)
    {
        Regex? regex = null;
        try
        {
            regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
        catch
        {
            // Treat as literal if invalid regex
            regex = new Regex(Regex.Escape(pattern), RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        for (var i = Math.Max(0, startFrom); i < lines.Length; i++)
        {
            if (regex.IsMatch(lines[i]))
                return i;
        }
        // wrap-around search
        for (var i = 0; i < Math.Min(startFrom, lines.Length); i++)
        {
            if (regex.IsMatch(lines[i]))
                return i;
        }
        return null;
    }

    private static string ReadLineWithPrompt(string prompt, int maxWidth)
    {
        Console.Write(prompt);
        var buffer = new List<char>();
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return new string(buffer.ToArray());
            }
            if (key.Key == ConsoleKey.Escape)
            {
                Console.WriteLine();
                return string.Empty;
            }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Count > 0)
                {
                    buffer.RemoveAt(buffer.Count - 1);
                    Console.Write("\b \b");
                }
                continue;
            }
            if (!char.IsControl(key.KeyChar))
            {
                buffer.Add(key.KeyChar);
                // avoid wrapping; truncate echo if needed
                var toEcho = buffer.Count <= maxWidth ? key.KeyChar.ToString() : string.Empty;
                Console.Write(toEcho);
            }
        }
    }

    private static bool TryReadCharWithin(int milliseconds, out char ch)
    {
        var start = DateTime.UtcNow;
        var end = start.AddMilliseconds(milliseconds);
        while (DateTime.UtcNow < end)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                ch = key.KeyChar;
                return true;
            }
            Thread.Sleep(1);
        }
        ch = '\0';
        return false;
    }

    private static (int width, int height) GetConsoleSize()
    {
        try
        {
            return (Console.WindowWidth, Console.WindowHeight);
        }
        catch
        {
            return (120, 40);
        }
    }

    private static string[] SplitLines(string content)
    {
        return content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
    }

    private static string Truncate(string value, int width)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (width <= 0) return string.Empty;
        return value.Length <= width ? value : value[..Math.Max(0, width - 1)];
    }
}
