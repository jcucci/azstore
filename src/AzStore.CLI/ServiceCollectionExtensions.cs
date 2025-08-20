using AzStore.Configuration;
using AzStore.Core;
using AzStore.Terminal;
using AzStore.Terminal.Commands;
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

        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IStorageService, AzureStorageService>();
        services.AddSingleton<IReplEngine, ReplEngine>();

        services.AddTransient<ICommand, ExitCommand>();
        services.AddTransient<ICommand, HelpCommand>();
        services.AddTransient<ICommand, ListCommand>();
        services.AddSingleton<ICommandRegistry, CommandRegistry>();

        services.AddHostedService<ReplHostedService>();

        return services;
    }
}