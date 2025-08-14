namespace AzStore.Terminal.Commands;

public interface ICommandRegistry
{
    ICommand? FindCommand(string commandName);
    IEnumerable<ICommand> GetAllCommands();
}