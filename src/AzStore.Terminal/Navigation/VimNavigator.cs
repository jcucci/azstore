using AzStore.Terminal.Events;
using AzStore.Terminal.Input;
using Microsoft.Extensions.Logging;

namespace AzStore.Terminal.Navigation;

/// <summary>
/// Provides VIM-like navigation state machine functionality for modal interaction.
/// </summary>
public class VimNavigator
{
    private const int MaxCommandLength = 256;
    private readonly ILogger<VimNavigator> _logger;
    private readonly Stack<NavigationMode> _modeHistory = [];

    private NavigationMode _currentMode = NavigationMode.Normal;
    private string? _pendingCommand;

    /// <summary>
    /// Gets the current navigation mode.
    /// </summary>
    public NavigationMode CurrentMode => _currentMode;

    /// <summary>
    /// Gets a value indicating whether there is a pending command in command mode.
    /// </summary>
    public bool HasPendingCommand => !string.IsNullOrEmpty(_pendingCommand);

    /// <summary>
    /// Gets the current pending command, if any.
    /// </summary>
    public string? PendingCommand => _pendingCommand;

    /// <summary>
    /// Event raised when the navigation mode changes.
    /// </summary>
    public event EventHandler<NavigationModeChangedEventArgs>? ModeChanged;

    /// <summary>
    /// Initializes a new instance of the VimNavigator class.
    /// </summary>
    /// <param name="logger">Logger instance for this service.</param>
    public VimNavigator(ILogger<VimNavigator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Switches to the specified navigation mode.
    /// </summary>
    /// <param name="mode">The mode to switch to.</param>
    /// <returns>true if the mode was changed; false if already in the specified mode.</returns>
    public bool SwitchMode(NavigationMode mode)
    {
        if (_currentMode == mode)
            return false;

        var previousMode = _currentMode;
        _modeHistory.Push(_currentMode);
        _currentMode = mode;

        if (mode != NavigationMode.Command)
        {
            _pendingCommand = null;
        }

        _logger.LogDebug("Navigation mode changed from {PreviousMode} to {NewMode}", previousMode, mode);
        ModeChanged?.Invoke(this, new NavigationModeChangedEventArgs(previousMode, mode));

        return true;
    }

    /// <summary>
    /// Returns to the previous navigation mode.
    /// </summary>
    /// <returns>true if the mode was changed; false if no previous mode exists.</returns>
    public bool ExitMode()
    {
        if (_modeHistory.Count == 0)
            return false;

        var previousMode = _currentMode;
        _currentMode = _modeHistory.Pop();
        _pendingCommand = null;

        _logger.LogDebug("Exited mode {PreviousMode}, returned to {CurrentMode}", previousMode, _currentMode);
        ModeChanged?.Invoke(this, new NavigationModeChangedEventArgs(previousMode, _currentMode));

        return true;
    }

    /// <summary>
    /// Enters command mode, switching from the current mode.
    /// </summary>
    /// <param name="commandPrefix">The command prefix (typically ":").</param>
    public void EnterCommandMode(string commandPrefix = ":")
    {
        if (SwitchMode(NavigationMode.Command))
        {
            _pendingCommand = commandPrefix;
            _logger.LogDebug("Entered command mode with prefix: {Prefix}", commandPrefix);
        }
    }

    /// <summary>
    /// Appends a character to the pending command in command mode.
    /// </summary>
    /// <param name="character">The character to append.</param>
    /// <returns>true if the character was appended; false if not in command mode or command would exceed maximum length.</returns>
    public bool AppendToCommand(char character)
    {
        if (_currentMode != NavigationMode.Command)
            return false;

        if (_pendingCommand?.Length >= MaxCommandLength)
        {
            _logger.LogWarning("Command length limit reached ({MaxLength}), ignoring input", MaxCommandLength);
            return false;
        }

        _pendingCommand += character;
        _logger.LogTrace("Appended '{Character}' to command: {Command}", character, _pendingCommand);
        return true;
    }

    /// <summary>
    /// Removes the last character from the pending command in command mode.
    /// </summary>
    /// <returns>true if a character was removed; false if command is empty or not in command mode.</returns>
    public bool BackspaceCommand()
    {
        if (_currentMode != NavigationMode.Command || string.IsNullOrEmpty(_pendingCommand) || _pendingCommand.Length <= 1)
            return false;

        _pendingCommand = _pendingCommand[..^1];
        _logger.LogTrace("Backspaced command to: {Command}", _pendingCommand);
        return true;
    }

    /// <summary>
    /// Completes the pending command and returns it.
    /// </summary>
    /// <returns>The completed command string, or null if no command is pending.</returns>
    public string? CompleteCommand()
    {
        if (_currentMode != NavigationMode.Command || string.IsNullOrEmpty(_pendingCommand))
            return null;

        var completedCommand = _pendingCommand;
        _pendingCommand = null;

        _logger.LogDebug("Completed command: {Command}", completedCommand);
        return completedCommand;
    }

    /// <summary>
    /// Determines if the specified key binding action is valid for the current mode.
    /// </summary>
    /// <param name="action">The key binding action to validate.</param>
    /// <returns>true if the action is valid for the current mode; otherwise, false.</returns>
    public bool IsActionValidForCurrentMode(KeyBindingAction action) => _currentMode switch
    {
        NavigationMode.Normal => action switch
        {
            KeyBindingAction.MoveDown or
            KeyBindingAction.MoveUp or
            KeyBindingAction.Enter or
            KeyBindingAction.Back or
            KeyBindingAction.Top or
            KeyBindingAction.Bottom or
            KeyBindingAction.Download or
            KeyBindingAction.Search or
            KeyBindingAction.Command or
            KeyBindingAction.Refresh or
            KeyBindingAction.Info or
            KeyBindingAction.Help => true,
            _ => false
        },
        NavigationMode.Command => action == KeyBindingAction.Command,
        NavigationMode.Visual => false, // Placeholder for future implementation
        _ => false
    };

    /// <summary>
    /// Resets the navigator to normal mode and clears any pending commands.
    /// </summary>
    public void Reset()
    {
        var previousMode = _currentMode;
        _currentMode = NavigationMode.Normal;
        _modeHistory.Clear();
        _pendingCommand = null;

        _logger.LogDebug("Reset navigator to normal mode");

        if (previousMode != NavigationMode.Normal)
        {
            ModeChanged?.Invoke(this, new NavigationModeChangedEventArgs(previousMode, NavigationMode.Normal));
        }
    }

    /// <summary>
    /// Gets a description of the current mode for display purposes.
    /// </summary>
    /// <returns>A user-friendly description of the current mode.</returns>
    public string GetModeDescription() => _currentMode switch
    {
        NavigationMode.Normal => "NORMAL",
        NavigationMode.Command => $"COMMAND {_pendingCommand}",
        NavigationMode.Visual => "VISUAL",
        _ => "UNKNOWN"
    };
}