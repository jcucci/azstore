namespace AzStore.Terminal.Commands;

public interface ICommand
{
    string Name { get; }
    string[] Aliases { get; }
    string Description { get; }
    Task<CommandResult> ExecuteAsync(string[] args, CancellationToken cancellationToken = default);
}

public class CommandResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public bool ShouldExit { get; init; }

    public static CommandResult Ok(string? message = null) => new() { Success = true, Message = message };
    public static CommandResult Error(string message) => new() { Success = false, Message = message };
    public static CommandResult Exit(string? message = null) => new() { Success = true, Message = message, ShouldExit = true };
}