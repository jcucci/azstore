using AzStore.Configuration;
using AzStore.Core.Services.Abstractions;
using AzStore.Terminal.Commands;
using AzStore.Terminal.Navigation;
using AzStore.Terminal.Repl;
using AzStore.Terminal.Theming;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace AzStore.Terminal.Tests;

public class ReplEngineExitTests
{
    private static IOptions<AzStoreSettings> CreateMockSettings()
    {
        var settings = new AzStoreSettings();
        var options = Substitute.For<IOptions<AzStoreSettings>>();
        options.Value.Returns(settings);
        return options;
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ProcessInputAsync_ExitCommand_ReturnsShouldExit()
    {
        var settings = CreateMockSettings();
        var logger = Substitute.For<ILogger<ReplEngine>>();
        var sessionManager = Substitute.For<ISessionManager>();
        var navigationEngine = Substitute.For<INavigationEngine>();
        var theme = Substitute.For<IThemeService>();

        var exitCmd = Substitute.For<ICommand>();
        exitCmd.Name.Returns("exit");
        exitCmd.Aliases.Returns(Array.Empty<string>());
        exitCmd.Description.Returns("Exit");
        exitCmd.ExecuteAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(CommandResult.Exit()));

        var registry = Substitute.For<ICommandRegistry>();
        registry.FindCommand("exit").Returns(exitCmd);

        var repl = new ReplEngine(settings, logger, registry, sessionManager, navigationEngine, theme);

        var shouldExit = await repl.ProcessInputAsync(":exit");

        Assert.True(shouldExit);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ProcessInputAsync_ExitForce_AppendsForceArg()
    {
        var settings = CreateMockSettings();
        var logger = Substitute.For<ILogger<ReplEngine>>();
        var sessionManager = Substitute.For<ISessionManager>();
        var navigationEngine = Substitute.For<INavigationEngine>();
        var theme = Substitute.For<IThemeService>();

        string[]? capturedArgs = null;
        var exitCmd = Substitute.For<ICommand>();
        exitCmd.Name.Returns("exit");
        exitCmd.Aliases.Returns(new[] { "q" });
        exitCmd.Description.Returns("Exit");
        exitCmd.ExecuteAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedArgs = ci.ArgAt<string[]>(0);
                return Task.FromResult(CommandResult.Exit());
            });

        var registry = Substitute.For<ICommandRegistry>();
        registry.FindCommand("exit").Returns(exitCmd);
        registry.FindCommand("q").Returns(exitCmd); // alias lookup
        registry.FindCommand(":exit").Returns((ICommand?)null); // ensure lookup uses parsed name

        var repl = new ReplEngine(settings, logger, registry, sessionManager, navigationEngine, theme);

        var shouldExit = await repl.ProcessInputAsync(":q!");

        Assert.True(shouldExit);
        Assert.NotNull(capturedArgs);
        Assert.Contains("--force", capturedArgs!);
    }
}
