using AzStore.Configuration;
using AzStore.Core.Models;
using AzStore.Core.Models.Navigation;
using AzStore.Core.Models.Storage;
using AzStore.Terminal.Input;
using AzStore.Terminal.Navigation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Terminal.Gui;
using AzStore.Terminal.Theming;

namespace AzStore.Terminal.UI;

public class TerminalGuiUI : ITerminalUI
{
    private readonly ILogger<TerminalGuiUI> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly BlobBrowserView _browserView;
    private bool _isRunning;
    private readonly IThemeService _theme;
    private TaskCompletionSource<NavigationResult>? _currentNavigationTask;

    public TerminalGuiUI(ILogger<TerminalGuiUI> logger, ILoggerFactory loggerFactory, IOptions<AzStoreSettings> settings, IInputHandler inputHandler, IThemeService theme)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _theme = theme;

        var browserLogger = _loggerFactory.CreateLogger<BlobBrowserView>();
        _browserView = new BlobBrowserView(browserLogger, settings.Value.KeyBindings, inputHandler, theme);
        _browserView.NavigationRequested += OnNavigationRequested;
    }

    private void OnNavigationRequested(object? sender, NavigationResult result)
    {
        _logger.LogDebug("Navigation requested: {Action}", result.Action);
        _currentNavigationTask?.TrySetResult(result);
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Terminal.Gui application");

        try
        {
            Application.Init();
            _isRunning = true;

            var top = new Toplevel();
            var win = new Window()
            {
                Title = "AzStore - Azure Blob Storage Terminal"
            };

            // Apply basic theming
            win.ColorScheme = _theme.GetLabelColorScheme(ThemeToken.Title);

            _browserView.X = 0;
            _browserView.Y = 0;
            _browserView.Width = Dim.Fill();
            _browserView.Height = Dim.Fill();

            win.Add(_browserView);
            top.Add(win);

            Application.Top?.Add(top);
            Application.Run();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running Terminal.Gui application");
            throw;
        }
        finally
        {
            Application.Shutdown();
            _isRunning = false;
            _logger.LogInformation("Terminal.Gui application stopped");
        }

        return Task.CompletedTask;
    }

    public async Task<NavigationResult> ShowStorageItemsAsync(
        IReadOnlyList<StorageItem> items,
        NavigationState navigationState,
        CancellationToken cancellationToken = default)
    {
        _currentNavigationTask = new TaskCompletionSource<NavigationResult>();

        _browserView.UpdateItems(items, navigationState);

        using var registration = cancellationToken.Register(() => _currentNavigationTask.TrySetCanceled());

        try
        {
            return await _currentNavigationTask.Task;
        }
        finally
        {
            _currentNavigationTask = null;
        }
    }

    public void ShowStatus(string message)
    {
        _logger.LogInformation("Status: {Message}", message);
    }

    public void ShowError(string message)
    {
        _logger.LogError("Error: {Message}", message);
    }

    public void ShowInfo(string message)
    {
        _logger.LogInformation("Info: {Message}", message);
    }

    public void Shutdown()
    {
        if (_isRunning)
        {
            _logger.LogInformation("Shutting down Terminal.Gui application");
            Application.RequestStop();
        }
    }
}
