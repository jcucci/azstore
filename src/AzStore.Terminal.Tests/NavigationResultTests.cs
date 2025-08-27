using AzStore.Core.Models;
using AzStore.Terminal;
using Xunit;

namespace AzStore.Terminal.Tests;

[Trait("Category", "Unit")]
public class NavigationResultTests
{
    [Fact]
    public void NavigationResult_WithActionOnly_CreatesCorrectly()
    {
        var result = new NavigationResult(NavigationAction.Exit);
        
        Assert.Equal(NavigationAction.Exit, result.Action);
        Assert.Null(result.SelectedIndex);
        Assert.Null(result.SelectedItem);
        Assert.Null(result.Command);
    }

    [Fact]
    public void NavigationResult_WithActionAndIndex_CreatesCorrectly()
    {
        var result = new NavigationResult(NavigationAction.Enter, 5);
        
        Assert.Equal(NavigationAction.Enter, result.Action);
        Assert.Equal(5, result.SelectedIndex);
        Assert.Null(result.SelectedItem);
        Assert.Null(result.Command);
    }

    [Fact]
    public void NavigationResult_WithActionIndexAndItem_CreatesCorrectly()
    {
        var container = Container.Create("test-container", "/test-container");
        var result = new NavigationResult(NavigationAction.Enter, 2, container);
        
        Assert.Equal(NavigationAction.Enter, result.Action);
        Assert.Equal(2, result.SelectedIndex);
        Assert.Equal(container, result.SelectedItem);
        Assert.Null(result.Command);
    }

    [Fact]
    public void NavigationResult_WithCommand_CreatesCorrectly()
    {
        var result = new NavigationResult(NavigationAction.Command, Command: ":help");
        
        Assert.Equal(NavigationAction.Command, result.Action);
        Assert.Null(result.SelectedIndex);
        Assert.Null(result.SelectedItem);
        Assert.Equal(":help", result.Command);
    }

    [Fact]
    public void NavigationResult_WithAllParameters_CreatesCorrectly()
    {
        var blob = Blob.Create("test.txt", "/test.txt", "container", BlobType.BlockBlob, 1024);
        var result = new NavigationResult(NavigationAction.Enter, 3, blob, "/search");
        
        Assert.Equal(NavigationAction.Enter, result.Action);
        Assert.Equal(3, result.SelectedIndex);
        Assert.Equal(blob, result.SelectedItem);
        Assert.Equal("/search", result.Command);
    }

    [Fact]
    public void NavigationResult_RecordEquality_WorksCorrectly()
    {
        var container = Container.Create("test-container", "/test-container");
        var result1 = new NavigationResult(NavigationAction.Enter, 1, container, "test");
        var result2 = new NavigationResult(NavigationAction.Enter, 1, container, "test");
        var result3 = new NavigationResult(NavigationAction.Back, 1, container, "test");
        
        Assert.Equal(result1, result2);
        Assert.NotEqual(result1, result3);
    }

    [Fact]
    public void NavigationResult_GetHashCode_WorksCorrectly()
    {
        var container = Container.Create("test-container", "/test-container");
        var result1 = new NavigationResult(NavigationAction.Enter, 1, container);
        var result2 = new NavigationResult(NavigationAction.Enter, 1, container);
        
        Assert.Equal(result1.GetHashCode(), result2.GetHashCode());
    }

    [Fact]
    public void NavigationResult_ToString_ContainsAction()
    {
        var result = new NavigationResult(NavigationAction.Refresh);
        var stringResult = result.ToString();
        
        Assert.Contains("Refresh", stringResult);
        Assert.Contains("NavigationResult", stringResult);
    }

    [Theory]
    [InlineData(NavigationAction.None)]
    [InlineData(NavigationAction.Enter)]
    [InlineData(NavigationAction.Back)]
    [InlineData(NavigationAction.Exit)]
    [InlineData(NavigationAction.Refresh)]
    [InlineData(NavigationAction.Help)]
    [InlineData(NavigationAction.Command)]
    public void NavigationResult_SupportsAllNavigationActions(NavigationAction action)
    {
        var result = new NavigationResult(action);
        
        Assert.Equal(action, result.Action);
    }

    [Fact]
    public void NavigationResult_WithBlob_StoresCorrectly()
    {
        var blob = Blob.Create("document.pdf", "/document.pdf", "container", BlobType.PageBlob, 2048);
        var result = new NavigationResult(NavigationAction.Enter, 0, blob);
        
        Assert.Equal(blob, result.SelectedItem);
        Assert.IsType<Blob>(result.SelectedItem);
        Assert.Equal("document.pdf", result.SelectedItem.Name);
    }

    [Fact]
    public void NavigationResult_WithContainer_StoresCorrectly()
    {
        var container = Container.Create("my-container", "/my-container");
        var result = new NavigationResult(NavigationAction.Enter, 1, container);
        
        Assert.Equal(container, result.SelectedItem);
        Assert.IsType<Container>(result.SelectedItem);
        Assert.Equal("my-container", result.SelectedItem.Name);
    }
}