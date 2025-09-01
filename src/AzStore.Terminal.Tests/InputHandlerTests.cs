using AzStore.Terminal.Input;
using AzStore.Terminal.Navigation;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Terminal.Gui;
using Xunit;
using KeyBindingsConfig = AzStore.Configuration.KeyBindings;

namespace AzStore.Terminal.Tests;

[Trait("Category", "Unit")]
public class InputHandlerTests : IDisposable
{
    private readonly ILogger<InputHandler> _mockLogger;
    private readonly KeyBindingsConfig _keyBindings;
    private readonly InputHandler _inputHandler;
    private readonly List<NavigationResult> _capturedResults = [];

    public InputHandlerTests()
    {
        _mockLogger = Substitute.For<ILogger<InputHandler>>();
        _keyBindings = new KeyBindingsConfig
        {
            KeySequenceTimeout = 1000,
            KeyRepeatDelay = 100, // Shorter for testing
            KeyRepeatInterval = 50  // Shorter for testing
        };
        _inputHandler = new InputHandler(_mockLogger, _keyBindings);
        _inputHandler.NavigationRequested += OnNavigationRequested;
    }

    private void OnNavigationRequested(object? sender, NavigationResult result)
    {
        _capturedResults.Add(result);
    }

    [Fact]
    public void ProcessKeyEvent_ArrowKeyDown_RaisesNavigationWithMoveDown()
    {
        var result = _inputHandler.ProcessKeyEvent(Key.CursorDown);

        Assert.True(result);
        Assert.Single(_capturedResults);
        Assert.Equal(NavigationAction.Enter, _capturedResults[0].Action);
        Assert.Equal(KeyBindingAction.MoveDown, _capturedResults[0].KeyBindingAction);
    }

    [Fact]
    public void ProcessKeyEvent_ArrowKeyUp_RaisesNavigationWithMoveUp()
    {
        var result = _inputHandler.ProcessKeyEvent(Key.CursorUp);

        Assert.True(result);
        Assert.Single(_capturedResults);
        Assert.Equal(NavigationAction.Enter, _capturedResults[0].Action);
        Assert.Equal(KeyBindingAction.MoveUp, _capturedResults[0].KeyBindingAction);
    }

    [Fact]
    public void ProcessKeyEvent_ArrowKeyLeft_RaisesBackNavigation()
    {
        var result = _inputHandler.ProcessKeyEvent(Key.CursorLeft);

        Assert.True(result);
        Assert.Single(_capturedResults);
        Assert.Equal(NavigationAction.Back, _capturedResults[0].Action);
    }

    [Fact]
    public void ProcessKeyEvent_ArrowKeyRight_RaisesEnterNavigation()
    {
        var result = _inputHandler.ProcessKeyEvent(Key.CursorRight);

        Assert.True(result);
        Assert.Single(_capturedResults);
        Assert.Equal(NavigationAction.Enter, _capturedResults[0].Action);
    }

    [Fact]
    public void ProcessKeyEvent_EscapeKey_RaisesCancelNavigation()
    {
        var result = _inputHandler.ProcessKeyEvent(Key.Esc);

        Assert.True(result);
        Assert.Single(_capturedResults);
        Assert.Equal(NavigationAction.Cancel, _capturedResults[0].Action);
    }

    [Fact]
    public void ProcessKeyEvent_EnterKey_RaisesEnterNavigation()
    {
        var result = _inputHandler.ProcessKeyEvent(Key.Enter);

        Assert.True(result);
        Assert.Single(_capturedResults);
        Assert.Equal(NavigationAction.Enter, _capturedResults[0].Action);
    }

    [Fact]
    public void ProcessKeyEvent_VimKeyJ_RaisesNavigationWithMoveDown()
    {
        var result = _inputHandler.ProcessKeyEvent((Key)'j');

        Assert.True(result);
        Assert.Single(_capturedResults);
        Assert.Equal(NavigationAction.Enter, _capturedResults[0].Action);
        Assert.Equal(KeyBindingAction.MoveDown, _capturedResults[0].KeyBindingAction);
    }

    [Fact]
    public void ProcessKeyEvent_VimKeyK_RaisesNavigationWithMoveUp()
    {
        var result = _inputHandler.ProcessKeyEvent((Key)'k');

        Assert.True(result);
        Assert.Single(_capturedResults);
        Assert.Equal(NavigationAction.Enter, _capturedResults[0].Action);
        Assert.Equal(KeyBindingAction.MoveUp, _capturedResults[0].KeyBindingAction);
    }

    [Fact]
    public void ProcessKeyEvent_MultiCharSequenceGG_RaisesJumpToTop()
    {
        // First 'g' should return partial match
        var result1 = _inputHandler.ProcessKeyEvent((Key)'g');
        Assert.True(result1);
        Assert.Empty(_capturedResults);

        // Second 'g' should complete sequence
        var result2 = _inputHandler.ProcessKeyEvent((Key)'g');
        Assert.True(result2);
        Assert.Single(_capturedResults);
        Assert.Equal(NavigationAction.JumpToTop, _capturedResults[0].Action);
        Assert.Equal(KeyBindingAction.Top, _capturedResults[0].KeyBindingAction);
    }

    [Fact]
    public void ProcessKeyEvent_UppercaseG_RaisesJumpToBottom()
    {
        // Test uppercase G directly - InputHandler should detect this as uppercase
        var result = _inputHandler.ProcessKeyEvent((Key)'G');

        Assert.True(result);
        Assert.Single(_capturedResults);
        Assert.Equal(NavigationAction.JumpToBottom, _capturedResults[0].Action);
        Assert.Equal(KeyBindingAction.Bottom, _capturedResults[0].KeyBindingAction);
    }

    [Fact]
    public void ProcessKeyEvent_JumpToTopEmptyList_HandlesGracefully()
    {
        // First 'g' should return partial match
        var result1 = _inputHandler.ProcessKeyEvent((Key)'g');
        Assert.True(result1);
        Assert.Empty(_capturedResults);

        // Second 'g' should complete sequence even with empty list
        var result2 = _inputHandler.ProcessKeyEvent((Key)'g');
        Assert.True(result2);
        Assert.Single(_capturedResults);
        Assert.Equal(NavigationAction.JumpToTop, _capturedResults[0].Action);
        Assert.Equal(KeyBindingAction.Top, _capturedResults[0].KeyBindingAction);
    }

    [Fact]
    public void ProcessKeyEvent_JumpToBottomEmptyList_HandlesGracefully()
    {
        var result = _inputHandler.ProcessKeyEvent((Key)'G');

        Assert.True(result);
        Assert.Single(_capturedResults);
        Assert.Equal(NavigationAction.JumpToBottom, _capturedResults[0].Action);
        Assert.Equal(KeyBindingAction.Bottom, _capturedResults[0].KeyBindingAction);
    }

    [Fact]
    public void ProcessKeyEvent_MultipleJumpCommands_AllProcessedCorrectly()
    {
        // Test gg (jump to top)
        _inputHandler.ProcessKeyEvent((Key)'g');
        _inputHandler.ProcessKeyEvent((Key)'g');

        // Test G (jump to bottom)  
        _inputHandler.ProcessKeyEvent((Key)'G');

        Assert.Equal(2, _capturedResults.Count);
        Assert.Equal(NavigationAction.JumpToTop, _capturedResults[0].Action);
        Assert.Equal(NavigationAction.JumpToBottom, _capturedResults[1].Action);
    }

    [Fact]
    public void ProcessKeyEvent_SearchCommand_RaisesCommandWithSlash()
    {
        var result = _inputHandler.ProcessKeyEvent((Key)'/');

        Assert.True(result);
        Assert.Single(_capturedResults);
        Assert.Equal(NavigationAction.Command, _capturedResults[0].Action);
        Assert.Equal("/", _capturedResults[0].Command);
        Assert.Equal(KeyBindingAction.Search, _capturedResults[0].KeyBindingAction);
    }

    [Fact]
    public void ProcessKeyEvent_CommandMode_RaisesCommandWithColon()
    {
        var result = _inputHandler.ProcessKeyEvent((Key)':');

        Assert.True(result);
        Assert.Single(_capturedResults);
        Assert.Equal(NavigationAction.Command, _capturedResults[0].Action);
        Assert.Equal(":", _capturedResults[0].Command);
        Assert.Equal(KeyBindingAction.Command, _capturedResults[0].KeyBindingAction);
    }

    [Fact]
    public void Clear_ClearsKeySequenceBuffer()
    {
        // Start a sequence
        _inputHandler.ProcessKeyEvent((Key)'g');
        Assert.Equal("g", _inputHandler.CurrentSequence);

        // Clear should reset sequence
        _inputHandler.Clear();
        Assert.Equal(string.Empty, _inputHandler.CurrentSequence);
        Assert.False(_inputHandler.HasPendingSequence);
    }

    [Fact]
    public void CurrentSequence_TracksPendingKeySequence()
    {
        Assert.Equal(string.Empty, _inputHandler.CurrentSequence);
        Assert.False(_inputHandler.HasPendingSequence);

        // Start sequence
        _inputHandler.ProcessKeyEvent((Key)'g');
        Assert.Equal("g", _inputHandler.CurrentSequence);
        Assert.True(_inputHandler.HasPendingSequence);

        // Complete sequence
        _inputHandler.ProcessKeyEvent((Key)'g');
        Assert.Equal(string.Empty, _inputHandler.CurrentSequence);
        Assert.False(_inputHandler.HasPendingSequence);
    }

    [Fact]
    public void ProcessKeyEvent_UnknownKey_ReturnsFalse()
    {
        var result = _inputHandler.ProcessKeyEvent((Key)'x');

        Assert.False(result);
        Assert.Empty(_capturedResults);
    }

    public void Dispose()
    {
        _inputHandler?.Dispose();
    }
}