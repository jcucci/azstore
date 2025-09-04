using AzStore.Configuration;
using AzStore.Core.Models.Authentication;
using AzStore.Terminal.Input;
using AzStore.Terminal.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AzStore.Terminal.Theming;

namespace AzStore.Terminal.Selection;

public class ConsoleAccountSelectionService : IAccountSelectionService
{
    private readonly ILogger<ConsoleAccountSelectionService> _logger;
    private readonly KeyBindings _keyBindings;
    private readonly TerminalSelectionOptions _options;
    private readonly IFuzzyMatcher _matcher;
    private readonly IThemeService _theme;

    public ConsoleAccountSelectionService(
        ILogger<ConsoleAccountSelectionService> logger,
        IOptions<AzStoreSettings> settings,
        IFuzzyMatcher matcher,
        IThemeService theme)
    {
        _logger = logger;
        _keyBindings = settings.Value.KeyBindings;
        _options = settings.Value.Selection;
        _matcher = matcher;
        _theme = theme;
    }

    public async Task<StorageAccountInfo?> PickAsync(IReadOnlyList<StorageAccountInfo> accounts, CancellationToken cancellationToken = default)
    {
        if (accounts.Count == 0)
        {
            _theme.WriteLine("No storage accounts found.", ThemeToken.Error);
            return null;
        }

        if (accounts.Count == 1)
        {
            _logger.LogInformation("Single storage account available: {Account}", accounts[0].AccountName);
            return accounts[0];
        }

        _logger.LogWarning("Multiple storage accounts discovered: {Count}", accounts.Count);

        var engine = new AccountPickerEngine(accounts, _matcher, _options);
        var maxVisible = engine.MaxVisible;

        // Treat picker timeout as an inactivity timeout; reset on keypress
        var lastInputTime = _options.PickerTimeoutMs.HasValue ? DateTime.UtcNow : (DateTime?)null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_options.PickerTimeoutMs.HasValue && lastInputTime.HasValue && (DateTime.UtcNow - lastInputTime.Value).TotalMilliseconds > _options.PickerTimeoutMs.Value)
            {
                _logger.LogInformation("Account picker timed out after {Ms} ms", _options.PickerTimeoutMs);
                Console.WriteLine("Selection cancelled (timeout)");
                return null;
            }

            DrawOverlay(engine.Filtered, engine.Query, engine.Index, engine.WindowStart, maxVisible);

            if (!Console.KeyAvailable)
            {
                await Task.Delay(10, cancellationToken);
                continue;
            }

            var key = Console.ReadKey(intercept: true);
            lastInputTime = _options.PickerTimeoutMs.HasValue ? DateTime.UtcNow : (DateTime?)null;

            if (key.Key == ConsoleKey.Escape)
            {
                _logger.LogInformation("User cancelled account selection");
                ClearOverlay(maxVisible + 5);
                Console.WriteLine("Selection cancelled");
                return null;
            }

            if (key.Key == ConsoleKey.Enter)
            {
                if (engine.Filtered.Count == 0) continue;
                var chosen = engine.Current();
                if (chosen == null) continue;
                _logger.LogInformation("User selected storage account: {Account} ({SubscriptionId})", chosen.AccountName, chosen.SubscriptionId);
                ClearOverlay(maxVisible + 5);
                return chosen;
            }

            // Navigation keys: arrows, j/k, gg/G
            if (key.Key == ConsoleKey.DownArrow || key.KeyChar == SafeChar(_keyBindings.MoveDown))
            {
                engine.MoveDown();
            }
            else if (key.Key == ConsoleKey.UpArrow || key.KeyChar == SafeChar(_keyBindings.MoveUp))
            {
                engine.MoveUp();
            }
            else if (key.Key == ConsoleKey.PageDown)
            {
                engine.PageDown();
            }
            else if (key.Key == ConsoleKey.PageUp)
            {
                engine.PageUp();
            }
            else if (key.KeyChar == SafeChar(_keyBindings.Bottom))
            {
                engine.Bottom();
            }
            else if (MatchesTopSequence(key))
            {
                engine.Top();
            }
            else if (key.Key == ConsoleKey.Backspace)
            {
                engine.Backspace();
            }
            else if (!char.IsControl(key.KeyChar))
            {
                engine.TypeChar(key.KeyChar);
            }

            // clamp and adjust window will occur in next loop before draw
        }
    }

    private static char SafeChar(string s) => string.IsNullOrEmpty(s) ? '\0' : s[^1];

    private bool MatchesTopSequence(ConsoleKeyInfo key)
    {
        // crude handling for 'gg' top navigation: if user presses 'g' followed by 'g' quickly
        if (string.Equals(_keyBindings.Top, "gg", StringComparison.Ordinal))
        {
            if (key.KeyChar == 'g')
            {
                if (Console.KeyAvailable)
                {
                    var next = Console.ReadKey(intercept: true);
                    if (next.KeyChar == 'g') return true;
                }
            }
        }
        return false;
    }

    private void DrawOverlay(IReadOnlyList<FuzzyMatchResult<StorageAccountInfo>> rows, string query, int index, int windowStart, int maxVisible)
    {
        var header = $"Select storage account (type to filter, Enter=select, Esc=cancel)";
        Console.WriteLine();
        Console.WriteLine(header);
        Console.WriteLine($"Filter: {query}");

        var end = Math.Min(rows.Count, windowStart + maxVisible);
        for (int i = windowStart; i < end; i++)
        {
            var item = rows[i].Item;
            var prefix = (i == index) ? "> " : "  ";
            var line = $"{item.AccountName}  [{item.SubscriptionId}]  {item.ResourceGroupName ?? ""}";
            line = Truncate(line, Console.WindowWidth - 4);

            // write with optional highlighting of first substring occurrence
            if (_options.HighlightMatches && !string.IsNullOrEmpty(query))
            {
                var idx = line.IndexOf(query, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var before = line[..idx];
                    var matchLength = Math.Min(query.Length, Math.Max(0, line.Length - idx));
                    var match = line.Substring(idx, matchLength);
                    var after = line[(idx + match.Length)..];

                    Console.Write(prefix);
                    Console.Write(before);
                    var c = Console.ForegroundColor;
                    Console.ForegroundColor = _theme.ResolveForeground(ThemeToken.Status);
                    Console.Write(match);
                    Console.ForegroundColor = c;
                    Console.WriteLine(after);
                    continue;
                }
            }

            Console.WriteLine(prefix + line);
        }

        // clear remaining area if fewer than maxVisible
        for (int i = end; i < windowStart + maxVisible; i++)
        {
            Console.WriteLine("~");
        }

        Console.WriteLine();
    }

    private void ClearOverlay(int lines)
    {
        // Move cursor up and clear lines to simulate non-destructive overlay
        for (int i = 0; i < lines; i++)
        {
            Console.SetCursorPosition(0, Math.Max(Console.CursorTop - 1, 0));
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Math.Max(Console.CursorTop - 1, 0));
        }
    }

    private static string Truncate(string s, int width)
    {
        if (width <= 0) return string.Empty;
        if (string.IsNullOrEmpty(s)) return s;
        if (s.Length <= width) return s;
        return width <= 1 ? s[..width] : s[..(width - 1)] + "\u2026"; // ellipsis
    }

    private static void WriteInfo(string message) => Console.WriteLine(message);
}
