namespace AzStore.Terminal;

/// <summary>
/// Result of navigation operations indicating user action.
/// </summary>
public enum NavigationAction
{
    /// <summary>No action taken</summary>
    None,
    /// <summary>Navigate into selected item (Enter/l key)</summary>
    Enter,
    /// <summary>Navigate back/up one level (h key)</summary>
    Back,
    /// <summary>Exit the application</summary>
    Exit,
    /// <summary>Refresh current view</summary>
    Refresh,
    /// <summary>Show help</summary>
    Help,
    /// <summary>Enter command mode</summary>
    Command
}