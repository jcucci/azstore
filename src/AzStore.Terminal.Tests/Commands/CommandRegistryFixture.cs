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
        services.AddTransient<ILogger<HelpCommand>>(_ => Substitute.For<ILogger<HelpCommand>>());
        services.AddTransient<ILogger<ListCommand>>(_ => Substitute.For<ILogger<ListCommand>>());
        services.AddTransient<IServiceProvider>(sp => sp);
        
        services.AddTransient<ICommand, ExitCommand>();
        services.AddTransient<ICommand, HelpCommand>();
        services.AddTransient<ICommand, ListCommand>();
        
        var serviceProvider = services.BuildServiceProvider();
        return new CommandRegistry(serviceProvider);
    }

    public static CommandRegistry CreateEmpty()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetServices<ICommand>().Returns(Array.Empty<ICommand>());
        
        return new CommandRegistry(serviceProvider);
    }
}