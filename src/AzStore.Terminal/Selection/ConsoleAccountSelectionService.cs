using AzStore.Configuration;
using AzStore.Core.Models.Authentication;
using AzStore.Terminal.Input;
using AzStore.Terminal.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AzStore.Terminal.Theming;
using System.Linq;

namespace AzStore.Terminal.Selection;

public class ConsoleAccountSelectionService : IAccountSelectionService
{
    private readonly ILogger<ConsoleAccountSelectionService> _logger;
    private readonly KeyBindings _keyBindings;
    private readonly TerminalSelectionOptions _options;
    private readonly IFuzzyMatcher _matcher;
    private readonly IThemeService _theme;
    private readonly IConsoleLogScope _consoleLogScope;

    public ConsoleAccountSelectionService(
        ILogger<ConsoleAccountSelectionService> logger,
        IOptions<AzStoreSettings> settings,
        IFuzzyMatcher matcher,
        IThemeService theme,
        IConsoleLogScope consoleLogScope)
    {
        _logger = logger;
        _keyBindings = settings.Value.KeyBindings;
        _options = settings.Value.Selection;
        _matcher = matcher;
        _theme = theme;
        _consoleLogScope = consoleLogScope;
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

        // Track whether we've drawn the overlay to avoid duplicating output
        var overlayDrawn = false;
        var overlayLines = 0;

        bool? prevCursorVisible = null;
        // Suppress console logging while the interactive picker is active to avoid visual interference
        using var __suppressLogs = _consoleLogScope.Suppress();
        try
        {
            #pragma warning disable CA1416
            try { prevCursorVisible = Console.CursorVisible; } catch { /* ignore on unsupported platforms */ }
            try { Console.CursorVisible = false; } catch { /* ignore */ }
            #pragma warning restore CA1416

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

            if (_options.PickerTimeoutMs.HasValue && lastInputTime.HasValue && (DateTime.UtcNow - lastInputTime.Value).TotalMilliseconds > _options.PickerTimeoutMs.Value)
            {
                _logger.LogInformation("Account picker timed out after {Ms} ms", _options.PickerTimeoutMs);
                if (overlayDrawn)
                {
                    ClearOverlay(maxVisible + 5);
                }
                Console.WriteLine("Selection cancelled (timeout)");
                return null;
            }

            // Only draw when needed: initially, and after handling a keypress
            if (!Console.KeyAvailable)
            {
                if (!overlayDrawn)
                {
                    overlayLines = DrawOverlay(engine.Filtered, engine.Query, engine.Index, engine.WindowStart, maxVisible);
                    overlayDrawn = true;
                }
                await Task.Delay(10, cancellationToken);
                continue;
            }

            var key = Console.ReadKey(intercept: true);
            lastInputTime = _options.PickerTimeoutMs.HasValue ? DateTime.UtcNow : (DateTime?)null;

            if (key.Key == ConsoleKey.Escape)
            {
                _logger.LogInformation("User cancelled account selection");
                if (overlayDrawn)
                {
                    ClearOverlay(overlayLines);
                }
                Console.WriteLine("Selection cancelled");
                return null;
            }

            if (key.Key == ConsoleKey.Enter)
            {
                if (engine.Filtered.Count == 0) continue;
                var chosen = engine.Current();
                if (chosen == null) continue;
                _logger.LogInformation("User selected storage account: {Account} ({SubscriptionId})", chosen.AccountName, chosen.SubscriptionId);
                if (overlayDrawn)
                {
                    ClearOverlay(overlayLines);
                }
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

                // Redraw overlay to reflect changes, clearing previous overlay first
                if (overlayDrawn)
                {
                    ClearOverlay(overlayLines);
                }
                overlayLines = DrawOverlay(engine.Filtered, engine.Query, engine.Index, engine.WindowStart, maxVisible);
                overlayDrawn = true;
            }
        }
        finally
        {
            if (prevCursorVisible.HasValue)
            {
                #pragma warning disable CA1416
                try { Console.CursorVisible = prevCursorVisible.Value; } catch { /* ignore */ }
                #pragma warning restore CA1416
            }
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

    private int DrawOverlay(IReadOnlyList<FuzzyMatchResult<StorageAccountInfo>> rows, string query, int index, int windowStart, int maxVisible)
    {
        var header = "Select storage account (type to filter, Enter=select, Esc=cancel)";
        int lines = 0;
        // Header
        _theme.WriteLine(header, ThemeToken.Title); lines++;
        _theme.WriteLine($"Filter: {query}", ThemeToken.Status); lines++;

        var end = Math.Min(rows.Count, windowStart + maxVisible);

        // Compute column widths for visible window to align vertically
        var visible = new List<StorageAccountInfo>(Math.Max(0, end - windowStart));
        for (int i = windowStart; i < end; i++) visible.Add(rows[i].Item);

        var consoleWidth = 0;
        try { consoleWidth = Console.WindowWidth; } catch { consoleWidth = 0; }
        if (consoleWidth <= 0) consoleWidth = 100;

        var maxName = visible.Count > 0 ? visible.Max(v => (v.AccountName ?? string.Empty).Length) : 0;
        var maxRg = visible.Count > 0 ? visible.Max(v => (v.ResourceGroupName ?? string.Empty).Length) : 0;
        var subWidth = 10; // e.g., "[1234abcd]" => fixed column

        // Budget columns to avoid wrapping in narrow terminals
        var padding = 2; // spaces between columns
        var prefixWidth = 2; // "> " or two spaces
        // Keep total printed length strictly less than the window width to avoid soft-wrapping
        var budget = Math.Max(10, consoleWidth - prefixWidth - padding * 2 - subWidth - 1);
        var nameWidth = Math.Min(maxName, Math.Max(8, budget / 2));
        var rgWidth = Math.Min(maxRg, Math.Max(8, budget - nameWidth));
        if (nameWidth + rgWidth > budget)
        {
            rgWidth = Math.Max(0, budget - nameWidth);
        }

        for (int i = windowStart; i < end; i++)
        {
            var item = rows[i].Item;
            var prefix = (i == index) ? "> " : "  ";
            var subText = item.SubscriptionId?.ToString() ?? string.Empty;
            var subShort = subText.Length >= 8 ? subText[..8] : subText;
            var rg = item.ResourceGroupName ?? string.Empty;

            // Prepare padded columns (truncate if needed)
            var nameCol = Truncate(item.AccountName, nameWidth).PadRight(nameWidth);
            var subCol = ($"[{subShort}]").PadRight(subWidth);
            var rgCol = Truncate(rg, rgWidth).PadRight(rgWidth);

            Console.Write(prefix);
            if (i == index)
            {
                _theme.Write(nameCol, ThemeToken.Selection);
            }
            else
            {
                _theme.Write(nameCol, ThemeToken.Status);
            }
            Console.Write(new string(' ', padding));
            _theme.Write(subCol, ThemeToken.Prompt);
            Console.Write(new string(' ', padding));
            _theme.Write(rgCol, ThemeToken.Status);
            Console.WriteLine();
            lines++;
        }

        // Footer spacer
        // Do not append multiple blank lines to avoid visual gaps
        // Console.WriteLine();
        // lines++;
        return lines;
    }

    private void ClearOverlay(int lines)
    {
        // Move cursor up and clear lines to simulate non-destructive overlay
        for (int i = 0; i < lines; i++)
        {
            Console.SetCursorPosition(0, Math.Max(Console.CursorTop - 1, 0));
            // Avoid writing exactly WindowWidth spaces to prevent implicit line-wrapping in some terminals
            var width = 0;
            try { width = Console.WindowWidth; } catch { width = 0; }
            if (width <= 1) width = 80;
            Console.Write(new string(' ', width - 1));
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

    
}
