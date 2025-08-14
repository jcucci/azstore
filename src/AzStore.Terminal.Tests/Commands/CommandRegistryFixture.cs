using AzStore.Terminal.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AzStore.Terminal.Tests.Commands;

public static class CommandRegistryFixture
{
    public static CommandRegistry CreateWithCommands()
    {
        var services = new ServiceCollection();
        
        services.AddTransient<ILogger<ExitCommand>>(_ => Substitute.For<ILogger<ExitCommand>>());
        services.AddTransient<ILogger<ListCommand>>(_ => Substitute.For<ILogger<ListCommand>>());
        
        // Create mock commands to avoid circular dependency with HelpCommand
        services.AddTransient<ICommand>(_ => CreateExitMockCommand());
        services.AddTransient<ICommand>(_ => CreateHelpMockCommand());
        services.AddTransient<ICommand>(_ => CreateListMockCommand());
        
        var serviceProvider = services.BuildServiceProvider();
        return new CommandRegistry(serviceProvider);
    }

    public static CommandRegistry CreateEmpty()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetServices<ICommand>().Returns(Array.Empty<ICommand>());
        
        return new CommandRegistry(serviceProvider);
    }

    private static ICommand CreateExitMockCommand()
    {
        var command = Substitute.For<ICommand>();
        command.Name.Returns("exit");
        command.Aliases.Returns(["q"]);
        command.Description.Returns("Mock exit command");
        return command;
    }

    private static ICommand CreateHelpMockCommand()
    {
        var command = Substitute.For<ICommand>();
        command.Name.Returns("help");
        command.Aliases.Returns(Array.Empty<string>());
        command.Description.Returns("Mock help command");
        return command;
    }

    private static ICommand CreateListMockCommand()
    {
        var command = Substitute.For<ICommand>();
        command.Name.Returns("list");
        command.Aliases.Returns(["ls"]);
        command.Description.Returns("Mock list command");
        return command;
    }
}