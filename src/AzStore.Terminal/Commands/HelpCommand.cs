using Microsoft.Extensions.Logging;
using AzStore.Configuration;

namespace AzStore.Terminal.Commands;

public class HelpCommand : ICommand
{
    private readonly ILogger<HelpCommand> _logger;
    private readonly ICommandRegistry _commandRegistry;
    private readonly KeyBindings _keyBindings;

    public string Name => "help";
    public string[] Aliases => [];
    public string Description => "Show help and navigation keybindings";

    public HelpCommand(ILogger<HelpCommand> logger, ICommandRegistry commandRegistry, KeyBindings keyBindings)
    {
        _logger = logger;
        _commandRegistry = commandRegistry;
        _keyBindings = keyBindings;
    }

    public Task<CommandResult> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("User requested help");

        var helpText = BuildHelpText();
        return Task.FromResult(CommandResult.Ok(helpText));
    }

    private string BuildHelpText()
    {
        var commands = _commandRegistry.GetAllCommands();
        var helpLines = commands.Select(FormatCommandHelp);

        var commandsSection = "Available commands:\n" + string.Join("\n", helpLines);
        var navigationSection = BuildNavigationHelp();

        var generator = new Utilities.HelpTextGenerator(_keyBindings);
        var quickRef = generator.GenerateQuickReference();
        var tip = "Tip: Press '?' for interactive help with scrolling and search.";

        return string.Join("\n\n", new[] { commandsSection, navigationSection, "Quick Reference:", quickRef, tip });
    }

    private string BuildNavigationHelp()
    {
        return $"""
            Navigation keybindings:
              {_keyBindings.MoveDown}/{_keyBindings.MoveUp} - Move down/up in list
              {_keyBindings.Enter}/Enter/→ - Select item or navigate into container
              {_keyBindings.Back}/← - Navigate back/up one level
              {_keyBindings.Top} - Jump to top of list
              {_keyBindings.Bottom} - Jump to bottom of list
              {_keyBindings.Download} - Download selected item
              {_keyBindings.Search} - Enter search mode
              {_keyBindings.Command} - Enter command mode
              Esc - Cancel current operation
            """;
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
