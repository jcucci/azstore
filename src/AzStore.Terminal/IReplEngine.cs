namespace AzStore.Terminal;

/// <summary>
/// Provides the core REPL (Read-Eval-Print Loop) functionality for the Azure Blob Storage CLI.
/// </summary>
public interface IReplEngine
{
    /// <summary>
    /// Starts the REPL session and begins processing user input.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the REPL session.</param>
    /// <returns>A task representing the asynchronous REPL session.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the REPL is not properly configured.</exception>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Displays a message to the user in the standard info format.
    /// </summary>
    /// <param name="message">The message to display.</param>
    void WriteInfo(string message);

    /// <summary>
    /// Displays an error message to the user in the error format.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    void WriteError(string message);

    /// <summary>
    /// Displays a status message to the user in the status format.
    /// </summary>
    /// <param name="message">The status message to display.</param>
    void WriteStatus(string message);

    /// <summary>
    /// Displays a prompt to the user for input.
    /// </summary>
    /// <param name="prompt">The prompt text to display.</param>
    void WritePrompt(string prompt);

    /// <summary>
    /// Displays a colored message to the user.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="colorName">The name of the console color to use.</param>
    void WriteColored(string message, string colorName);

    /// <summary>
    /// Processes a single command input without starting the full REPL session.
    /// </summary>
    /// <param name="input">The command input to process.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>true if the command should cause the REPL to exit; otherwise, false.</returns>
    Task<bool> ProcessInputAsync(string input, CancellationToken cancellationToken = default);
}