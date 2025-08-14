using AzStore.Terminal.Commands;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace AzStore.Terminal.Tests.Commands;

public class CommandRegistryTests
{
    [Fact]
    public void FindCommand_WithValidCommandName_ReturnsCommand()
    {
        var registry = CommandRegistryFixture.CreateWithCommands();
        
        var result = registry.FindCommand("exit");
        
        Assert.NotNull(result);
        Assert.Equal("exit", result.Name);
    }

    [Fact]
    public void FindCommand_WithColonPrefix_ReturnsCommand()
    {
        var registry = CommandRegistryFixture.CreateWithCommands();
        
        var result = registry.FindCommand(":exit");
        
        Assert.NotNull(result);
        Assert.Equal("exit", result.Name);
    }

    [Fact]
    public void FindCommand_WithAlias_ReturnsCommand()
    {
        var registry = CommandRegistryFixture.CreateWithCommands();
        
        var result = registry.FindCommand("q");
        
        Assert.NotNull(result);
        Assert.Equal("exit", result.Name);
    }

    [Fact]
    public void FindCommand_CaseInsensitive_ReturnsCommand()
    {
        var registry = CommandRegistryFixture.CreateWithCommands();
        
        var result = registry.FindCommand("EXIT");
        
        Assert.NotNull(result);
        Assert.Equal("exit", result.Name);
    }

    [Fact]
    public void FindCommand_WithInvalidCommand_ReturnsNull()
    {
        var registry = CommandRegistryFixture.CreateWithCommands();
        
        var result = registry.FindCommand("invalid");
        
        Assert.Null(result);
    }

    [Fact]
    public void GetAllCommands_ReturnsAllRegisteredCommands()
    {
        var registry = CommandRegistryFixture.CreateWithCommands();
        
        var commands = registry.GetAllCommands().ToList();
        
        Assert.Equal(3, commands.Count);
        Assert.Contains(commands, c => c.Name == "exit");
        Assert.Contains(commands, c => c.Name == "help");
        Assert.Contains(commands, c => c.Name == "list");
    }

    [Fact]
    public void GetAllCommands_ReturnsDistinctCommands()
    {
        var registry = CommandRegistryFixture.CreateWithCommands();
        
        var commands = registry.GetAllCommands().ToList();
        var distinctCommands = commands.DistinctBy(c => c.GetType()).ToList();
        
        Assert.Equal(commands.Count, distinctCommands.Count);
    }
}