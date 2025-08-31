namespace AzStore.Terminal;

/// <summary>
/// Defines the navigation modes for VIM-like modal interaction.
/// </summary>
public enum NavigationMode
{
    /// <summary>
    /// Normal mode for navigation and selection using VIM-like keybindings (j/k/h/l, gg/G).
    /// </summary>
    Normal,

    /// <summary>
    /// Command mode for entering colon-prefixed commands (:ls, :help, :exit).
    /// </summary>
    Command,

    /// <summary>
    /// Visual mode for future multi-select functionality.
    /// </summary>
    Visual
}