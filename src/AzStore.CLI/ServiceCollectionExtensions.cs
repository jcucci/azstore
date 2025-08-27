using AzStore.Configuration;
using AzStore.Core;
using AzStore.Terminal;
using AzStore.Terminal.Commands;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddSingleton<IPathService, PathService>();
        services.AddSingleton<IStorageService, AzureStorageService>();
        services.AddSingleton<IReplEngine, ReplEngine>();
        services.AddSingleton<ITerminalUI, TerminalGuiUI>();

        services.AddTransient<ICommand, ExitCommand>();
        services.AddTransient<ICommand, HelpCommand>();
        services.AddTransient<ICommand, ListCommand>();
        services.AddTransient<ICommand, DownloadCommand>();
        services.AddSingleton<ICommandRegistry, CommandRegistry>();

        services.AddHostedService<ReplHostedService>();

        return services;
    }
}