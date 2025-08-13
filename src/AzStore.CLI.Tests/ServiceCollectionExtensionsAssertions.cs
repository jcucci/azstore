using AzStore.Configuration;
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
        
        Assert.NotNull(serviceProvider.GetService<BlobService>());
        Assert.NotNull(serviceProvider.GetService<ReplEngine>());
    }
}