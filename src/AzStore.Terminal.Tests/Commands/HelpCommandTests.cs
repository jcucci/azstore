using AzStore.Terminal.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AzStore.Terminal.Tests.Commands;

public class HelpCommandTests
{
    [Fact]
    public void Name_ReturnsHelp()
    {
        var (logger, serviceProvider) = CreateTestDependenciesWithCommands();
        var command = new HelpCommand(logger, serviceProvider);
        
        Assert.Equal("help", command.Name);
    }

    [Fact]
    public void Aliases_IsEmpty()
    {
        var (logger, serviceProvider) = CreateTestDependenciesWithCommands();
        var command = new HelpCommand(logger, serviceProvider);
        
        Assert.Empty(command.Aliases);
    }

    [Fact]
    public void Description_IsNotEmpty()
    {
        var (logger, serviceProvider) = CreateTestDependenciesWithCommands();
        var command = new HelpCommand(logger, serviceProvider);
        
        Assert.False(string.IsNullOrWhiteSpace(command.Description));
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsSuccessResult()
    {
        var (logger, serviceProvider) = CreateTestDependenciesWithCommands();
        var command = new HelpCommand(logger, serviceProvider);
        
        var result = await command.ExecuteAsync(Array.Empty<string>());
        
        Assert.True(result.Success);
        Assert.False(result.ShouldExit);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_LogsDebugMessage()
    {
        var (logger, serviceProvider) = CreateTestDependenciesWithCommands();
        var command = new HelpCommand(logger, serviceProvider);
        
        await command.ExecuteAsync(Array.Empty<string>());
        
        logger.Received(1).LogDebug("User requested help");
    }

    [Fact]
    public async Task ExecuteAsync_IncludesAllCommands()
    {
        var services = new ServiceCollection();
        services.AddTransient<ICommand>(_ => CreateMockCommand("test1", Array.Empty<string>(), "Test command 1"));
        services.AddTransient<ICommand>(_ => CreateMockCommand("test2", new[] { "t2" }, "Test command 2"));
        
        var serviceProvider = services.BuildServiceProvider();
        var logger = Substitute.For<ILogger<HelpCommand>>();
        var command = new HelpCommand(logger, serviceProvider);
        
        var result = await command.ExecuteAsync(Array.Empty<string>());
        
        Assert.Contains("test1", result.Message);
        Assert.Contains("test2", result.Message);
        Assert.Contains("t2", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_FormatsCommandsCorrectly()
    {
        var services = new ServiceCollection();
        services.AddTransient<ICommand>(_ => CreateMockCommand("test", new[] { "t" }, "Test description"));
        
        var serviceProvider = services.BuildServiceProvider();
        var logger = Substitute.For<ILogger<HelpCommand>>();
        var command = new HelpCommand(logger, serviceProvider);
        
        var result = await command.ExecuteAsync(Array.Empty<string>());
        
        Assert.Contains(":test, :t - Test description", result.Message);
    }

    private static (ILogger<HelpCommand>, IServiceProvider) CreateTestDependenciesWithCommands()
    {
        var services = new ServiceCollection();
        services.AddTransient<ICommand>(_ => CreateMockCommand("test", Array.Empty<string>(), "Test description"));
        
        var serviceProvider = services.BuildServiceProvider();
        var logger = Substitute.For<ILogger<HelpCommand>>();
        
        return (logger, serviceProvider);
    }

    private static ICommand CreateMockCommand(string name, string[] aliases, string description)
    {
        var command = Substitute.For<ICommand>();
        command.Name.Returns(name);
        command.Aliases.Returns(aliases);
        command.Description.Returns(description);
        return command;
    }
}