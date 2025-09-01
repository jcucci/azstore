using AzStore.Terminal.Navigation;
using Microsoft.Extensions.Logging;
using Terminal.Gui;
using KeyBindingsConfig = AzStore.Configuration.KeyBindings;

namespace AzStore.Terminal.Input;

/// <summary>
/// Handles keyboard input for VIM-like navigation with support for
/// single keys, multi-character sequences, arrow keys, and special keys.
/// </summary>
public class InputHandler : IInputHandler
{
    private const int KeyRepeatThresholdMs = 100;

    private readonly ILogger<InputHandler> _logger;
    private readonly KeyBindingsConfig _keyBindings;
    private readonly KeySequenceBuffer _keySequenceBuffer;
    private readonly Dictionary<KeyBindingAction, string> _bindingsLookup;
    private readonly Timer _keyRepeatTimer;

    private Key _lastRepeatingKey = (Key)0;
    private bool _hasRepeatingKey;
    private DateTime _lastKeyTime;
    private bool _keyRepeatActive;

    public event EventHandler<NavigationResult>? NavigationRequested;

    public string CurrentSequence => _keySequenceBuffer.CurrentSequence;
    public bool HasPendingSequence => !string.IsNullOrEmpty(CurrentSequence);

    public InputHandler(ILogger<InputHandler> logger, KeyBindingsConfig keyBindings)
    {
        _logger = logger;
        _keyBindings = keyBindings;
        _keySequenceBuffer = new KeySequenceBuffer(_keyBindings.KeySequenceTimeout);
        _keyRepeatTimer = new Timer(OnKeyRepeatTimer, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

        // Create reverse lookup dictionary for binding matching
        _bindingsLookup = new Dictionary<KeyBindingAction, string>
        {
            { KeyBindingAction.MoveDown, _keyBindings.MoveDown },
            { KeyBindingAction.MoveUp, _keyBindings.MoveUp },
            { KeyBindingAction.Enter, _keyBindings.Enter },
            { KeyBindingAction.Back, _keyBindings.Back },
            { KeyBindingAction.Search, _keyBindings.Search },
            { KeyBindingAction.Command, _keyBindings.Command },
            { KeyBindingAction.Top, _keyBindings.Top },
            { KeyBindingAction.Bottom, _keyBindings.Bottom },
            { KeyBindingAction.Download, _keyBindings.Download },
            { KeyBindingAction.Refresh, _keyBindings.Refresh },
            { KeyBindingAction.Info, _keyBindings.Info },
            { KeyBindingAction.Help, _keyBindings.Help }
        };
    }

    public bool ProcessKeyEvent(Key keyEvent)
    {
        var now = DateTime.UtcNow;

        if (IsRepeatableKey(keyEvent))
        {
            if (_hasRepeatingKey && _lastRepeatingKey == keyEvent)
            {
                // Same key pressed again - could be natural repeat or user holding
                var timeSinceLastKey = (now - _lastKeyTime).TotalMilliseconds;
                if (timeSinceLastKey < KeyRepeatThresholdMs) // Very fast repeat, likely from holding key
                {
                    return true; // Ignore rapid repeats, let timer handle them
                }
            }
            else
            {
                // Different key or first press - stop any current repeat
                StopKeyRepeat();
                _lastRepeatingKey = keyEvent;
                StartKeyRepeat(keyEvent);
            }

            _lastKeyTime = now;
        }
        else
        {
            // Non-repeatable key pressed - stop any repeat
            StopKeyRepeat();
        }

        // Handle special keys that don't require sequence processing
        if (keyEvent == Key.Enter)
        {
            RaiseNavigationRequest(NavigationAction.Enter);
            return true;
        }
        if (keyEvent == Key.CursorDown)
        {
            RaiseNavigationRequest(NavigationAction.Enter, keyBindingAction: KeyBindingAction.MoveDown);
            return true;
        }
        if (keyEvent == Key.CursorUp)
        {
            RaiseNavigationRequest(NavigationAction.Enter, keyBindingAction: KeyBindingAction.MoveUp);
            return true;
        }
        if (keyEvent == Key.CursorRight)
        {
            RaiseNavigationRequest(NavigationAction.Enter);
            return true;
        }
        if (keyEvent == Key.CursorLeft)
        {
            RaiseNavigationRequest(NavigationAction.Back);
            return true;
        }
        if (keyEvent == Key.Esc)
        {
            Clear();
            RaiseNavigationRequest(NavigationAction.Cancel);
            return true;
        }

        // Convert key to character for sequence processing
        var keyChar = (char)(uint)keyEvent;

        // Terminal.Gui encodes A-Z keys as KeyCode.A-Z, but uses ShiftMask to distinguish case:
        // - lowercase 'a'-'z': KeyCode.A-Z without ShiftMask
        // - uppercase 'A'-'Z': KeyCode.A-Z with ShiftMask
        // The cast to char always gives uppercase, so we need to check ShiftMask for the original case
        if (keyChar >= 'A' && keyChar <= 'Z')
        {
            // Check if Shift is NOT pressed, meaning this was originally lowercase
            if (!keyEvent.IsShift)
            {
                keyChar = char.ToLower(keyChar);
            }
            // If Shift IS pressed, keep as uppercase
        }

        // Check if this key completes a binding sequence
        var (isComplete, matchedBinding, hasPartialMatch) =
            _keySequenceBuffer.AddKey(keyChar, _bindingsLookup);

        if (isComplete && matchedBinding != null)
        {
            // Execute the matched binding
            var action = matchedBinding switch
            {
                KeyBindingAction.MoveDown => NavigationAction.Enter,
                KeyBindingAction.MoveUp => NavigationAction.Enter,
                KeyBindingAction.Enter => NavigationAction.Enter,
                KeyBindingAction.Back => NavigationAction.Back,
                KeyBindingAction.Search => NavigationAction.Command,
                KeyBindingAction.Command => NavigationAction.Command,
                KeyBindingAction.Top => NavigationAction.JumpToTop,
                KeyBindingAction.Bottom => NavigationAction.JumpToBottom,
                KeyBindingAction.Download => NavigationAction.Download,
                _ => NavigationAction.None
            };

            if (action != NavigationAction.None)
            {
                RaiseNavigationRequest(action, keyBindingAction: matchedBinding);
                return true;
            }
        }
        else if (!hasPartialMatch)
        {
            // No match and no partial match - clear buffer
            _keySequenceBuffer.Clear();
            return false;
        }

        // If hasPartialMatch is true, we keep the sequence for potential completion
        return hasPartialMatch;
    }

    public void Clear()
    {
        _keySequenceBuffer.Clear();
        _logger.LogDebug("Input handler cleared");
    }

    private void RaiseNavigationRequest(NavigationAction action, int selectedIndex = -1, KeyBindingAction? keyBindingAction = null, string? command = null)
    {
        NavigationResult result = action switch
        {
            NavigationAction.Command when keyBindingAction == KeyBindingAction.Search =>
                new NavigationResult(action, Command: "/", KeyBindingAction: keyBindingAction),

            NavigationAction.Command when keyBindingAction == KeyBindingAction.Command =>
                new NavigationResult(action, Command: ":", KeyBindingAction: keyBindingAction),

            NavigationAction.Command when !string.IsNullOrEmpty(command) =>
                new NavigationResult(action, Command: command, KeyBindingAction: keyBindingAction),

            _ => new NavigationResult(action, selectedIndex, KeyBindingAction: keyBindingAction)
        };

        NavigationRequested?.Invoke(this, result);

        var logMessage = keyBindingAction != null
            ? $"Navigation requested: {action} via {keyBindingAction}"
            : $"Navigation requested: {action}";

        _logger.LogDebug(logMessage);
    }

    private bool IsRepeatableKey(Key keyEvent)
    {
        if (keyEvent == Key.CursorUp || keyEvent == Key.CursorDown || keyEvent == Key.CursorLeft || keyEvent == Key.CursorRight)
            return true;

        if (keyEvent == (Key)'j' || keyEvent == (Key)'k' || keyEvent == (Key)'h' || keyEvent == (Key)'l')
            return true;

        return false;
    }

    private void StartKeyRepeat(Key keyEvent)
    {
        _lastRepeatingKey = keyEvent;
        _hasRepeatingKey = true;
        _keyRepeatActive = false; // Will be set to true after initial delay

        // Start timer with initial delay
        _keyRepeatTimer.Change(_keyBindings.KeyRepeatDelay, System.Threading.Timeout.Infinite);

        _logger.LogDebug("Started key repeat for: {Key}", keyEvent);
    }

    private void StopKeyRepeat()
    {
        _keyRepeatTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        _keyRepeatActive = false;
        _hasRepeatingKey = false;

        _logger.LogDebug("Stopped key repeat");
    }

    private void OnKeyRepeatTimer(object? state)
    {
        if (!_hasRepeatingKey) return;

        if (!_keyRepeatActive)
        {
            // First repeat after initial delay - switch to repeat interval
            _keyRepeatActive = true;
            _keyRepeatTimer.Change(_keyBindings.KeyRepeatInterval, _keyBindings.KeyRepeatInterval);
        }

        // Generate repeat key event
        // Note: Using Task.Run to avoid recursive call on timer thread
        // This may need refinement if Terminal.Gui requires UI thread synchronization
        Task.Run(() => ProcessKeyEvent(_lastRepeatingKey));
    }

    public void Dispose()
    {
        _keyRepeatTimer?.Dispose();
    }
}