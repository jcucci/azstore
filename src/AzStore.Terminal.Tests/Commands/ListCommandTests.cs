using AzStore.Terminal.Commands;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AzStore.Terminal.Tests.Commands;

public class ListCommandTests
{
    [Fact]
    public void Name_ReturnsList()
    {
        var logger = Substitute.For<ILogger<ListCommand>>();
        var command = new ListCommand(logger);
        
        Assert.Equal("list", command.Name);
    }

    [Fact]
    public void Aliases_ContainsLs()
    {
        var logger = Substitute.For<ILogger<ListCommand>>();
        var command = new ListCommand(logger);
        
        Assert.Contains("ls", command.Aliases);
    }

    [Fact]
    public void Description_IsNotEmpty()
    {
        var logger = Substitute.For<ILogger<ListCommand>>();
        var command = new ListCommand(logger);
        
        Assert.False(string.IsNullOrWhiteSpace(command.Description));
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsSuccessResult()
    {
        var logger = Substitute.For<ILogger<ListCommand>>();
        var command = new ListCommand(logger);
        
        var result = await command.ExecuteAsync(Array.Empty<string>());
        
        Assert.True(result.Success);
        Assert.False(result.ShouldExit);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_LogsDebugMessage()
    {
        var logger = Substitute.For<ILogger<ListCommand>>();
        var command = new ListCommand(logger);
        
        await command.ExecuteAsync(Array.Empty<string>());
        
        logger.Received(1).LogDebug("User requested file list");
    }
}