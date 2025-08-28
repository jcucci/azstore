using AzStore.Configuration;
using AzStore.Core.Models;
using AzStore.Terminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace AzStore.Terminal.Tests;

[Trait("Category", "Unit")]
public class TerminalGuiUITests
{
    [Fact]
    public void TerminalGuiUI_CanBeInstantiated()
    {
        var logger = Substitute.For<ILogger<TerminalGuiUI>>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var browserLogger = Substitute.For<ILogger<BlobBrowserView>>();
        
        loggerFactory.CreateLogger<BlobBrowserView>().Returns(browserLogger);
        
        var settings = Substitute.For<IOptions<AzStoreSettings>>();
        settings.Value.Returns(new AzStoreSettings());
        var inputHandler = Substitute.For<IInputHandler>();
        
        var ui = new TerminalGuiUI(logger, loggerFactory, settings, inputHandler);
        
        Assert.NotNull(ui);
    }

    [Fact]
    public void ShowStatus_LogsInformation()
    {
        var logger = Substitute.For<ILogger<TerminalGuiUI>>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var browserLogger = Substitute.For<ILogger<BlobBrowserView>>();
        
        loggerFactory.CreateLogger<BlobBrowserView>().Returns(browserLogger);
        
        var settings = Substitute.For<IOptions<AzStoreSettings>>();
        settings.Value.Returns(new AzStoreSettings());
        var inputHandler = Substitute.For<IInputHandler>();
        
        var ui = new TerminalGuiUI(logger, loggerFactory, settings, inputHandler);
        
        ui.ShowStatus("Test status message");
        
        logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Test status message")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void ShowError_LogsError()
    {
        var logger = Substitute.For<ILogger<TerminalGuiUI>>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var browserLogger = Substitute.For<ILogger<BlobBrowserView>>();
        
        loggerFactory.CreateLogger<BlobBrowserView>().Returns(browserLogger);
        
        var settings = Substitute.For<IOptions<AzStoreSettings>>();
        settings.Value.Returns(new AzStoreSettings());
        var inputHandler = Substitute.For<IInputHandler>();
        
        var ui = new TerminalGuiUI(logger, loggerFactory, settings, inputHandler);
        
        ui.ShowError("Test error message");
        
        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Test error message")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void ShowInfo_LogsInformation()
    {
        var logger = Substitute.For<ILogger<TerminalGuiUI>>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var browserLogger = Substitute.For<ILogger<BlobBrowserView>>();
        
        loggerFactory.CreateLogger<BlobBrowserView>().Returns(browserLogger);
        
        var settings = Substitute.For<IOptions<AzStoreSettings>>();
        settings.Value.Returns(new AzStoreSettings());
        var inputHandler = Substitute.For<IInputHandler>();
        
        var ui = new TerminalGuiUI(logger, loggerFactory, settings, inputHandler);
        
        ui.ShowInfo("Test info message");
        
        logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Test info message")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void Shutdown_LogsShutdownMessage()
    {
        var logger = Substitute.For<ILogger<TerminalGuiUI>>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var browserLogger = Substitute.For<ILogger<BlobBrowserView>>();
        
        loggerFactory.CreateLogger<BlobBrowserView>().Returns(browserLogger);
        
        var settings = Substitute.For<IOptions<AzStoreSettings>>();
        settings.Value.Returns(new AzStoreSettings());
        var inputHandler = Substitute.For<IInputHandler>();
        
        var ui = new TerminalGuiUI(logger, loggerFactory, settings, inputHandler);
        
        ui.Shutdown();
        
        // Shutdown should not log if not running
        logger.DidNotReceive().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Shutting down")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ShowStorageItemsAsync_UpdatesBrowserView()
    {
        var logger = Substitute.For<ILogger<TerminalGuiUI>>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var browserLogger = Substitute.For<ILogger<BlobBrowserView>>();
        
        loggerFactory.CreateLogger<BlobBrowserView>().Returns(browserLogger);
        
        var settings = Substitute.For<IOptions<AzStoreSettings>>();
        settings.Value.Returns(new AzStoreSettings());
        var inputHandler = Substitute.For<IInputHandler>();
        
        var ui = new TerminalGuiUI(logger, loggerFactory, settings, inputHandler);
        
        var items = new List<StorageItem>
        {
            Container.Create("test-container", "/test-container")
        };
        
        var navigationState = NavigationState.CreateAtRoot("test-session", "storage-account");
        
        // Start a background task that will complete the navigation quickly
        var navigationTask = Task.Run(async () =>
        {
            await Task.Delay(10); // Short delay to allow ShowStorageItemsAsync to set up
            // Simulate navigation completion by accessing the private field via reflection
            var field = typeof(TerminalGuiUI).GetField("_currentNavigationTask", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var taskCompletionSource = field?.GetValue(ui) as TaskCompletionSource<NavigationResult>;
            taskCompletionSource?.TrySetResult(new NavigationResult(NavigationAction.Exit));
        });
        
        var result = await ui.ShowStorageItemsAsync(items, navigationState, CancellationToken.None);
        await navigationTask;
        
        Assert.Equal(NavigationAction.Exit, result.Action);
    }

    [Fact]
    public async Task ShowStorageItemsAsync_HandlesCancellation()
    {
        var logger = Substitute.For<ILogger<TerminalGuiUI>>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var browserLogger = Substitute.For<ILogger<BlobBrowserView>>();
        
        loggerFactory.CreateLogger<BlobBrowserView>().Returns(browserLogger);
        
        var settings = Substitute.For<IOptions<AzStoreSettings>>();
        settings.Value.Returns(new AzStoreSettings());
        var inputHandler = Substitute.For<IInputHandler>();
        
        var ui = new TerminalGuiUI(logger, loggerFactory, settings, inputHandler);
        
        var items = new List<StorageItem>
        {
            Container.Create("test-container", "/test-container")
        };
        
        var navigationState = NavigationState.CreateAtRoot("test-session", "storage-account");
        
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately
        
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await ui.ShowStorageItemsAsync(items, navigationState, cts.Token);
        });
    }
}