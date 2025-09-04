using AzStore.Configuration;
using AzStore.Core;
using AzStore.Core.Services.Abstractions;
using AzStore.Core.Services.Implementations;
using AzStore.Terminal;
using AzStore.Terminal.Commands;
using AzStore.Terminal.Input;
using AzStore.Terminal.Navigation;
using AzStore.Terminal.Repl;
using AzStore.Terminal.UI;
using AzStore.Terminal.Utilities;
using AzStore.Terminal.Selection;
using AzStore.Terminal.Theming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        // Provide strongly-typed settings sub-objects for DI consumers
        services.AddSingleton<KeyBindings>(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<AzStoreSettings>>();
            return settings.Value.KeyBindings;
        });

        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IPathService, PathService>();
        services.AddSingleton<IStorageService, AzureStorageService>();
        services.AddSingleton<ISessionManager, SessionManager>();
        services.AddSingleton<VimNavigator>();
        services.AddSingleton<INavigationEngine, NavigationEngine>();
        services.AddSingleton<HelpTextGenerator>(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<AzStoreSettings>>();
            return new HelpTextGenerator(settings.Value.KeyBindings);
        });
        services.AddSingleton<IDownloadActivity, DownloadActivity>();
        services.AddSingleton<IReplEngine, ReplEngine>();
        services.AddSingleton<IFileConflictResolver, ConsoleFileConflictResolver>();
        services.AddSingleton<IFuzzyMatcher, SimpleFuzzyMatcher>();
        services.AddSingleton<IAccountSelectionService, ConsoleAccountSelectionService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IInputHandler>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<InputHandler>>();
            var settings = provider.GetRequiredService<IOptions<AzStoreSettings>>();
            return new InputHandler(logger, settings.Value.KeyBindings);
        });
        services.AddSingleton<ITerminalUI, TerminalGuiUI>();

        services.AddTransient<ICommand, ExitCommand>();
        services.AddTransient<ICommand, HelpCommand>();
        services.AddTransient<ICommand, ListCommand>();
        services.AddTransient<ICommand, DownloadCommand>();
        services.AddTransient<ICommand, SessionCommand>();
        services.AddSingleton<ICommandRegistry, CommandRegistry>();

        services.AddHostedService<ReplHostedService>();

        return services;
    }
}
