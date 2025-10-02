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
using AzStore.Terminal.UI.Layout;

namespace AzStore.Terminal.UI;

public class TerminalGuiUI : ITerminalUI
{
    private readonly ILogger<TerminalGuiUI> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly BlobBrowserView _browserView;
    private readonly LayoutRootView _layoutRoot;
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

        var chromeLogger = _loggerFactory.CreateLogger<UI.Panes.PaneChromeView>();
        var layoutLogger = _loggerFactory.CreateLogger<UI.Layout.LayoutRootView>();
        var paneLogger = _loggerFactory.CreateLogger<UI.Panes.PaneViewBase>();
        _layoutRoot = new LayoutRootView(_browserView, theme, chromeLogger, layoutLogger);
    }

    private void OnNavigationRequested(object? sender, NavigationResult result)
    {
        _logger.LogDebug("Navigation requested: {Action}", result.Action);

        if (result.Action == NavigationAction.Cancel)
        {
            try
            {
                Application.RequestStop();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop Terminal.Gui application during cancel navigation");
            }

            return;
        }

        _currentNavigationTask?.TrySetResult(result);
    }

    private void OnApplicationKeyDown(object? sender, Key e)
    {
        _logger.LogDebug("Application.KeyDown: Key={Key}", e);

        if (e == Key.Tab || e == Key.Tab.WithShift)
        {
            _logger.LogDebug("Tab key detected at Application level, handling focus traversal");

            if (_layoutRoot.HandleFocusTraversal(e))
            {
                _logger.LogDebug("Focus traversal handled, marking key as handled");
                e.Handled = true;  // Mark the Key object itself as handled
            }
            else
            {
                _logger.LogDebug("Focus traversal not handled");
            }
        }
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Terminal.Gui application");

        try
        {
            Application.Init();
            _isRunning = true;

            // Subscribe to Application.KeyDown before views process keys
            _logger.LogDebug("Subscribing to Application.KeyDown event");
            Application.KeyDown += OnApplicationKeyDown;
            _logger.LogDebug("Successfully subscribed to Application.KeyDown");

            var baseScheme = _theme.GetLabelColorScheme(ThemeToken.Background);

            // Apply global color schemes to ensure a black background across the app
            // Note: Terminal.Gui v2 may not expose global Colors; we rely on per-view schemes

            if (Application.Top != null)
            {
                Application.Top.ColorScheme = baseScheme;
            }

            var top = new Toplevel { ColorScheme = baseScheme };

            _layoutRoot.X = 0;
            _layoutRoot.Y = 0;
            _layoutRoot.Width = Dim.Fill();
            _layoutRoot.Height = Dim.Fill();

            top.Add(_layoutRoot);

            _layoutRoot.ScheduleInitialFocus();


            Application.Run(top);
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
