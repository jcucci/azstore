using AzStore.Core.Models;
using Microsoft.Extensions.Logging;
using Terminal.Gui;
using System.Collections.ObjectModel;
using KeyBindingsConfig = AzStore.Configuration.KeyBindings;

namespace AzStore.Terminal;

public class BlobBrowserView : View
{
    private static readonly string[] FileSizeSuffixes = ["B", "KB", "MB", "GB", "TB"];

    private readonly ILogger<BlobBrowserView> _logger;
    private readonly KeyBindingsConfig _keyBindings;
    private readonly IInputHandler _inputHandler;
    private readonly ObservableCollection<string> _displayItems = [];

    private ListView? _listView;
    private Label? _breadcrumbLabel;
    private Label? _statusLabel;
    private NavigationState? _currentState;
    private IReadOnlyList<StorageItem> _currentItems = [];

    public event EventHandler<NavigationResult>? NavigationRequested;

    public BlobBrowserView(ILogger<BlobBrowserView> logger, KeyBindingsConfig keyBindings, IInputHandler inputHandler)
    {
        _logger = logger;
        _keyBindings = keyBindings;
        _inputHandler = inputHandler;

        // Subscribe to input handler events
        _inputHandler.NavigationRequested += OnNavigationRequested;

        InitializeComponents();
        SetupKeyBindings();
    }

    private void InitializeComponents()
    {
        Width = Dim.Fill();
        Height = Dim.Fill();

        _breadcrumbLabel = new Label()
        {
            X = 1,
            Y = 0,
            Width = Dim.Fill(1),
            Height = 1,
            Text = "Azure Storage"
        };
        Add(_breadcrumbLabel);

        _listView = new ListView()
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(1),
            Height = Dim.Fill(3),
            AllowsMarking = false,
            AllowsMultipleSelection = false
        };
        _listView.SetSource(_displayItems);
        Add(_listView);

        _statusLabel = new Label()
        {
            X = 1,
            Y = Pos.Bottom(this) - 1,
            Width = Dim.Fill(1),
            Height = 1,
            Text = $"{_keyBindings.MoveDown}/{_keyBindings.MoveUp}: navigate, {_keyBindings.Enter}/Enter: select, {_keyBindings.Back}: back, {_keyBindings.Search}: search, {_keyBindings.Command}: command"
        };
        Add(_statusLabel);
    }

    private void SetupKeyBindings()
    {
        if (_listView == null) return;

        _listView.KeyDown += (s, keyEvent) =>
        {
            // Delegate all key processing to the input handler
            _inputHandler.ProcessKeyEvent(keyEvent);
        };
    }

    private void OnNavigationRequested(object? sender, NavigationResult result)
    {
        // Handle navigation actions that affect the view directly
        switch (result.Action)
        {
            case NavigationAction.Enter when result.KeyBindingAction == KeyBindingAction.MoveDown:
                _listView?.MoveDown();
                return;
            case NavigationAction.Enter when result.KeyBindingAction == KeyBindingAction.MoveUp:
                _listView?.MoveUp();
                return;
            case NavigationAction.Enter when result.KeyBindingAction == KeyBindingAction.Enter:
                HandleItemSelection();
                return;
            case NavigationAction.Enter when result.KeyBindingAction == null:
                HandleItemSelection();
                return;
            case NavigationAction.Back:
                HandleBackNavigation();
                return;
            case NavigationAction.JumpToTop:
                HandleJumpToTop();
                return;
            case NavigationAction.JumpToBottom:
                HandleJumpToBottom();
                return;
            case NavigationAction.Download:
                HandleDownloadRequest();
                return;
            case NavigationAction.Command when result.Command == "/":
                HandleSearchRequest();
                return;
            case NavigationAction.Command when result.Command == ":":
                HandleCommandRequest();
                return;
            case NavigationAction.Cancel:
                // Clear any visual state if needed
                return;
        }

        // Forward all navigation requests to parent
        NavigationRequested?.Invoke(this, result);
    }

    public void UpdateItems(IReadOnlyList<StorageItem> items, NavigationState navigationState)
    {
        _currentItems = items;
        _currentState = navigationState;

        UpdateBreadcrumb();
        UpdateListDisplay();
        UpdateStatus();

        _logger.LogDebug("Updated browser view with {ItemCount} items at {Level}",
            items.Count, navigationState.GetLevel());
    }

    private void UpdateBreadcrumb()
    {
        if (_breadcrumbLabel == null || _currentState == null) return;

        _breadcrumbLabel.Text = _currentState.BreadcrumbPath;
    }

    private void UpdateListDisplay()
    {
        _displayItems.Clear();

        foreach (var item in _currentItems)
        {
            var displayText = FormatStorageItem(item);
            _displayItems.Add(displayText);
        }

        if (_currentState != null && _currentState.SelectedIndex < _displayItems.Count)
        {
            _listView!.SelectedItem = _currentState.SelectedIndex;
        }
    }

    private void UpdateStatus()
    {
        if (_statusLabel == null || _currentState == null) return;

        var level = _currentState.GetLevel() switch
        {
            NavigationLevel.StorageAccount => "Storage Account",
            NavigationLevel.Container => "Container",
            NavigationLevel.BlobPrefix => "Folder",
            _ => "Unknown"
        };

        var itemCount = _currentItems.Count;
        var selectedIndex = _listView?.SelectedItem ?? 0;

        _statusLabel.Text = $"{level} | {itemCount} items | Selected: {selectedIndex + 1}/{itemCount} | j/k:nav l:enter h:back gg:top G:bottom dd:download";
    }

    private void HandleItemSelection()
    {
        var selectedIndex = _listView?.SelectedItem ?? -1;
        if (selectedIndex >= 0 && selectedIndex < _currentItems.Count)
        {
            var selectedItem = _currentItems[selectedIndex];
            var result = new NavigationResult(NavigationAction.Enter, selectedIndex, selectedItem);
            NavigationRequested?.Invoke(this, result);

            _logger.LogDebug("Item selected: {ItemName} at index {Index}", selectedItem.Name, selectedIndex);
        }
    }

    private void HandleBackNavigation()
    {
        if (_currentState?.CanNavigateUp() == true)
        {
            var result = new NavigationResult(NavigationAction.Back);
            NavigationRequested?.Invoke(this, result);

            _logger.LogDebug("Back navigation requested from {CurrentPath}", _currentState.BreadcrumbPath);
        }
    }

    private void HandleSearchRequest()
    {
        var result = new NavigationResult(NavigationAction.Command, Command: "/");
        NavigationRequested?.Invoke(this, result);

        _logger.LogDebug("Search requested");
    }

    private void HandleCommandRequest()
    {
        var result = new NavigationResult(NavigationAction.Command, Command: ":");
        NavigationRequested?.Invoke(this, result);

        _logger.LogDebug("Command mode requested");
    }

    private void HandleJumpToTop()
    {
        if (_listView != null && _currentItems.Count > 0)
        {
            _listView.SelectedItem = 0;
            var result = new NavigationResult(NavigationAction.JumpToTop);
            NavigationRequested?.Invoke(this, result);

            _logger.LogDebug("Jump to top requested");
        }
    }

    private void HandleJumpToBottom()
    {
        if (_listView != null && _currentItems.Count > 0)
        {
            _listView.SelectedItem = _currentItems.Count - 1;
            var result = new NavigationResult(NavigationAction.JumpToBottom);
            NavigationRequested?.Invoke(this, result);

            _logger.LogDebug("Jump to bottom requested");
        }
    }

    private void HandleDownloadRequest()
    {
        var selectedIndex = _listView?.SelectedItem ?? -1;
        if (selectedIndex >= 0 && selectedIndex < _currentItems.Count)
        {
            var selectedItem = _currentItems[selectedIndex];
            var result = new NavigationResult(NavigationAction.Download, selectedIndex, selectedItem);
            NavigationRequested?.Invoke(this, result);

            _logger.LogDebug("Download requested for: {ItemName} at index {Index}", selectedItem.Name, selectedIndex);
        }
    }


    private static string FormatStorageItem(StorageItem item)
    {
        var icon = item switch
        {
            Container => "📁",
            Blob blob => blob.BlobType switch
            {
                BlobType.BlockBlob => "📄",
                BlobType.PageBlob => "📋",
                BlobType.AppendBlob => "📝",
                _ => "📄"
            },
            _ => "❓"
        };

        var size = item switch
        {
            Blob blob => FormatFileSize(blob.Size),
            _ => ""
        };

        var sizeColumn = size.PadLeft(12);
        var nameColumn = item.Name.PadRight(50);

        return $"{icon} {nameColumn} {sizeColumn}";
    }

    private static string FormatFileSize(long? bytes)
    {
        if (bytes == null || bytes == 0) return "";

        var counter = 0;
        var number = (double)bytes.Value;

        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        return $"{number:n1}{FileSizeSuffixes[counter]}";
    }
}