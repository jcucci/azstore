using AzStore.Core.Models.Storage;

namespace AzStore.Terminal;

/// <summary>
/// Navigation result containing the action taken and optional data.
/// </summary>
public record NavigationResult(
    NavigationAction Action,
    int? SelectedIndex = null,
    StorageItem? SelectedItem = null,
    string? Command = null,
    KeyBindingAction? KeyBindingAction = null
);