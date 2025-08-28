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
    public event EventHandler<NavigationStateChangedEventArgs>? StateChanged;

    /// <inheritdoc/>
    public event EventHandler<NavigationErrorEventArgs>? NavigationError;

    /// <summary>
    /// Initializes a new instance of the NavigationEngine class.
    /// </summary>
    /// <param name="storageService">The storage service for accessing Azure Blob Storage.</param>
    /// <param name="logger">Logger instance for this service.</param>
    public NavigationEngine(IStorageService storageService, ILogger<NavigationEngine> logger)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                    Console.WriteLine($"{prefix}📄 {item.Name} ({FormatSize(item.Size ?? 0)})");
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

    private Task DownloadSelectedItemAsync(CancellationToken cancellationToken)
    {
        var selectedItem = SelectedItem;
        if (selectedItem?.Type != NavigationItemType.BlobFile || _currentState == null || _currentSession == null)
            return Task.CompletedTask;

        _logger.LogInformation("Download requested for blob: {BlobName}", selectedItem.Name);

        NavigationError?.Invoke(this, new NavigationErrorEventArgs(
            $"Download functionality will be implemented in future phases. Selected: {selectedItem.Name}"));

        return Task.CompletedTask;
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}