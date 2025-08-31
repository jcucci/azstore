namespace AzStore.Terminal;

/// <summary>
/// Event arguments for navigation errors.
/// </summary>
public class NavigationErrorEventArgs : EventArgs
{
    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the exception that caused the error, if available.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Initializes a new instance of the NavigationErrorEventArgs class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="exception">The exception that caused the error.</param>
    public NavigationErrorEventArgs(string message, Exception? exception = null)
    {
        Message = message;
        Exception = exception;
    }
}