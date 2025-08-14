using AzStore.Terminal.Commands;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AzStore.Terminal.Tests.Commands;

public class ExitCommandTests
{
    [Fact]
    public void Name_ReturnsExit()
    {
        var logger = Substitute.For<ILogger<ExitCommand>>();
        var command = new ExitCommand(logger);
        
        Assert.Equal("exit", command.Name);
    }

    [Fact]
    public void Aliases_ContainsQ()
    {
        var logger = Substitute.For<ILogger<ExitCommand>>();
        var command = new ExitCommand(logger);
        
        Assert.Contains("q", command.Aliases);
    }

    [Fact]
    public void Description_IsNotEmpty()
    {
        var logger = Substitute.For<ILogger<ExitCommand>>();
        var command = new ExitCommand(logger);
        
        Assert.False(string.IsNullOrWhiteSpace(command.Description));
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsExitResult()
    {
        var logger = Substitute.For<ILogger<ExitCommand>>();
        var command = new ExitCommand(logger);
        
        var result = await command.ExecuteAsync(Array.Empty<string>());
        
        Assert.True(result.Success);
        Assert.True(result.ShouldExit);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_CompletesSuccessfully()
    {
        var logger = Substitute.For<ILogger<ExitCommand>>();
        var command = new ExitCommand(logger);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        var result = await command.ExecuteAsync(Array.Empty<string>(), cts.Token);
        
        Assert.True(result.Success);
        Assert.True(result.ShouldExit);
    }

    [Fact]
    public async Task ExecuteAsync_LogsInformation()
    {
        var logger = Substitute.For<ILogger<ExitCommand>>();
        var command = new ExitCommand(logger);
        
        await command.ExecuteAsync(Array.Empty<string>());
        
        logger.Received(1).LogInformation("User initiated exit via command");
    }
}