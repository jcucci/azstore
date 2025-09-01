namespace AzStore.Terminal.Input;

/// <summary>
/// Buffers key sequences to support multi-character key bindings like 'gg', etc.
/// </summary>
public class KeySequenceBuffer
{
    private string _currentSequence = string.Empty;
    private DateTime _lastKeyTime = DateTime.MinValue;
    private readonly int _timeoutMs;

    public KeySequenceBuffer(int timeoutMs = 1000)
    {
        _timeoutMs = timeoutMs;
    }

    /// <summary>
    /// Gets the current partial key sequence.
    /// </summary>
    public string CurrentSequence => _currentSequence;

    /// <summary>
    /// Adds a key to the current sequence and checks if it matches any of the provided bindings.
    /// </summary>
    /// <param name="key">The key character to add</param>
    /// <param name="bindings">Dictionary of key bindings to match against</param>
    /// <returns>
    /// A tuple containing:
    /// - isComplete: true if a complete binding was matched
    /// - matchedBinding: the matched binding action (null if no match)
    /// - hasPartialMatch: true if the current sequence is a prefix of any binding
    /// </returns>
    public (bool isComplete, KeyBindingAction? matchedBinding, bool hasPartialMatch) AddKey(char key, IReadOnlyDictionary<KeyBindingAction, string> bindings)
    {
        var now = DateTime.UtcNow;

        // Check if we should complete a pending sequence due to timeout
        if (_currentSequence.Length > 0 && (now - _lastKeyTime).TotalMilliseconds > _timeoutMs)
        {
            // Try to complete the current sequence before clearing it
            var timedOutMatch = bindings.FirstOrDefault(kvp => kvp.Value == _currentSequence).Key;
            if (timedOutMatch != default(KeyBindingAction))
            {
                _currentSequence = string.Empty;
                return (true, timedOutMatch, false);
            }
            _currentSequence = string.Empty;
        }

        _currentSequence += key;
        _lastKeyTime = now;

        var hasPartialMatch = bindings.Values.Any(binding =>
            binding.Length > _currentSequence.Length &&
            binding.StartsWith(_currentSequence, StringComparison.Ordinal));

        var matchedBinding = bindings.FirstOrDefault(kvp => kvp.Value == _currentSequence);
        if (!string.IsNullOrEmpty(matchedBinding.Value))
        {
            // If there are partial matches, don't complete yet - wait for more input
            if (hasPartialMatch)
            {
                return (false, null, true);
            }

            // Complete match found with no ambiguity - clear sequence
            _currentSequence = string.Empty;
            return (true, matchedBinding.Key, false);
        }

        // If no partial matches, clear sequence and try single key
        if (!hasPartialMatch)
        {
            _currentSequence = string.Empty;

            // Check if single key matches any binding
            var singleKeyMatch = bindings.FirstOrDefault(kvp => kvp.Value == key.ToString());
            if (!string.IsNullOrEmpty(singleKeyMatch.Value))
            {
                return (true, singleKeyMatch.Key, false);
            }
        }

        return (false, null, hasPartialMatch);
    }

    /// <summary>
    /// Clears the current key sequence buffer.
    /// </summary>
    public void Clear()
    {
        _currentSequence = string.Empty;
        _lastKeyTime = DateTime.MinValue;
    }

    /// <summary>
    /// Checks if the current sequence has timed out.
    /// </summary>
    public bool HasTimedOut()
    {
        if (_currentSequence.Length == 0) return false;

        return (DateTime.UtcNow - _lastKeyTime).TotalMilliseconds > _timeoutMs;
    }
}