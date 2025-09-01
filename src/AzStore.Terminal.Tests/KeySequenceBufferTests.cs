using AzStore.Terminal;
using Xunit;

namespace AzStore.Terminal.Tests;

[Trait("Category", "Unit")]
public class KeySequenceBufferTests
{
    private readonly Dictionary<KeyBindingAction, string> _testBindings = new()
    {
        { KeyBindingAction.MoveDown, "j" },
        { KeyBindingAction.MoveUp, "k" },
        { KeyBindingAction.Enter, "l" },
        { KeyBindingAction.Top, "gg" },
        { KeyBindingAction.Bottom, "G" },
        { KeyBindingAction.Download, "d" },
        { KeyBindingAction.Command, ":" }
    };

    [Fact]
    public void AddKey_SingleCharacterBinding_ReturnsCompleteMatch()
    {
        var buffer = new KeySequenceBuffer();

        var result = buffer.AddKey('j', _testBindings);

        Assert.True(result.isComplete);
        Assert.Equal(KeyBindingAction.MoveDown, result.matchedBinding);
        Assert.False(result.hasPartialMatch);
    }

    [Fact]
    public void AddKey_FirstCharacterOfMultiChar_ReturnsPartialMatch()
    {
        var buffer = new KeySequenceBuffer();

        var result = buffer.AddKey('g', _testBindings);

        Assert.False(result.isComplete);
        Assert.Null(result.matchedBinding);
        Assert.True(result.hasPartialMatch);
        Assert.Equal("g", buffer.CurrentSequence);
    }

    [Fact]
    public void AddKey_CompleteTwoCharacterSequence_ReturnsCompleteMatch()
    {
        var buffer = new KeySequenceBuffer();

        // First 'g'
        var firstResult = buffer.AddKey('g', _testBindings);
        Assert.False(firstResult.isComplete);
        Assert.True(firstResult.hasPartialMatch);

        // Second 'g'
        var secondResult = buffer.AddKey('g', _testBindings);
        Assert.True(secondResult.isComplete);
        Assert.Equal(KeyBindingAction.Top, secondResult.matchedBinding);
        Assert.False(secondResult.hasPartialMatch);
        Assert.Equal(string.Empty, buffer.CurrentSequence); // Should be cleared after match
    }

    [Fact]
    public void AddKey_DifferentTwoCharacterSequence_ReturnsCompleteMatch()
    {
        var buffer = new KeySequenceBuffer();

        // First 'd'
        var firstResult = buffer.AddKey('d', _testBindings);
        Assert.False(firstResult.isComplete);
        Assert.True(firstResult.hasPartialMatch);

        // Second 'd'
        var secondResult = buffer.AddKey('d', _testBindings);
        Assert.True(secondResult.isComplete);
        Assert.Equal(KeyBindingAction.Download, secondResult.matchedBinding);
        Assert.False(secondResult.hasPartialMatch);
    }

    [Fact]
    public void AddKey_InvalidSequence_ClearsBufferAndChecksSingleKey()
    {
        var buffer = new KeySequenceBuffer();

        // Start with 'g'
        buffer.AddKey('g', _testBindings);
        Assert.Equal("g", buffer.CurrentSequence);

        // Add invalid second character 'x'
        var result = buffer.AddKey('x', _testBindings);
        Assert.False(result.isComplete);
        Assert.Null(result.matchedBinding);
        Assert.False(result.hasPartialMatch);
        Assert.Equal(string.Empty, buffer.CurrentSequence); // Should be cleared
    }

    [Fact]
    public void AddKey_InvalidSequenceFollowedByValidSingle_ReturnsMatch()
    {
        var buffer = new KeySequenceBuffer();

        var bindings = new Dictionary<KeyBindingAction, string>(_testBindings)
        {
            { KeyBindingAction.Search, "x" }
        };

        // Start with 'g'
        buffer.AddKey('g', bindings);

        // Add 'x' which should clear buffer and match as single char
        var result = buffer.AddKey('x', bindings);
        Assert.True(result.isComplete);
        Assert.Equal(KeyBindingAction.Search, result.matchedBinding);
        Assert.False(result.hasPartialMatch);
    }

    [Fact]
    public void AddKey_TimeoutClearsSequence()
    {
        var buffer = new KeySequenceBuffer(100); // 100ms timeout

        // First 'g'
        buffer.AddKey('g', _testBindings);
        Assert.Equal("g", buffer.CurrentSequence);

        // Wait for timeout
        Thread.Sleep(150);

        // Second 'g' should start new sequence, not complete 'gg'
        var result = buffer.AddKey('g', _testBindings);
        Assert.False(result.isComplete);
        Assert.True(result.hasPartialMatch);
        Assert.Equal("g", buffer.CurrentSequence); // Should be just 'g', not 'gg'
    }

    [Fact]
    public void Clear_EmptiesCurrentSequence()
    {
        var buffer = new KeySequenceBuffer();

        buffer.AddKey('g', _testBindings);
        Assert.Equal("g", buffer.CurrentSequence);

        buffer.Clear();
        Assert.Equal(string.Empty, buffer.CurrentSequence);
    }

    [Fact]
    public void HasTimedOut_WithEmptySequence_ReturnsFalse()
    {
        var buffer = new KeySequenceBuffer();

        Assert.False(buffer.HasTimedOut());
    }

    [Fact]
    public void HasTimedOut_WithinTimeout_ReturnsFalse()
    {
        var buffer = new KeySequenceBuffer(1000);

        buffer.AddKey('g', _testBindings);
        Assert.False(buffer.HasTimedOut());
    }

    [Fact]
    public void HasTimedOut_AfterTimeout_ReturnsTrue()
    {
        var buffer = new KeySequenceBuffer(50);

        buffer.AddKey('g', _testBindings);
        Thread.Sleep(100);
        Assert.True(buffer.HasTimedOut());
    }

    [Fact]
    public void AddKey_PrefixConflictResolution_HandledCorrectly()
    {
        var conflictBindings = new Dictionary<KeyBindingAction, string>
        {
            { KeyBindingAction.MoveUp, "g" },
            { KeyBindingAction.Top, "gg" }
        };

        var buffer = new KeySequenceBuffer();

        // Add first 'g' - should have partial match due to 'gg' possibility
        var firstResult = buffer.AddKey('g', conflictBindings);
        Assert.False(firstResult.isComplete);
        Assert.True(firstResult.hasPartialMatch);

        // Add second 'g' - should complete 'gg' match
        var secondResult = buffer.AddKey('g', conflictBindings);
        Assert.True(secondResult.isComplete);
        Assert.Equal(KeyBindingAction.Top, secondResult.matchedBinding);
    }

    [Fact]
    public void AddKey_SingleCharAfterTimeout_CompletesTimedOutMatch()
    {
        var conflictBindings = new Dictionary<KeyBindingAction, string>
        {
            { KeyBindingAction.MoveUp, "g" },
            { KeyBindingAction.Top, "gg" }
        };

        var buffer = new KeySequenceBuffer(50);

        // Add first 'g' - should return partial match
        var firstResult = buffer.AddKey('g', conflictBindings);
        Assert.False(firstResult.isComplete);
        Assert.True(firstResult.hasPartialMatch);

        // Wait for timeout
        Thread.Sleep(100);

        // Add another key after timeout - should complete the timed out 'g' first
        var result = buffer.AddKey('x', conflictBindings);
        Assert.True(result.isComplete);
        Assert.Equal(KeyBindingAction.MoveUp, result.matchedBinding);
    }

    [Fact]
    public void AddKey_CaseSensitiveMatching_WorksCorrectly()
    {
        var caseSensitiveBindings = new Dictionary<KeyBindingAction, string>
        {
            { KeyBindingAction.MoveUp, "g" },
            { KeyBindingAction.Bottom, "G" }
        };

        var buffer = new KeySequenceBuffer();

        var lowerResult = buffer.AddKey('g', caseSensitiveBindings);
        Assert.True(lowerResult.isComplete);
        Assert.Equal(KeyBindingAction.MoveUp, lowerResult.matchedBinding);

        var upperResult = buffer.AddKey('G', caseSensitiveBindings);
        Assert.True(upperResult.isComplete);
        Assert.Equal(KeyBindingAction.Bottom, upperResult.matchedBinding);
    }
}