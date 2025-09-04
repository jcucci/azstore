using AzStore.Configuration;
using AzStore.Core;
using AzStore.Core.Services.Abstractions;
using AzStore.Terminal;
using AzStore.Terminal.Commands;
using AzStore.Terminal.Navigation;
using AzStore.Terminal.Repl;
using AzStore.Terminal.Theming;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace AzStore.Terminal.Tests;

public static class ReplEngineFixture
{
    public static ReplEngine CreateWithDefaults()
    {
        var settings = new AzStoreSettings();
        var options = Substitute.For<IOptions<AzStoreSettings>>();
        options.Value.Returns(settings);

        var logger = Substitute.For<ILogger<ReplEngine>>();
        var commandRegistry = Substitute.For<ICommandRegistry>();
        var sessionManager = Substitute.For<ISessionManager>();
        var navigationEngine = Substitute.For<INavigationEngine>();
        var theme = Substitute.For<IThemeService>();

        return new ReplEngine(options, logger, commandRegistry, sessionManager, navigationEngine, theme);
    }

    public static ReplEngine CreateWithCustomTheme(string promptColor)
    {
        var settings = new AzStoreSettings
        {
            Theme = new ThemeSettings { PromptColor = promptColor }
        };

        var options = Substitute.For<IOptions<AzStoreSettings>>();
        options.Value.Returns(settings);

        var logger = Substitute.For<ILogger<ReplEngine>>();
        var commandRegistry = Substitute.For<ICommandRegistry>();
        var sessionManager = Substitute.For<ISessionManager>();
        var navigationEngine = Substitute.For<INavigationEngine>();
        var theme = Substitute.For<IThemeService>();

        return new ReplEngine(options, logger, commandRegistry, sessionManager, navigationEngine, theme);
    }
}
