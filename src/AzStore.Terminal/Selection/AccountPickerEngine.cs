using AzStore.Configuration;
using AzStore.Core.Models.Authentication;
using AzStore.Terminal.Utilities;

namespace AzStore.Terminal.Selection;

public class AccountPickerEngine
{
    private readonly IReadOnlyList<StorageAccountInfo> _all;
    private readonly IFuzzyMatcher _matcher;
    private readonly TerminalSelectionOptions _options;

    public string Query { get; private set; } = string.Empty;
    public int Index { get; private set; }
    public int WindowStart { get; private set; }
    public int MaxVisible { get; }
    private List<FuzzyMatchResult<StorageAccountInfo>> _filtered = [];
    public IReadOnlyList<FuzzyMatchResult<StorageAccountInfo>> Filtered => _filtered;

    public AccountPickerEngine(IReadOnlyList<StorageAccountInfo> accounts, IFuzzyMatcher matcher, TerminalSelectionOptions options)
    {
        _all = accounts;
        _matcher = matcher;
        _options = options;
        MaxVisible = Math.Max(5, options.MaxVisibleItems);
        _filtered = accounts.Select(a => new FuzzyMatchResult<StorageAccountInfo>(a, 0))
            .OrderBy(a => a.Item.AccountName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        Index = _filtered.Count > 0 ? 0 : -1;
        WindowStart = 0;
    }

    public void TypeChar(char c)
    {
        if (!char.IsControl(c))
        {
            Query += c;
            ApplyFilter();
        }
    }

    public void Backspace()
    {
        if (Query.Length > 0)
        {
            Query = Query[..^1];
            ApplyFilter();
        }
    }

    public void MoveDown()
    {
        if (_filtered.Count == 0) return;
        Index = Math.Min(Index + 1, _filtered.Count - 1);
        EnsureWindow();
    }

    public void MoveUp()
    {
        if (_filtered.Count == 0) return;
        Index = Math.Max(Index - 1, 0);
        EnsureWindow();
    }

    public void PageDown()
    {
        if (_filtered.Count == 0) return;
        Index = Math.Min(Index + MaxVisible, _filtered.Count - 1);
        EnsureWindow();
    }

    public void PageUp()
    {
        if (_filtered.Count == 0) return;
        Index = Math.Max(Index - MaxVisible, 0);
        EnsureWindow();
    }

    public void Top()
    {
        if (_filtered.Count == 0) return;
        Index = 0;
        EnsureWindow();
    }

    public void Bottom()
    {
        if (_filtered.Count == 0) return;
        Index = _filtered.Count - 1;
        EnsureWindow();
    }

    public StorageAccountInfo? Current()
    {
        if (Index < 0 || Index >= _filtered.Count) return null;
        return _filtered[Index].Item;
    }

    public (int start, int end) VisibleWindow()
    {
        var end = Math.Min(_filtered.Count, WindowStart + MaxVisible);
        return (WindowStart, end);
    }

    private void ApplyFilter()
    {
        if (_options.EnableFuzzySearch && Query.Length > 0)
        {
            _filtered = _matcher
                .MatchAndRank(_all, a => [a.AccountName, a.ResourceGroupName ?? string.Empty, a.SubscriptionId?.ToString() ?? string.Empty], Query)
                .OrderByDescending(r => r.Score)
                .ThenBy(r => r.Item.AccountName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        else
        {
            _filtered = _all.Select(a => new FuzzyMatchResult<StorageAccountInfo>(a, 0))
                .OrderBy(a => a.Item.AccountName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (_filtered.Count == 0)
        {
            Index = -1;
            WindowStart = 0;
            return;
        }

        if (Index < 0) Index = 0;
        if (Index >= _filtered.Count) Index = _filtered.Count - 1;
        EnsureWindow();
    }

    private void EnsureWindow()
    {
        if (Index < 0) { WindowStart = 0; return; }
        if (Index < WindowStart) WindowStart = Index;
        if (Index >= WindowStart + MaxVisible) WindowStart = Math.Max(0, Index - MaxVisible + 1);
    }
}
