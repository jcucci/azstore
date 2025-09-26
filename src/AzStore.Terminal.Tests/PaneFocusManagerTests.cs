using AzStore.Terminal.UI.Focus;
using Terminal.Gui;
using Xunit;

namespace AzStore.Terminal.Tests;

[Trait("Category", "Unit")]
public class PaneFocusManagerTests
{
    [Fact]
    public void TryGetFirst_ReturnsFirstFocusableView()
    {
        var manager = new PaneFocusManager();
        var first = new View { CanFocus = true };
        var second = new View { CanFocus = true };

        manager.Register(first);
        manager.Register(second);

        var result = manager.TryGetFirst(out var view);

        Assert.True(result);
        Assert.Same(first, view);
    }

    [Fact]
    public void TryGetNext_CyclesThroughViews()
    {
        var manager = new PaneFocusManager();
        var first = new View { CanFocus = true };
        var second = new View { CanFocus = true };

        manager.Register(first);
        manager.Register(second);

        manager.TryGetFirst(out _);

        var moved = manager.TryGetNext(out var viewAfterFirst);
        var wrapped = manager.TryGetNext(out var viewAfterSecond);

        Assert.True(moved);
        Assert.Same(second, viewAfterFirst);
        Assert.True(wrapped);
        Assert.Same(first, viewAfterSecond);
    }

    [Fact]
    public void TryGetPrevious_MovesBackwardAndWraps()
    {
        var manager = new PaneFocusManager();
        var first = new View { CanFocus = true };
        var second = new View { CanFocus = true };
        var third = new View { CanFocus = true };

        manager.Register(first);
        manager.Register(second);
        manager.Register(third);

        manager.TryGetFirst(out _);
        manager.TryGetNext(out _); // now on second

        var movedBack = manager.TryGetPrevious(out var viewAfterBack);
        var wrapped = manager.TryGetPrevious(out var viewAfterWrap);

        Assert.True(movedBack);
        Assert.Same(first, viewAfterBack);
        Assert.True(wrapped);
        Assert.Same(third, viewAfterWrap);
    }

    [Fact]
    public void FocusSkipsViewsThatCannotReceiveFocus()
    {
        var manager = new PaneFocusManager();
        var first = new View { CanFocus = true };
        var blocked = new View { CanFocus = false };
        var hidden = new View { CanFocus = true, Visible = false };
        var last = new View { CanFocus = true };

        manager.Register(first);
        manager.Register(blocked);
        manager.Register(hidden);
        manager.Register(last);

        manager.TryGetFirst(out _);
        var moved = manager.TryGetNext(out var next);

        Assert.True(moved);
        Assert.Same(last, next);
    }

    [Fact]
    public void SetCurrent_UpdatesIndex()
    {
        var manager = new PaneFocusManager();
        var first = new View { CanFocus = true };
        var second = new View { CanFocus = true };

        manager.Register(first);
        manager.Register(second);

        manager.SetCurrent(second);

        var moved = manager.TryGetNext(out var next);

        Assert.True(moved);
        Assert.Same(first, next);
    }
}
