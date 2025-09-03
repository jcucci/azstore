using AzStore.Core.Services.Abstractions;
using AzStore.Terminal.Repl;
using AzStore.Terminal.UI;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AzStore.CLI.Tests;

public static class ServiceCollectionExtensionsAssertions
{
    public static void ServicesRegistered(IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetService<IStorageService>());
        Assert.NotNull(serviceProvider.GetService<IReplEngine>());
        Assert.NotNull(serviceProvider.GetService<ITerminalUI>());
        Assert.NotNull(serviceProvider.GetService<IFileConflictResolver>());
    }
}
