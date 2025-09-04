namespace AzStore.Terminal.Utilities;

/// Provides a way to temporarily suppress console log output during interactive UI sections
/// to avoid visual interference. Implementations should not affect file logging.
public interface IConsoleLogScope
{
    /// Begins a suppression scope that reduces console log verbosity until disposed.
    /// Returns an IDisposable token that restores the previous console log level when disposed.
    IDisposable Suppress();
}

