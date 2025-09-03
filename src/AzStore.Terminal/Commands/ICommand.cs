namespace AzStore.Terminal.Commands;

public interface ICommand
{
    string Name { get; }
    string[] Aliases { get; }
    string Description { get; }
    Task<CommandResult> ExecuteAsync(string[] args, CancellationToken cancellationToken = default);
}
