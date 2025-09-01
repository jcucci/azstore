using AzStore.Terminal.Commands;

namespace AzStore.Terminal.Repl;

/// <summary>
/// Provides command processing functionality for the REPL engine.
/// </summary>
public interface ICommandProcessor
{
    /// <summary>
    /// Processes a command input string and executes the appropriate command.
    /// </summary>
    /// <param name="input">The raw command input string from the user.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The result of executing the command.</returns>
    /// <exception cref="ArgumentException">Thrown when input is null or empty.</exception>
    Task<CommandResult> ProcessCommandAsync(string input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses a command input string into command name and arguments.
    /// </summary>
    /// <param name="input">The raw command input string.</param>
    /// <returns>A tuple containing the command name and arguments array.</returns>
    (string commandName, string[] args) ParseCommand(string input);

    /// <summary>
    /// Validates that a command input string is properly formatted.
    /// </summary>
    /// <param name="input">The command input to validate.</param>
    /// <returns>true if the input is a valid command format; otherwise, false.</returns>
    bool IsValidCommand(string input);

    /// <summary>
    /// Gets help text for a specific command, or general help if no command is specified.
    /// </summary>
    /// <param name="commandName">The name of the command to get help for, or null for general help.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Help text for the specified command or general help.</returns>
    Task<string> GetHelpAsync(string? commandName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of all available command names.
    /// </summary>
    /// <returns>A collection of available command names.</returns>
    IEnumerable<string> GetAvailableCommands();

    /// <summary>
    /// Gets detailed information about a specific command.
    /// </summary>
    /// <param name="commandName">The name of the command to get information for.</param>
    /// <returns>The command instance, or null if not found.</returns>
    ICommand? GetCommand(string commandName);

    /// <summary>
    /// Registers a new command with the processor.
    /// </summary>
    /// <param name="command">The command to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when command is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a command with the same name or alias is already registered.</exception>
    void RegisterCommand(ICommand command);

    /// <summary>
    /// Unregisters a command from the processor.
    /// </summary>
    /// <param name="commandName">The name of the command to unregister.</param>
    /// <returns>true if the command was found and removed; otherwise, false.</returns>
    bool UnregisterCommand(string commandName);

    /// <summary>
    /// Checks if a command with the specified name is registered.
    /// </summary>
    /// <param name="commandName">The name of the command to check for.</param>
    /// <returns>true if the command is registered; otherwise, false.</returns>
    bool IsCommandRegistered(string commandName);

    /// <summary>
    /// Provides command name completion suggestions based on partial input.
    /// </summary>
    /// <param name="partialCommand">The partial command name to complete.</param>
    /// <returns>A collection of matching command names.</returns>
    IEnumerable<string> GetCommandCompletions(string partialCommand);
}