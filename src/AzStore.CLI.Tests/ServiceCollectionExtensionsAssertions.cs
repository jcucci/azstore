using AzStore.Core;
using AzStore.Terminal;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AzStore.CLI.Tests;

public static class ServiceCollectionExtensionsAssertions
{
    public static void ServicesRegistered(IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetService<AzureStorageService>());
        Assert.NotNull(serviceProvider.GetService<IReplEngine>());
    }
}