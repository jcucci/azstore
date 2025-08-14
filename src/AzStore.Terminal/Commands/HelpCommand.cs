using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzStore.Terminal.Commands;

public class HelpCommand : ICommand
{
    private readonly ILogger<HelpCommand> _logger;
    private readonly IServiceProvider _serviceProvider;

    public string Name => "help";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Show this help message";

    public HelpCommand(ILogger<HelpCommand> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public Task<CommandResult> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("User requested help");
        
        var helpText = BuildHelpText();
        return Task.FromResult(CommandResult.Ok(helpText));
    }

    private string BuildHelpText()
    {
        var commands = _serviceProvider.GetServices<ICommand>();
        var helpLines = commands.Select(FormatCommandHelp);
        
        return "Available commands:\n" + string.Join("\n", helpLines);
    }

    private static string FormatCommandHelp(ICommand command)
    {
        var commandNames = $":{command.Name}";
        
        if (command.Aliases.Length > 0)
        {
            var aliases = string.Join(", :", command.Aliases);
            commandNames += $", :{aliases}";
        }
        
        return $"  {commandNames} - {command.Description}";
    }
}