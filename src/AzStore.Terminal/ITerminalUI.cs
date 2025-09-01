using AzStore.Core.Models.Navigation;
using AzStore.Core.Models.Storage;

namespace AzStore.Terminal;

/// <summary>
/// Interface for terminal UI operations with VIM-like navigation support.
/// Provides abstraction for different terminal UI implementations.
/// </summary>
public interface ITerminalUI
{
    /// <summary>
    /// Starts the terminal UI application with navigation support.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Task representing the async operation</returns>
    Task RunAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Displays a list of storage items (containers or blobs) with VIM-like navigation.
    /// </summary>
    /// <param name="items">Items to display</param>
    /// <param name="navigationState">Current navigation state</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Navigation result indicating user action</returns>
    Task<NavigationResult> ShowStorageItemsAsync(
        IReadOnlyList<StorageItem> items,
        NavigationState navigationState,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows a status message in the UI.
    /// </summary>
    /// <param name="message">Message to display</param>
    void ShowStatus(string message);

    /// <summary>
    /// Shows an error message in the UI.
    /// </summary>
    /// <param name="message">Error message to display</param>
    void ShowError(string message);

    /// <summary>
    /// Shows an information message in the UI.
    /// </summary>
    /// <param name="message">Information message to display</param>
    void ShowInfo(string message);

    /// <summary>
    /// Disposes the terminal UI and cleans up resources.
    /// </summary>
    void Shutdown();
}