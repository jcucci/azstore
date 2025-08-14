using AzStore.Configuration;
using AzStore.Terminal;
using AzStore.Terminal.Commands;
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
        
        return new ReplEngine(options, logger, commandRegistry);
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
        
        return new ReplEngine(options, logger, commandRegistry);
    }
}