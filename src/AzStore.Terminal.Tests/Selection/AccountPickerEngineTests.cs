using AzStore.Configuration;
using AzStore.Core.Models.Authentication;
using AzStore.Terminal.Selection;
using AzStore.Terminal.Utilities;
using Xunit;

namespace AzStore.Terminal.Tests;

public class AccountPickerEngineTests
{
    private static (AccountPickerEngine Engine, List<StorageAccountInfo> Accounts) CreateEngine(int count = 30)
    {
        var accounts = Enumerable.Range(1, count)
            .Select(i => new StorageAccountInfo($"acct{i:000}", null, Guid.NewGuid(), $"rg{i}", new Uri($"https://acct{i}.blob.core.windows.net/")))
            .ToList();
        var matcher = new SimpleFuzzyMatcher();
        var options = new TerminalSelectionOptions { MaxVisibleItems = 5, EnableFuzzySearch = true };
        var engine = new AccountPickerEngine(accounts, matcher, options);
        return (engine, accounts);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void MoveDown_And_Window_Adjusts()
    {
        var (engine, _) = CreateEngine();

        Assert.Equal(0, engine.Index);
        Assert.Equal((0, 5), engine.VisibleWindow());

        for (int i = 0; i < 6; i++) engine.MoveDown();
        Assert.Equal(6, engine.Index);
        var (start, end) = engine.VisibleWindow();
        Assert.True(engine.Index >= start && engine.Index < end);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Top_And_Bottom_Work()
    {
        var (engine, accounts) = CreateEngine();
        engine.Bottom();
        Assert.Equal(accounts.Count - 1, engine.Index);
        engine.Top();
        Assert.Equal(0, engine.Index);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Fuzzy_Substring_Ranks_Highest()
    {
        var accounts = new List<StorageAccountInfo>
        {
            new("strgacct", null, Guid.NewGuid(), "rg1", new Uri("https://a/")),
            new("mystorage", null, Guid.NewGuid(), "rg2", new Uri("https://b/")),
            new("s-t-r-g-a-c-c-t", null, Guid.NewGuid(), "rg3", new Uri("https://c/")),
        };
        var engine = new AccountPickerEngine(accounts, new SimpleFuzzyMatcher(), new TerminalSelectionOptions { EnableFuzzySearch = true });

        engine.TypeChar('s'); engine.TypeChar('t'); engine.TypeChar('o'); engine.TypeChar('r');

        Assert.True(engine.Filtered.Count > 0);
        Assert.Equal("mystorage", engine.Filtered[0].Item.AccountName);
    }
}

