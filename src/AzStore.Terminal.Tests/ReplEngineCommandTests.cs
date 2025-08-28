using AzStore.Configuration;
using AzStore.Core;
using AzStore.Terminal;
using AzStore.Terminal.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace AzStore.Terminal.Tests;

public class ReplEngineCommandTests
{
    [Fact]
    public void ReplEngine_WithCommandRegistry_InitializesCorrectly()
    {
        var settings = CreateMockSettings();
        var logger = Substitute.For<ILogger<ReplEngine>>();
        var commandRegistry = Substitute.For<ICommandRegistry>();
        var sessionManager = Substitute.For<ISessionManager>();
        var navigationEngine = Substitute.For<INavigationEngine>();
        
        var replEngine = new ReplEngine(settings, logger, commandRegistry, sessionManager, navigationEngine);
        
        Assert.NotNull(replEngine);
    }

    [Fact]
    public void ParseCommandArgs_WithSimpleCommand_ReturnsEmptyArray()
    {
        var result = InvokeParseCommandArgs(":help");
        
        Assert.Empty(result);
    }

    [Fact]
    public void ParseCommandArgs_WithArguments_ReturnsArguments()
    {
        var result = InvokeParseCommandArgs(":command arg1 arg2 arg3");
        
        Assert.Equal(new[] { "arg1", "arg2", "arg3" }, result);
    }

    [Fact]
    public void ParseCommandArgs_WithExtraSpaces_IgnoresExtraSpaces()
    {
        var result = InvokeParseCommandArgs(":command  arg1   arg2  ");
        
        Assert.Equal(new[] { "arg1", "arg2" }, result);
    }

    [Fact]
    public void ParseCommandArgs_WithEmptyInput_ReturnsEmptyArray()
    {
        var result = InvokeParseCommandArgs("");
        
        Assert.Empty(result);
    }

    private static IOptions<AzStoreSettings> CreateMockSettings()
    {
        var settings = new AzStoreSettings();
        var options = Substitute.For<IOptions<AzStoreSettings>>();
        options.Value.Returns(settings);
        return options;
    }

    private static string[] InvokeParseCommandArgs(string input)
    {
        var type = typeof(ReplEngine);
        var method = type.GetMethod("ParseCommandArgs", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        if (method == null)
            throw new InvalidOperationException("ParseCommandArgs method not found");
            
        var result = method.Invoke(null, new object[] { input });
        return (string[])(result ?? Array.Empty<string>());
    }
}