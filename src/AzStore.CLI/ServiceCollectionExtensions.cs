using AzStore.Configuration;
using AzStore.Core;
using AzStore.Terminal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AzStore.CLI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzStoreServices(this IServiceCollection services)
    {
        services.AddOptions<AzStoreSettings>()
            .BindConfiguration(AzStoreSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<BlobService>();
        services.AddSingleton<ReplEngine>();

        services.AddHostedService<ReplHostedService>();

        return services;
    }
}