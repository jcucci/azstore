using Microsoft.Extensions.Logging;

namespace AzStore.Terminal.Commands;

public class ExitCommand : ICommand
{
    private readonly ILogger<ExitCommand> _logger;

    public string Name => "exit";
    public string[] Aliases => new[] { "q" };
    public string Description => "Exit the application";

    public ExitCommand(ILogger<ExitCommand> logger)
    {
        _logger = logger;
    }

    public Task<CommandResult> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User initiated exit via command");
        return Task.FromResult(CommandResult.Exit("Goodbye!"));
    }
}