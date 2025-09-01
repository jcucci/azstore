using AzStore.Terminal.Commands;
using Xunit;

namespace AzStore.Terminal.Tests.Commands;

public class ListCommandOptionsTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void FromArgs_NoArgs_Defaults()
    {
        var opts = ListCommandOptions.FromArgs([]);
        Assert.Null(opts.Container);
        Assert.Null(opts.Pattern);
        Assert.Equal("name", opts.SortKey);
        Assert.False(opts.Descending);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void FromArgs_SortVariants_Parsed()
    {
        var a = ListCommandOptions.FromArgs(["--sort", "size"]);
        Assert.Equal("size", a.SortKey);

        var b = ListCommandOptions.FromArgs(["--sort=date"]);
        Assert.Equal("date", b.SortKey);

        var c = ListCommandOptions.FromArgs(["--SORT", "time"]);
        Assert.Equal("time", c.SortKey);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void FromArgs_DescendingFlags_Parsed()
    {
        var a = ListCommandOptions.FromArgs(["--desc"]);
        Assert.True(a.Descending);

        var b = ListCommandOptions.FromArgs(["--reverse"]);
        Assert.True(b.Descending);

        var c = ListCommandOptions.FromArgs(["-r"]);
        Assert.True(c.Descending);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void FromArgs_ContainerAndPattern_OrderMatters()
    {
        var opts = ListCommandOptions.FromArgs(["docs", "*.txt"]);
        Assert.Equal("docs", opts.Container);
        Assert.Equal("*.txt", opts.Pattern);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void FromArgs_PatternOnly_GlobDetected()
    {
        var a = ListCommandOptions.FromArgs(["*.log"]);
        Assert.Null(a.Container);
        Assert.Equal("*.log", a.Pattern);

        var b = ListCommandOptions.FromArgs(["????.txt"]);
        Assert.Null(b.Container);
        Assert.Equal("????.txt", b.Pattern);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void FromArgs_Mixed_WithSortAndDesc()
    {
        var opts = ListCommandOptions.FromArgs(["docs", "*.txt", "--sort", "date", "--desc"]);
        Assert.Equal("docs", opts.Container);
        Assert.Equal("*.txt", opts.Pattern);
        Assert.Equal("date", opts.SortKey);
        Assert.True(opts.Descending);
    }
}

