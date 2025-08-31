using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AzStore.Terminal.Tests;

[Trait("Category", "Unit")]
public class VimNavigatorTests
{
    private readonly ILogger<VimNavigator> _logger;
    private readonly VimNavigator _navigator;

    public VimNavigatorTests()
    {
        _logger = Substitute.For<ILogger<VimNavigator>>();
        _navigator = new VimNavigator(_logger);
    }

    [Fact]
    public void Constructor_SetsInitialMode()
    {
        Assert.Equal(NavigationMode.Normal, _navigator.CurrentMode);
        Assert.False(_navigator.HasPendingCommand);
        Assert.Null(_navigator.PendingCommand);
    }

    [Fact]
    public void SwitchMode_ChangesMode()
    {
        var result = _navigator.SwitchMode(NavigationMode.Command);

        Assert.True(result);
        Assert.Equal(NavigationMode.Command, _navigator.CurrentMode);
    }

    [Fact]
    public void SwitchMode_SameMode_ReturnsFalse()
    {
        var result = _navigator.SwitchMode(NavigationMode.Normal);

        Assert.False(result);
        Assert.Equal(NavigationMode.Normal, _navigator.CurrentMode);
    }

    [Fact]
    public void SwitchMode_RaisesModeChangedEvent()
    {
        NavigationModeChangedEventArgs? capturedArgs = null;
        _navigator.ModeChanged += (_, args) => capturedArgs = args;

        _navigator.SwitchMode(NavigationMode.Command);

        Assert.NotNull(capturedArgs);
        Assert.Equal(NavigationMode.Normal, capturedArgs.PreviousMode);
        Assert.Equal(NavigationMode.Command, capturedArgs.CurrentMode);
    }

    [Fact]
    public void ExitMode_ReturnsToInitialMode()
    {
        _navigator.SwitchMode(NavigationMode.Command);
        
        var result = _navigator.ExitMode();

        Assert.True(result);
        Assert.Equal(NavigationMode.Normal, _navigator.CurrentMode);
    }

    [Fact]
    public void ExitMode_NoModeHistory_ReturnsFalse()
    {
        var result = _navigator.ExitMode();

        Assert.False(result);
        Assert.Equal(NavigationMode.Normal, _navigator.CurrentMode);
    }

    [Fact]
    public void ExitMode_ClearsPendingCommand()
    {
        _navigator.EnterCommandMode(":");
        _navigator.AppendToCommand('l');
        _navigator.AppendToCommand('s');

        _navigator.ExitMode();

        Assert.False(_navigator.HasPendingCommand);
        Assert.Null(_navigator.PendingCommand);
    }

    [Fact]
    public void EnterCommandMode_SwitchesToCommandMode()
    {
        _navigator.EnterCommandMode();

        Assert.Equal(NavigationMode.Command, _navigator.CurrentMode);
        Assert.True(_navigator.HasPendingCommand);
        Assert.Equal(":", _navigator.PendingCommand);
    }

    [Fact]
    public void EnterCommandMode_CustomPrefix()
    {
        _navigator.EnterCommandMode("/");

        Assert.Equal(NavigationMode.Command, _navigator.CurrentMode);
        Assert.Equal("/", _navigator.PendingCommand);
    }

    [Fact]
    public void AppendToCommand_InCommandMode()
    {
        _navigator.EnterCommandMode();

        var result1 = _navigator.AppendToCommand('h');
        var result2 = _navigator.AppendToCommand('e');
        var result3 = _navigator.AppendToCommand('l');
        var result4 = _navigator.AppendToCommand('p');

        Assert.True(result1);
        Assert.True(result2);
        Assert.True(result3);
        Assert.True(result4);
        Assert.Equal(":help", _navigator.PendingCommand);
    }

    [Fact]
    public void AppendToCommand_NotInCommandMode_ReturnsFalse()
    {
        var result = _navigator.AppendToCommand('x');

        Assert.False(result);
        Assert.Null(_navigator.PendingCommand);
    }

    [Fact]
    public void BackspaceCommand_RemovesLastCharacter()
    {
        _navigator.EnterCommandMode();
        _navigator.AppendToCommand('l');
        _navigator.AppendToCommand('s');

        var result = _navigator.BackspaceCommand();

        Assert.True(result);
        Assert.Equal(":l", _navigator.PendingCommand);
    }

    [Fact]
    public void BackspaceCommand_EmptyCommand_ReturnsFalse()
    {
        _navigator.EnterCommandMode();

        var result = _navigator.BackspaceCommand();

        Assert.False(result);
        Assert.Equal(":", _navigator.PendingCommand);
    }

    [Fact]
    public void BackspaceCommand_NotInCommandMode_ReturnsFalse()
    {
        var result = _navigator.BackspaceCommand();

        Assert.False(result);
    }

    [Fact]
    public void CompleteCommand_ReturnsAndClearsCommand()
    {
        _navigator.EnterCommandMode();
        _navigator.AppendToCommand('e');
        _navigator.AppendToCommand('x');
        _navigator.AppendToCommand('i');
        _navigator.AppendToCommand('t');

        var command = _navigator.CompleteCommand();

        Assert.Equal(":exit", command);
        Assert.False(_navigator.HasPendingCommand);
        Assert.Null(_navigator.PendingCommand);
    }

    [Fact]
    public void CompleteCommand_NotInCommandMode_ReturnsNull()
    {
        var command = _navigator.CompleteCommand();

        Assert.Null(command);
    }

    [Theory]
    [InlineData(NavigationMode.Normal, KeyBindingAction.MoveDown, true)]
    [InlineData(NavigationMode.Normal, KeyBindingAction.MoveUp, true)]
    [InlineData(NavigationMode.Normal, KeyBindingAction.Enter, true)]
    [InlineData(NavigationMode.Normal, KeyBindingAction.Back, true)]
    [InlineData(NavigationMode.Normal, KeyBindingAction.Top, true)]
    [InlineData(NavigationMode.Normal, KeyBindingAction.Bottom, true)]
    [InlineData(NavigationMode.Normal, KeyBindingAction.Download, true)]
    [InlineData(NavigationMode.Normal, KeyBindingAction.Search, true)]
    [InlineData(NavigationMode.Normal, KeyBindingAction.Command, true)]
    [InlineData(NavigationMode.Command, KeyBindingAction.Command, true)]
    [InlineData(NavigationMode.Command, KeyBindingAction.MoveDown, false)]
    [InlineData(NavigationMode.Command, KeyBindingAction.Enter, false)]
    [InlineData(NavigationMode.Visual, KeyBindingAction.MoveDown, false)]
    [InlineData(NavigationMode.Visual, KeyBindingAction.Command, false)]
    public void IsActionValidForCurrentMode_ValidatesCorrectly(NavigationMode mode, KeyBindingAction action, bool expected)
    {
        _navigator.SwitchMode(mode);

        var result = _navigator.IsActionValidForCurrentMode(action);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Reset_ReturnsToNormalMode()
    {
        _navigator.SwitchMode(NavigationMode.Command);
        _navigator.SwitchMode(NavigationMode.Visual);
        _navigator.AppendToCommand('t');
        _navigator.AppendToCommand('e');
        _navigator.AppendToCommand('s');
        _navigator.AppendToCommand('t');

        _navigator.Reset();

        Assert.Equal(NavigationMode.Normal, _navigator.CurrentMode);
        Assert.False(_navigator.HasPendingCommand);
        Assert.Null(_navigator.PendingCommand);
    }

    [Fact]
    public void Reset_RaisesModeChangedEvent()
    {
        _navigator.SwitchMode(NavigationMode.Command);
        
        NavigationModeChangedEventArgs? capturedArgs = null;
        _navigator.ModeChanged += (_, args) => capturedArgs = args;

        _navigator.Reset();

        Assert.NotNull(capturedArgs);
        Assert.Equal(NavigationMode.Command, capturedArgs.PreviousMode);
        Assert.Equal(NavigationMode.Normal, capturedArgs.CurrentMode);
    }

    [Fact]
    public void GetModeDescription_ReturnsCorrectDescriptions()
    {
        Assert.Equal("NORMAL", _navigator.GetModeDescription());

        _navigator.EnterCommandMode();
        Assert.Equal("COMMAND :", _navigator.GetModeDescription());

        _navigator.AppendToCommand('h');
        _navigator.AppendToCommand('e');
        _navigator.AppendToCommand('l');
        _navigator.AppendToCommand('p');
        Assert.Equal("COMMAND :help", _navigator.GetModeDescription());

        _navigator.SwitchMode(NavigationMode.Visual);
        Assert.Equal("VISUAL", _navigator.GetModeDescription());
    }

    [Fact]
    public void ModeHistory_MaintainsStack()
    {
        _navigator.SwitchMode(NavigationMode.Command);
        _navigator.SwitchMode(NavigationMode.Visual);

        _navigator.ExitMode(); // Should return to Command
        Assert.Equal(NavigationMode.Command, _navigator.CurrentMode);

        _navigator.ExitMode(); // Should return to Normal
        Assert.Equal(NavigationMode.Normal, _navigator.CurrentMode);

        var result = _navigator.ExitMode(); // No more history
        Assert.False(result);
        Assert.Equal(NavigationMode.Normal, _navigator.CurrentMode);
    }
}