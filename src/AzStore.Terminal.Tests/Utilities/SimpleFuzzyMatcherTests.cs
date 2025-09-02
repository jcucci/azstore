using AzStore.Terminal.Utilities;
using Xunit;

namespace AzStore.Terminal.Tests;

public class SimpleFuzzyMatcherTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void MatchAndRank_EmptyQuery_ReturnsAllWithZeroScore()
    {
        var matcher = new SimpleFuzzyMatcher();
        var items = new[] { "alpha", "beta" };

        var results = matcher.MatchAndRank(items, s => new[] { s }, "").ToList();

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(0, r.Score));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void MatchAndRank_SubstringVsSubsequence_PrioritizesSubstring()
    {
        var matcher = new SimpleFuzzyMatcher();
        var items = new[] { "strgacct", "mystorage", "s-t-r-g-a-c-c-t" };

        var results = matcher.MatchAndRank(items, s => new[] { s }, "stor").OrderByDescending(r => r.Score).ToList();

        Assert.Equal("mystorage", results[0].Item);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void MatchAndRank_CaseInsensitive_Works()
    {
        var matcher = new SimpleFuzzyMatcher();
        var items = new[] { "MyStorage", "other" };

        var results = matcher.MatchAndRank(items, s => new[] { s }, "storage").ToList();

        Assert.Single(results);
        Assert.Equal("MyStorage", results[0].Item);
    }
}

