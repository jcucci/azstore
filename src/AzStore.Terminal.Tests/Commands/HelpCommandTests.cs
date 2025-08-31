using AzStore.Terminal.Commands;
using AzStore.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AzStore.Terminal.Tests.Commands;

public class HelpCommandTests
{
    [Fact]
    public void Name_ReturnsHelp()
    {
        var (logger, commandRegistry, keyBindings) = CreateTestDependencies();
        var command = new HelpCommand(logger, commandRegistry, keyBindings);
        
        Assert.Equal("help", command.Name);
    }

    [Fact]
    public void Aliases_IsEmpty()
    {
        var (logger, commandRegistry, keyBindings) = CreateTestDependencies();
        var command = new HelpCommand(logger, commandRegistry, keyBindings);
        
        Assert.Empty(command.Aliases);
    }

    [Fact]
    public void Description_IsNotEmpty()
    {
        var (logger, commandRegistry, keyBindings) = CreateTestDependencies();
        var command = new HelpCommand(logger, commandRegistry, keyBindings);
        
        Assert.False(string.IsNullOrWhiteSpace(command.Description));
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsSuccessResult()
    {
        var (logger, commandRegistry, keyBindings) = CreateTestDependencies();
        var command = new HelpCommand(logger, commandRegistry, keyBindings);
        
        var result = await command.ExecuteAsync(Array.Empty<string>());
        
        Assert.True(result.Success);
        Assert.False(result.ShouldExit);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_LogsDebugMessage()
    {
        var (logger, commandRegistry, keyBindings) = CreateTestDependencies();
        var command = new HelpCommand(logger, commandRegistry, keyBindings);
        
        await command.ExecuteAsync(Array.Empty<string>());
        
        logger.Received(1).LogDebug("User requested help");
    }

    [Fact]
    public async Task ExecuteAsync_IncludesAllCommands()
    {
        var logger = Substitute.For<ILogger<HelpCommand>>();
        var commandRegistry = Substitute.For<ICommandRegistry>();
        
        var commands = new[]
        {
            CreateMockCommand("test1", Array.Empty<string>(), "Test command 1"),
            CreateMockCommand("test2", ["t2"], "Test command 2")
        };
        
        commandRegistry.GetAllCommands().Returns(commands);
        var keyBindings = new KeyBindings();
        
        var command = new HelpCommand(logger, commandRegistry, keyBindings);
        var result = await command.ExecuteAsync(Array.Empty<string>());
        
        Assert.Contains("test1", result.Message);
        Assert.Contains("test2", result.Message);
        Assert.Contains("t2", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_FormatsCommandsCorrectly()
    {
        var logger = Substitute.For<ILogger<HelpCommand>>();
        var commandRegistry = Substitute.For<ICommandRegistry>();
        
        var commands = new[] { CreateMockCommand("test", ["t"], "Test description") };
        commandRegistry.GetAllCommands().Returns(commands);
        var keyBindings = new KeyBindings();
        
        var command = new HelpCommand(logger, commandRegistry, keyBindings);
        var result = await command.ExecuteAsync(Array.Empty<string>());
        
        Assert.Contains(":test, :t - Test description", result.Message);
    }

    private static (ILogger<HelpCommand>, ICommandRegistry, KeyBindings) CreateTestDependencies()
    {
        var logger = Substitute.For<ILogger<HelpCommand>>();
        var commandRegistry = Substitute.For<ICommandRegistry>();
        var keyBindings = new KeyBindings();
        
        var commands = new[] { CreateMockCommand("test", Array.Empty<string>(), "Test description") };
        commandRegistry.GetAllCommands().Returns(commands);
        
        return (logger, commandRegistry, keyBindings);
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