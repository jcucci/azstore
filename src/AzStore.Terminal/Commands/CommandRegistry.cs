using Microsoft.Extensions.DependencyInjection;

namespace AzStore.Terminal.Commands;

public class CommandRegistry : ICommandRegistry
{
    private readonly Dictionary<string, ICommand> _commands;

    public CommandRegistry(IServiceProvider serviceProvider)
    {
        var commands = serviceProvider.GetServices<ICommand>();
        _commands = BuildCommandLookup(commands);
    }

    public ICommand? FindCommand(string commandName)
    {
        var normalizedName = commandName.TrimStart(':').ToLowerInvariant();
        return _commands.TryGetValue(normalizedName, out var command) ? command : null;
    }

    public IEnumerable<ICommand> GetAllCommands()
    {
        return _commands.Values.Distinct();
    }

    private static Dictionary<string, ICommand> BuildCommandLookup(IEnumerable<ICommand> commands)
    {
        var lookup = new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var command in commands)
        {
            lookup[command.Name.ToLowerInvariant()] = command;
            
            foreach (var alias in command.Aliases)
            {
                lookup[alias.ToLowerInvariant()] = command;
            }
        }
        
        return lookup;
    }
}