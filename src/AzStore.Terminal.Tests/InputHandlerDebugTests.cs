using AzStore.Terminal;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Terminal.Gui;
using Xunit;
using KeyBindingsConfig = AzStore.Configuration.KeyBindings;

namespace AzStore.Terminal.Tests;

[Trait("Category", "Unit")]
public class InputHandlerDebugTests : IDisposable
{
    [Fact]
    public void DebugKeyBindingsLookup()
    {
        var logger = Substitute.For<ILogger<InputHandler>>();
        var keyBindings = new KeyBindingsConfig();
        var inputHandler = new InputHandler(logger, keyBindings);
        
        // Debug the bindings
        var result = inputHandler.ProcessKeyEvent((Key)'j');
        
        // Also test the KeySequenceBuffer directly
        var buffer = new KeySequenceBuffer(1000);
        var bindingsLookup = new Dictionary<KeyBindingAction, string>
        {
            { KeyBindingAction.MoveDown, "j" },
            { KeyBindingAction.MoveUp, "k" },
            { KeyBindingAction.Enter, "l" },
            { KeyBindingAction.Back, "h" },
            { KeyBindingAction.Search, "/" },
            { KeyBindingAction.Command, ":" },
            { KeyBindingAction.Top, "gg" },
            { KeyBindingAction.Bottom, "G" },
            { KeyBindingAction.Download, "dd" }
        };
        
        var (isComplete, matchedBinding, hasPartialMatch) = buffer.AddKey('j', bindingsLookup);
        
        // Log the results
        Assert.True(isComplete, $"Expected complete match for 'j', got isComplete: {isComplete}, matchedBinding: {matchedBinding}, hasPartialMatch: {hasPartialMatch}");
        Assert.Equal(KeyBindingAction.MoveDown, matchedBinding);
    }

    public void Dispose()
    {
        // No cleanup needed for debug test
    }
}