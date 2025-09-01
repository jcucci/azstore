using AzStore.Core.Models.Navigation;
using AzStore.Core.Models.Session;

namespace AzStore.Terminal;

/// <summary>
/// Provides navigation functionality for browsing Azure Blob Storage containers and blobs
/// with VIM-like keyboard navigation.
/// </summary>
public interface INavigationEngine
{
    /// <summary>
    /// Gets the current navigation state.
    /// </summary>
    NavigationState? CurrentState { get; }

    /// <summary>
    /// Gets the current page of storage items being displayed.
    /// </summary>
    IReadOnlyList<NavigationItem> CurrentItems { get; }

    /// <summary>
    /// Gets the currently selected item, if any.
    /// </summary>
    NavigationItem? SelectedItem { get; }

    /// <summary>
    /// Gets the current navigation mode (Normal, Command, Visual).
    /// </summary>
    NavigationMode CurrentMode { get; }

    /// <summary>
    /// Gets the VIM navigator for modal state management.
    /// </summary>
    VimNavigator VimNavigator { get; }

    /// <summary>
    /// Initializes navigation at the storage account level for the given session.
    /// </summary>
    /// <param name="session">The session to navigate within.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous initialization.</returns>
    Task InitializeAsync(Session session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a key binding action and updates navigation state accordingly.
    /// </summary>
    /// <param name="action">The key binding action to process.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous navigation update.</returns>
    Task ProcessKeyActionAsync(KeyBindingAction action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the current view by reloading data from Azure storage.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous refresh operation.</returns>
    Task RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Navigates to the next page of items if available.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that returns true if navigation was successful, false if no next page exists.</returns>
    Task<bool> NextPageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Navigates to the previous page of items if available.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that returns true if navigation was successful, false if no previous page exists.</returns>
    Task<bool> PreviousPageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current page information for display purposes.
    /// </summary>
    /// <returns>A string describing the current page state (e.g., "Page 2 of 5").</returns>
    string GetPageInfo();

    /// <summary>
    /// Gets the current breadcrumb path for display in the prompt.
    /// </summary>
    /// <returns>A formatted breadcrumb path string.</returns>
    string GetBreadcrumbPath();

    /// <summary>
    /// Renders the current view to the console with highlighting for the selected item.
    /// </summary>
    void RenderCurrentView();

    /// <summary>
    /// Event raised when the navigation state changes.
    /// </summary>
    event EventHandler<NavigationStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Event raised when an error occurs during navigation.
    /// </summary>
    event EventHandler<NavigationErrorEventArgs>? NavigationError;

    /// <summary>
    /// Event raised when the navigation mode changes.
    /// </summary>
    event EventHandler<NavigationModeChangedEventArgs>? ModeChanged;
}

