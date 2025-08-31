using AzStore.Core.Models;

namespace AzStore.Terminal;

/// <summary>
/// Event arguments for navigation state changes.
/// </summary>
public class NavigationStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous navigation state.
    /// </summary>
    public NavigationState? PreviousState { get; }

    /// <summary>
    /// Gets the new navigation state.
    /// </summary>
    public NavigationState CurrentState { get; }

    /// <summary>
    /// Initializes a new instance of the NavigationStateChangedEventArgs class.
    /// </summary>
    /// <param name="previousState">The previous navigation state.</param>
    /// <param name="currentState">The new navigation state.</param>
    public NavigationStateChangedEventArgs(NavigationState? previousState, NavigationState currentState)
    {
        PreviousState = previousState;
        CurrentState = currentState;
    }
}