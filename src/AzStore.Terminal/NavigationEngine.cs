using AzStore.Core;
using AzStore.Core.Models;
using Microsoft.Extensions.Logging;

namespace AzStore.Terminal;

/// <summary>
/// Provides navigation functionality for browsing Azure Blob Storage containers and blobs
/// with VIM-like keyboard navigation.
/// </summary>
public class NavigationEngine : INavigationEngine
{
    private readonly IStorageService _storageService;
    private readonly ILogger<NavigationEngine> _logger;
    private readonly VimNavigator _vimNavigator;
    private readonly IPathService _pathService;
    private Session? _currentSession;
    private NavigationState? _currentState;
    private readonly List<NavigationItem> _currentItems = [];
    private PagedResult<Container>? _currentContainerPage;
    private PagedResult<Blob>? _currentBlobPage;
    private readonly Stack<string> _previousPageTokens = new();

    /// <inheritdoc/>
    public NavigationState? CurrentState => _currentState;

    /// <inheritdoc/>
    public IReadOnlyList<NavigationItem> CurrentItems => _currentItems;

    /// <inheritdoc/>
    public NavigationItem? SelectedItem => _currentState != null && _currentState.SelectedIndex >= 0 && _currentState.SelectedIndex < _currentItems.Count
        ? _currentItems[_currentState.SelectedIndex]
        : null;

    /// <inheritdoc/>
    public NavigationMode CurrentMode => _vimNavigator.CurrentMode;

    /// <inheritdoc/>
    public VimNavigator VimNavigator => _vimNavigator;

    /// <inheritdoc/>
    public event EventHandler<NavigationStateChangedEventArgs>? StateChanged;

    /// <inheritdoc/>
    public event EventHandler<NavigationErrorEventArgs>? NavigationError;

    /// <inheritdoc/>
    public event EventHandler<NavigationModeChangedEventArgs>? ModeChanged;

    /// <summary>
    /// Initializes a new instance of the NavigationEngine class.
    /// </summary>
    /// <param name="storageService">The storage service for accessing Azure Blob Storage.</param>
    /// <param name="logger">Logger instance for this service.</param>
    /// <param name="vimNavigator">The VIM navigator for modal state management.</param>
    /// <param name="pathService">The path service for calculating download paths.</param>
    public NavigationEngine(IStorageService storageService, ILogger<NavigationEngine> logger, VimNavigator vimNavigator, IPathService pathService)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _vimNavigator = vimNavigator ?? throw new ArgumentNullException(nameof(vimNavigator));
        _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));

        // Wire up VIM navigator events
        _vimNavigator.ModeChanged += OnVimNavigatorModeChanged;
    }

    /// <inheritdoc/>
    public async Task InitializeAsync(Session session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        _logger.LogInformation("Initializing navigation for session: {SessionName}", session.Name);

        _currentSession = session;
        var previousState = _currentState;
        _currentState = NavigationState.CreateAtRoot(session.Name, session.StorageAccountName);

        await LoadCurrentLevelAsync(cancellationToken);

        StateChanged?.Invoke(this, new NavigationStateChangedEventArgs(previousState, _currentState));
    }

    /// <inheritdoc/>
    public async Task ProcessKeyActionAsync(KeyBindingAction action, CancellationToken cancellationToken = default)
    {
        if (_currentState == null)
        {
            _logger.LogWarning("Cannot process key action: navigation not initialized");
            return;
        }

        // Check if the action is valid for the current mode
        if (!_vimNavigator.IsActionValidForCurrentMode(action))
        {
            _logger.LogDebug("Action {Action} not valid for current mode {Mode}", action, _vimNavigator.CurrentMode);
            return;
        }

        try
        {
            var previousState = _currentState;

            await (action switch
            {
                KeyBindingAction.MoveDown => MoveSelectionDownAsync(),
                KeyBindingAction.MoveUp => MoveSelectionUpAsync(),
                KeyBindingAction.Enter => EnterSelectedItemAsync(cancellationToken),
                KeyBindingAction.Back => NavigateBackAsync(cancellationToken),
                KeyBindingAction.Top => JumpToTopAsync(),
                KeyBindingAction.Bottom => JumpToBottomAsync(),
                KeyBindingAction.Download => DownloadSelectedItemAsync(cancellationToken),
                KeyBindingAction.Refresh => RefreshCurrentViewAsync(cancellationToken),
                KeyBindingAction.Info => ShowItemDetailsAsync(cancellationToken),
                KeyBindingAction.Help => ShowHelpAsync(cancellationToken),
                KeyBindingAction.Command => HandleCommandModeAsync(),
                _ => Task.CompletedTask
            });

            if (!_currentState.Equals(previousState))
            {
                StateChanged?.Invoke(this, new NavigationStateChangedEventArgs(previousState, _currentState));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing key action: {Action}", action);
            NavigationError?.Invoke(this, new NavigationErrorEventArgs($"Navigation error: {ex.Message}", ex));
        }
    }

    /// <inheritdoc/>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_currentState == null)
            return;

        _logger.LogInformation("Refreshing current navigation view");
        await LoadCurrentLevelAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> NextPageAsync(CancellationToken cancellationToken = default)
    {
        if (_currentState == null)
            return false;

        var hasNextPage = (_currentContainerPage?.HasMore ?? false) || (_currentBlobPage?.HasMore ?? false);
        if (!hasNextPage)
            return false;

        _logger.LogDebug("Loading next page");

        try
        {
            var currentToken = _currentContainerPage?.ContinuationToken ?? _currentBlobPage?.ContinuationToken;
            if (currentToken != null)
            {
                _previousPageTokens.Push(currentToken);
            }

            await LoadCurrentLevelAsync(cancellationToken, currentToken);
            _currentState = _currentState.WithSelectedIndex(0);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading next page");
            NavigationError?.Invoke(this, new NavigationErrorEventArgs($"Failed to load next page: {ex.Message}", ex));
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> PreviousPageAsync(CancellationToken cancellationToken = default)
    {
        if (_previousPageTokens.Count == 0)
            return false;

        _logger.LogDebug("Loading previous page");

        try
        {
            var previousToken = _previousPageTokens.Pop();
            await LoadCurrentLevelAsync(cancellationToken, previousToken);
            _currentState = _currentState?.WithSelectedIndex(0);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading previous page");
            NavigationError?.Invoke(this, new NavigationErrorEventArgs($"Failed to load previous page: {ex.Message}", ex));
            return false;
        }
    }

    /// <inheritdoc/>
    public string GetPageInfo()
    {
        var totalItems = _currentItems.Count;
        var hasMore = (_currentContainerPage?.HasMore ?? false) || (_currentBlobPage?.HasMore ?? false);
        var hasPrevious = _previousPageTokens.Count > 0;

        if (totalItems == 0)
            return "No items";

        var pageInfo = $"Items: {totalItems}";
        if (hasMore || hasPrevious)
        {
            var indicators = new List<string>();
            if (hasPrevious) indicators.Add("←");
            if (hasMore) indicators.Add("→");
            pageInfo += $" [{string.Join(" ", indicators)}]";
        }

        return pageInfo;
    }

    /// <inheritdoc/>
    public string GetBreadcrumbPath()
    {
        return _currentState?.BreadcrumbPath ?? string.Empty;
    }

    /// <inheritdoc/>
    public void RenderCurrentView()
    {
        if (_currentState == null || _currentItems.Count == 0)
        {
            Console.WriteLine("No items to display");
            return;
        }

        Console.WriteLine($"\n{GetBreadcrumbPath()} ({GetPageInfo()})");
        Console.WriteLine(new string('-', 60));

        for (int i = 0; i < _currentItems.Count; i++)
        {
            var item = _currentItems[i];
            var isSelected = i == _currentState.SelectedIndex;
            var prefix = isSelected ? "► " : "  ";

            Console.ForegroundColor = isSelected ? ConsoleColor.Yellow : ConsoleColor.White;

            switch (item.Type)
            {
                case NavigationItemType.Container:
                    Console.WriteLine($"{prefix}📁 {item.Name}");
                    break;
                case NavigationItemType.BlobFile:
                    Console.WriteLine($"{prefix}📄 {item.Name} ({TerminalUtils.FormatSize(item.Size ?? 0)})");
                    break;
                case NavigationItemType.BlobPrefix:
                    Console.WriteLine($"{prefix}📁 {item.Name}/");
                    break;
            }

            Console.ResetColor();
        }

        Console.WriteLine();
    }

    private async Task LoadCurrentLevelAsync(CancellationToken cancellationToken, string? continuationToken = null)
    {
        if (_currentState == null)
            return;

        _currentItems.Clear();

        var pageRequest = new PageRequest(50, continuationToken);

        switch (_currentState.GetLevel())
        {
            case NavigationLevel.StorageAccount:
                _currentContainerPage = await _storageService.ListContainersAsync(pageRequest, cancellationToken);
                _currentBlobPage = null;

                foreach (var container in _currentContainerPage.Items)
                {
                    var isPublic = container.AccessLevel != ContainerAccessLevel.None;
                    _currentItems.Add(new NavigationItem(
                        container.Name,
                        NavigationItemType.Container,
                        null,
                        container.LastModified,
                        isPublic,
                        container.Path));
                }
                break;

            case NavigationLevel.Container:
            case NavigationLevel.BlobPrefix:
                _currentBlobPage = await _storageService.ListBlobsAsync(
                    _currentState.ContainerName!,
                    _currentState.BlobPrefix,
                    pageRequest,
                    cancellationToken);
                _currentContainerPage = null;

                foreach (var blob in _currentBlobPage.Items)
                {
                    var isVirtualDirectory = blob.Name.EndsWith('/');
                    var itemType = isVirtualDirectory ? NavigationItemType.BlobPrefix : NavigationItemType.BlobFile;
                    var displayName = isVirtualDirectory ? blob.Name.TrimEnd('/') : blob.Name;

                    _currentItems.Add(new NavigationItem(
                        displayName,
                        itemType,
                        blob.Size,
                        blob.LastModified,
                        false,
                        blob.Path));
                }
                break;
        }

        _logger.LogDebug("Loaded {ItemCount} items for navigation level: {Level}",
            _currentItems.Count, _currentState.GetLevel());
    }

    private Task MoveSelectionDownAsync()
    {
        if (_currentState == null || _currentItems.Count == 0)
            return Task.CompletedTask;

        var newIndex = Math.Min(_currentState.SelectedIndex + 1, _currentItems.Count - 1);
        _currentState = _currentState.WithSelectedIndex(newIndex);
        return Task.CompletedTask;
    }

    private Task MoveSelectionUpAsync()
    {
        if (_currentState == null || _currentItems.Count == 0)
            return Task.CompletedTask;

        var newIndex = Math.Max(_currentState.SelectedIndex - 1, 0);
        _currentState = _currentState.WithSelectedIndex(newIndex);
        return Task.CompletedTask;
    }

    private async Task EnterSelectedItemAsync(CancellationToken cancellationToken)
    {
        var selectedItem = SelectedItem;
        if (selectedItem == null || _currentState == null)
            return;

        switch (selectedItem.Type)
        {
            case NavigationItemType.Container:
                _currentState = _currentState.NavigateInto(containerName: selectedItem.Name);
                _previousPageTokens.Clear();
                await LoadCurrentLevelAsync(cancellationToken);
                break;

            case NavigationItemType.BlobPrefix:
                _currentState = _currentState.NavigateInto(blobPrefix: selectedItem.Name);
                _previousPageTokens.Clear();
                await LoadCurrentLevelAsync(cancellationToken);
                break;

            case NavigationItemType.BlobFile:
                _logger.LogInformation("Selected blob file: {BlobName}", selectedItem.Name);
                break;
        }
    }

    private async Task NavigateBackAsync(CancellationToken cancellationToken)
    {
        if (_currentState == null || !_currentState.CanNavigateUp())
            return;

        _currentState = _currentState.NavigateUp();
        _previousPageTokens.Clear();
        await LoadCurrentLevelAsync(cancellationToken);
    }

    private Task JumpToTopAsync()
    {
        if (_currentState == null || _currentItems.Count == 0)
            return Task.CompletedTask;

        _currentState = _currentState.WithSelectedIndex(0);
        return Task.CompletedTask;
    }

    private Task JumpToBottomAsync()
    {
        if (_currentState == null || _currentItems.Count == 0)
            return Task.CompletedTask;

        _currentState = _currentState.WithSelectedIndex(_currentItems.Count - 1);
        return Task.CompletedTask;
    }

    private async Task DownloadSelectedItemAsync(CancellationToken cancellationToken)
    {
        var selectedItem = SelectedItem;
        if (selectedItem?.Type != NavigationItemType.BlobFile || _currentState == null || _currentSession == null)
        {
            NavigationError?.Invoke(this, new NavigationErrorEventArgs("No blob file selected for download"));
            return;
        }

        try
        {
            _logger.LogInformation("Download requested for blob: {BlobName}", selectedItem.Name);

            // Get blob details for confirmation
            var blob = await _storageService.GetBlobAsync(
                _currentState.ContainerName!,
                selectedItem.Path ?? selectedItem.Name,
                cancellationToken);

            if (blob == null)
            {
                NavigationError?.Invoke(this, new NavigationErrorEventArgs($"Blob '{selectedItem.Name}' not found"));
                return;
            }

            // Show confirmation prompt
            var confirmation = TerminalConfirmation.ShowDownloadConfirmation(selectedItem.Name, blob.Size);
            if (confirmation != ConfirmationResult.Yes)
            {
                _logger.LogDebug("Download cancelled by user");
                return;
            }

            // Calculate target path
            var targetPath = _pathService.CalculateBlobDownloadPath(
                _currentSession,
                _currentState.ContainerName!,
                selectedItem.Path ?? selectedItem.Name);

            // Check for conflicts
            if (File.Exists(targetPath))
            {
                var conflictResolution = TerminalConfirmation.ShowConflictResolutionPrompt(Path.GetFileName(targetPath));
                switch (conflictResolution)
                {
                    case 'S': // Skip
                        _logger.LogDebug("Download skipped due to file conflict");
                        return;
                    case 'R': // Rename
                        targetPath = GenerateUniqueFileName(targetPath);
                        break;
                    case 'O': // Overwrite - use original path
                        break;
                    default: // Cancelled or unknown
                        return;
                }
            }

            // Ensure target directory exists
            var targetDirectory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            // Create progress callback
            var progress = new Progress<BlobDownloadProgress>(progressInfo =>
            {
                var progressText = TerminalProgressRenderer.RenderBlobDownloadProgress(progressInfo);
                Console.Write($"\r{progressText}");
                Console.Out.Flush();
            });

            // Perform download
            var downloadOptions = DownloadOptions.Default;
            var result = await _storageService.DownloadBlobWithProgressAsync(
                _currentState.ContainerName!,
                selectedItem.Path ?? selectedItem.Name,
                targetPath,
                downloadOptions,
                progress,
                cancellationToken);

            Console.WriteLine(); // Move to next line after progress

            if (result.Success)
            {
                var sizeText = result.BytesDownloaded > 0 ? $" ({TerminalUtils.FormatBytes(result.BytesDownloaded)})" : "";
                NavigationError?.Invoke(this, new NavigationErrorEventArgs(
                    $"✓ Downloaded '{selectedItem.Name}'{sizeText} to {result.LocalFilePath}"));

                _logger.LogInformation("Successfully downloaded {BlobName} to {LocalPath}",
                    selectedItem.Name, result.LocalFilePath);
            }
            else
            {
                NavigationError?.Invoke(this, new NavigationErrorEventArgs(
                    $"✗ Failed to download '{selectedItem.Name}': {result.Error}"));

                _logger.LogError("Failed to download {BlobName}: {Error}", selectedItem.Name, result.Error);
            }
        }
        catch (OperationCanceledException)
        {
            NavigationError?.Invoke(this, new NavigationErrorEventArgs("Download cancelled"));
            _logger.LogInformation("Download cancelled for blob: {BlobName}", selectedItem.Name);
        }
        catch (Exception ex)
        {
            NavigationError?.Invoke(this, new NavigationErrorEventArgs($"Download error: {ex.Message}"));
            _logger.LogError(ex, "Error downloading blob: {BlobName}", selectedItem.Name);
        }
    }

    private Task HandleCommandModeAsync()
    {
        _vimNavigator.EnterCommandMode();
        return Task.CompletedTask;
    }

    private async Task RefreshCurrentViewAsync(CancellationToken cancellationToken)
    {
        if (_currentState == null)
            return;

        try
        {
            _logger.LogInformation("Refreshing current navigation view");
            var selectedIndex = _currentState.SelectedIndex;

            // Clear page tokens to refresh from beginning
            _previousPageTokens.Clear();

            await LoadCurrentLevelAsync(cancellationToken);

            // Restore selection if possible
            if (selectedIndex < _currentItems.Count)
            {
                _currentState = _currentState.WithSelectedIndex(selectedIndex);
            }

            NavigationError?.Invoke(this, new NavigationErrorEventArgs("✓ View refreshed"));
        }
        catch (Exception ex)
        {
            NavigationError?.Invoke(this, new NavigationErrorEventArgs($"Refresh failed: {ex.Message}"));
            _logger.LogError(ex, "Error refreshing current view");
        }
    }

    private async Task ShowItemDetailsAsync(CancellationToken cancellationToken)
    {
        var selectedItem = SelectedItem;
        if (selectedItem == null || _currentState == null)
        {
            NavigationError?.Invoke(this, new NavigationErrorEventArgs("No item selected"));
            return;
        }

        try
        {
            StorageItem? detailItem = selectedItem.Type switch
            {
                NavigationItemType.BlobFile => await _storageService.GetBlobAsync(
                    _currentState.ContainerName!,
                    selectedItem.Path ?? selectedItem.Name,
                    cancellationToken),
                NavigationItemType.Container => await _storageService.GetContainerPropertiesAsync(
                    selectedItem.Name,
                    cancellationToken),
                _ => null
            };

            var itemToShow = detailItem ?? CreateStorageItemFromNavigationItem(selectedItem, _currentState.ContainerName ?? "unknown");
            var detailsText = TerminalProgressRenderer.RenderItemDetails(itemToShow);

            Console.Clear();
            Console.WriteLine(detailsText);
            TerminalConfirmation.WaitForAnyKey();

            // Note: In a real terminal app, we'd need to redraw the current view
            // For now, just trigger a refresh
            await RefreshCurrentViewAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            NavigationError?.Invoke(this, new NavigationErrorEventArgs($"Could not show item details: {ex.Message}"));
            _logger.LogError(ex, "Error showing item details for: {ItemName}", selectedItem.Name);
        }
    }

    private Task ShowHelpAsync(CancellationToken cancellationToken)
    {
        try
        {
            // For now, we'll use a simple help display
            // In the future, this could be integrated with HelpTextGenerator
            var helpText = @"AzStore - Azure Blob Storage Terminal
====================================

NAVIGATION:
  j/↓      Move down
  k/↑      Move up  
  l/Enter  Navigate into item
  h        Navigate back
  gg       Jump to top
  G        Jump to bottom

ACTIONS:
  d       Download selected blob
  i        Show item details
  r        Refresh current view
  ?        Show this help

MODES:
  :        Command mode
  /        Search mode
  Escape   Cancel/exit mode

Press any key to return...";

            Console.Clear();
            Console.WriteLine(helpText);
            TerminalConfirmation.WaitForAnyKey();

            NavigationError?.Invoke(this, new NavigationErrorEventArgs("Help closed - returning to navigation"));
        }
        catch (Exception ex)
        {
            NavigationError?.Invoke(this, new NavigationErrorEventArgs($"Error displaying help: {ex.Message}"));
            _logger.LogError(ex, "Error showing help");
        }

        return Task.CompletedTask;
    }

    private static string GenerateUniqueFileName(string originalPath)
    {
        var directory = Path.GetDirectoryName(originalPath) ?? "";
        var fileName = Path.GetFileNameWithoutExtension(originalPath);
        var extension = Path.GetExtension(originalPath);

        var counter = 1;
        string newPath;

        do
        {
            var newFileName = $"{fileName} ({counter}){extension}";
            newPath = Path.Combine(directory, newFileName);
            counter++;
        }
        while (File.Exists(newPath));

        return newPath;
    }

    private static StorageItem CreateStorageItemFromNavigationItem(NavigationItem navigationItem, string containerName)
    {
        return navigationItem.Type switch
        {
            NavigationItemType.Container => Container.Create(
                navigationItem.Name,
                navigationItem.Path),
            NavigationItemType.BlobFile => Blob.Create(
                navigationItem.Name,
                navigationItem.Path,
                containerName,
                BlobType.BlockBlob,
                navigationItem.Size),
            _ => Blob.Create(
                navigationItem.Name,
                navigationItem.Path,
                containerName,
                BlobType.BlockBlob,
                navigationItem.Size)
        };
    }


    private void OnVimNavigatorModeChanged(object? sender, NavigationModeChangedEventArgs e)
    {
        _logger.LogDebug("Navigation mode changed: {PreviousMode} -> {CurrentMode}", e.PreviousMode, e.CurrentMode);
        ModeChanged?.Invoke(this, e);
    }
}