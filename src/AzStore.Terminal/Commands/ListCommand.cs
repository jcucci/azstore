using Microsoft.Extensions.Logging;

namespace AzStore.Terminal.Commands;

public class ListCommand : ICommand
{
    private readonly ILogger<ListCommand> _logger;

    public string Name => "list";
    public string[] Aliases => ["ls"];
    public string Description => "List downloaded files";

    public ListCommand(ILogger<ListCommand> logger)
    {
        _logger = logger;
    }

    public Task<CommandResult> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("User requested file list");
        
        // TODO: Implement actual file listing logic
        return Task.FromResult(CommandResult.Ok("No files downloaded yet."));
    }
}