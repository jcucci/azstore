using Microsoft.Extensions.DependencyInjection;

namespace AzStore.Terminal.Commands;

public class CommandRegistry : ICommandRegistry
{
    private readonly IServiceProvider _serviceProvider;
    private Dictionary<string, ICommand>? _commands;

    public CommandRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private Dictionary<string, ICommand> Commands => 
        _commands ??= BuildCommandLookup(_serviceProvider.GetServices<ICommand>());

    public ICommand? FindCommand(string commandName)
    {
        if (string.IsNullOrWhiteSpace(commandName))
            return null;
            
        var normalizedName = commandName.TrimStart(':');
        return Commands.TryGetValue(normalizedName, out var command) ? command : null;
    }

    public IEnumerable<ICommand> GetAllCommands()
    {
        return Commands.Values.Distinct();
    }

    private static Dictionary<string, ICommand> BuildCommandLookup(IEnumerable<ICommand> commands)
    {
        var lookup = new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var command in commands)
        {
            lookup[command.Name] = command;
            
            foreach (var alias in command.Aliases)
            {
                lookup[alias] = command;
            }
        }
        
        return lookup;
    }
}