namespace AzStore.Terminal.Commands;

public class CommandResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public bool ShouldExit { get; init; }

    public static CommandResult Ok(string? message = null) => new() { Success = true, Message = message };
    public static CommandResult Error(string message) => new() { Success = false, Message = message };
    public static CommandResult Exit(string? message = null) => new() { Success = true, Message = message, ShouldExit = true };
}

