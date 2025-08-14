using AzStore.Terminal.Commands;
using Xunit;

namespace AzStore.Terminal.Tests.Commands;

public class CommandResultTests
{
    [Fact]
    public void Ok_WithoutMessage_ReturnsSuccessResult()
    {
        var result = CommandResult.Ok();
        
        Assert.True(result.Success);
        Assert.False(result.ShouldExit);
        Assert.Null(result.Message);
    }

    [Fact]
    public void Ok_WithMessage_ReturnsSuccessResultWithMessage()
    {
        var message = "Operation completed";
        
        var result = CommandResult.Ok(message);
        
        Assert.True(result.Success);
        Assert.False(result.ShouldExit);
        Assert.Equal(message, result.Message);
    }

    [Fact]
    public void Error_ReturnsFailureResult()
    {
        var message = "Something went wrong";
        
        var result = CommandResult.Error(message);
        
        Assert.False(result.Success);
        Assert.False(result.ShouldExit);
        Assert.Equal(message, result.Message);
    }

    [Fact]
    public void Exit_WithoutMessage_ReturnsExitResult()
    {
        var result = CommandResult.Exit();
        
        Assert.True(result.Success);
        Assert.True(result.ShouldExit);
        Assert.Null(result.Message);
    }

    [Fact]
    public void Exit_WithMessage_ReturnsExitResultWithMessage()
    {
        var message = "Goodbye!";
        
        var result = CommandResult.Exit(message);
        
        Assert.True(result.Success);
        Assert.True(result.ShouldExit);
        Assert.Equal(message, result.Message);
    }
}