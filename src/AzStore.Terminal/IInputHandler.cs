using Terminal.Gui;

namespace AzStore.Terminal;

/// <summary>
/// Interface for handling keyboard input and key sequences.
/// Provides abstraction for VIM-like key handling with support for
/// single keys, multi-character sequences, special keys, and key repeat.
/// </summary>
public interface IInputHandler : IDisposable
{
    /// <summary>
    /// Event raised when a navigation action should be performed.
    /// </summary>
    event EventHandler<NavigationResult>? NavigationRequested;

    /// <summary>
    /// Processes a keyboard event and determines the appropriate action.
    /// </summary>
    /// <param name="keyEvent">The keyboard event to process</param>
    /// <returns>True if the key was handled, false otherwise</returns>
    bool ProcessKeyEvent(Key keyEvent);

    /// <summary>
    /// Clears any pending key sequences or input state.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets the current partial key sequence being built.
    /// </summary>
    string CurrentSequence { get; }

    /// <summary>
    /// Checks if there are any pending key sequences that may timeout.
    /// </summary>
    bool HasPendingSequence { get; }
}