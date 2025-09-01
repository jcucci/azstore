namespace AzStore.Terminal.Input;

/// <summary>
/// Defines the available key binding actions for the blob browser interface.
/// </summary>
public enum KeyBindingAction
{
    /// <summary>Move selection down in the list</summary>
    MoveDown,
    /// <summary>Move selection up in the list</summary>
    MoveUp,
    /// <summary>Select/enter the current item</summary>
    Enter,
    /// <summary>Navigate back/up one level</summary>
    Back,
    /// <summary>Enter search mode</summary>
    Search,
    /// <summary>Enter command mode</summary>
    Command,
    /// <summary>Jump to top of list</summary>
    Top,
    /// <summary>Jump to bottom of list</summary>
    Bottom,
    /// <summary>Download selected item</summary>
    Download,
    /// <summary>Refresh the current view</summary>
    Refresh,
    /// <summary>Show item details/information</summary>
    Info,
    /// <summary>Show help/key bindings</summary>
    Help
}