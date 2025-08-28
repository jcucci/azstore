namespace AzStore.Terminal;

/// <summary>
/// Defines the available interaction modes for the REPL engine.
/// </summary>
public enum ReplMode
{
    /// <summary>
    /// Navigation mode for browsing containers and blobs with VIM-like key bindings.
    /// In this mode, single keys like 'j', 'k', 'l', 'h' control navigation.
    /// Multi-character sequences like 'gg', 'G', 'dd' trigger special actions.
    /// Pressing ':' switches to command mode.
    /// </summary>
    Navigation,

    /// <summary>
    /// Command mode for executing colon-prefixed commands like :help, :exit, :session.
    /// In this mode, the user types commands that are processed by the command registry.
    /// Pressing Escape or completing a command returns to navigation mode.
    /// </summary>
    Command
}