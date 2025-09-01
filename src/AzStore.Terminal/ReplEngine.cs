using AzStore.Configuration;
using AzStore.Core;
using AzStore.Terminal.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzStore.Terminal;

public class ReplEngine : IReplEngine
{
    private readonly ThemeSettings _theme;
    private readonly KeyBindings _keyBindings;
    private readonly ILogger<ReplEngine> _logger;
    private readonly ICommandRegistry _commandRegistry;
    private readonly ISessionManager _sessionManager;
    private readonly INavigationEngine _navigationEngine;
    private readonly KeySequenceBuffer _keySequenceBuffer;

    private ReplMode _currentMode = ReplMode.Navigation;
    private string _commandBuffer = string.Empty;
    private bool _isInitialized = false;

    public ReplEngine(IOptions<AzStoreSettings> settings, ILogger<ReplEngine> logger, ICommandRegistry commandRegistry,
        ISessionManager sessionManager, INavigationEngine navigationEngine)
    {
        var settingsValue = settings.Value;
        _theme = settingsValue.Theme;
        _keyBindings = settingsValue.KeyBindings;
        _logger = logger;
        _commandRegistry = commandRegistry;
        _sessionManager = sessionManager;
        _navigationEngine = navigationEngine;
        _keySequenceBuffer = new KeySequenceBuffer(_keyBindings.KeySequenceTimeout);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting REPL session");
        WriteStatus("AzStore CLI - Azure Blob Storage Terminal");

        await InitializeSessionAsync(cancellationToken);

        WriteStatus("Use VIM keys (j/k/l/h) to navigate, ':' for commands, or ':help' for help");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                RenderCurrentState();
                await ProcessInteractionAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in REPL main loop");
                WriteError($"Error: {ex.Message}");
            }
        }

        _logger.LogInformation("REPL session ended");
    }

    public async Task<bool> ProcessInputAsync(string? input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        if (input.StartsWith(':'))
        {
            var command = _commandRegistry.FindCommand(input);
            if (command != null)
            {
                var args = ParseCommandArgs(input);
                var result = await command.ExecuteAsync(args, cancellationToken);

                if (!string.IsNullOrWhiteSpace(result.Message))
                {
                    if (result.Success)
                        WriteInfo(result.Message);
                    else
                        WriteError(result.Message);
                }

                await RefreshNavigationIfNeededAsync(cancellationToken);
                return result.ShouldExit;
            }
            else
            {
                _logger.LogWarning("User entered unknown command: {Command}", input);
                WriteError($"Unknown command: {input}");
                return false;
            }
        }
        else
        {
            _logger.LogWarning("User entered non-command input: {Input}", input);
            WriteError("Commands must start with ':'. Type :help for available commands.");
            return false;
        }
    }

    private async Task InitializeSessionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _sessionManager.LoadSessionsAsync(cancellationToken);
            var activeSession = _sessionManager.GetActiveSession();

            if (activeSession != null)
            {
                await _navigationEngine.InitializeAsync(activeSession, cancellationToken);
                _isInitialized = true;
                _logger.LogInformation("Initialized with active session: {SessionName}", activeSession.Name);
            }
            else
            {
                WriteStatus("No active session. Use ':session create' or ':session switch' to begin.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize session");
            WriteError($"Session initialization failed: {ex.Message}");
        }
    }

    private void RenderCurrentState()
    {
        Console.Clear();
        WriteStatus("AzStore CLI - Azure Blob Storage Terminal");
        Console.WriteLine();

        if (_isInitialized && _navigationEngine.CurrentState != null)
        {
            _navigationEngine.RenderCurrentView();
        }
        else
        {
            WriteInfo("No active session - use ':session create' or ':session switch' to begin browsing");
        }

        RenderPrompt();
    }

    private void RenderPrompt()
    {
        string prompt;

        if (_currentMode == ReplMode.Command)
        {
            prompt = $":{_commandBuffer}";
        }
        else
        {
            var activeSession = _sessionManager.GetActiveSession();
            if (activeSession != null && _navigationEngine.CurrentState != null)
            {
                var breadcrumb = _navigationEngine.GetBreadcrumbPath();
                prompt = $"{activeSession.Name} @ {breadcrumb}> ";
            }
            else
            {
                prompt = "azstore> ";
            }
        }

        WritePrompt(prompt);
    }

    private async Task ProcessInteractionAsync(CancellationToken cancellationToken)
    {
        var keyInfo = Console.ReadKey(true);

        if (_currentMode == ReplMode.Command)
        {
            await ProcessCommandModeInputAsync(keyInfo, cancellationToken);
        }
        else
        {
            await ProcessNavigationModeInputAsync(keyInfo, cancellationToken);
        }
    }

    private async Task ProcessCommandModeInputAsync(ConsoleKeyInfo keyInfo, CancellationToken cancellationToken)
    {
        switch (keyInfo.Key)
        {
            case ConsoleKey.Escape:
                _currentMode = ReplMode.Navigation;
                _commandBuffer = string.Empty;
                break;

            case ConsoleKey.Enter:
                if (!string.IsNullOrWhiteSpace(_commandBuffer))
                {
                    var shouldExit = await ProcessInputAsync($":{_commandBuffer}", cancellationToken);
                    if (shouldExit)
                        return;
                }
                _currentMode = ReplMode.Navigation;
                _commandBuffer = string.Empty;
                break;

            case ConsoleKey.Backspace:
                if (_commandBuffer.Length > 0)
                {
                    _commandBuffer = _commandBuffer[..^1];
                }
                else
                {
                    _currentMode = ReplMode.Navigation;
                }
                break;

            default:
                if (!char.IsControl(keyInfo.KeyChar))
                {
                    _commandBuffer += keyInfo.KeyChar;
                }
                break;
        }
    }

    private async Task ProcessNavigationModeInputAsync(ConsoleKeyInfo keyInfo, CancellationToken cancellationToken)
    {
        if (keyInfo.KeyChar == ':')
        {
            _currentMode = ReplMode.Command;
            _commandBuffer = string.Empty;
            return;
        }

        if (!_isInitialized || _navigationEngine.CurrentState == null)
        {
            return;
        }

        var keyBindingsMap = CreateKeyBindingsMap();
        var (isComplete, matchedBinding, hasPartialMatch) = _keySequenceBuffer.AddKey(keyInfo.KeyChar, keyBindingsMap);

        if (isComplete && matchedBinding.HasValue)
        {
            await _navigationEngine.ProcessKeyActionAsync(matchedBinding.Value, cancellationToken);
        }
        else if (!hasPartialMatch && !isComplete)
        {
            _keySequenceBuffer.Clear();
        }
    }

    private IReadOnlyDictionary<KeyBindingAction, string> CreateKeyBindingsMap()
    {
        return new Dictionary<KeyBindingAction, string>
        {
            { KeyBindingAction.MoveDown, _keyBindings.MoveDown },
            { KeyBindingAction.MoveUp, _keyBindings.MoveUp },
            { KeyBindingAction.Enter, _keyBindings.Enter },
            { KeyBindingAction.Back, _keyBindings.Back },
            { KeyBindingAction.Search, _keyBindings.Search },
            { KeyBindingAction.Top, _keyBindings.Top },
            { KeyBindingAction.Bottom, _keyBindings.Bottom },
            { KeyBindingAction.Download, _keyBindings.Download }
        };
    }

    private async Task RefreshNavigationIfNeededAsync(CancellationToken cancellationToken)
    {
        var activeSession = _sessionManager.GetActiveSession();

        if (activeSession != null && !_isInitialized)
        {
            await _navigationEngine.InitializeAsync(activeSession, cancellationToken);
            _isInitialized = true;
        }
        else if (activeSession == null && _isInitialized)
        {
            _isInitialized = false;
        }
    }

    private static string[] ParseCommandArgs(string input)
    {
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[1..] : [];
    }

    public void WritePrompt(string message)
    {
        WriteColored(message, _theme.PromptColor);
    }

    public void WriteStatus(string message)
    {
        WriteColored(message, _theme.StatusMessageColor);
    }

    public void WriteInfo(string message)
    {
        Console.WriteLine(message);
    }

    public void WriteError(string message)
    {
        WriteColored(message, nameof(ConsoleColor.Red));
    }

    public void WriteColored(string message, string colorName)
    {
        if (Enum.TryParse<ConsoleColor>(colorName, true, out var color))
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(message);
            Console.ForegroundColor = originalColor;
        }
        else
        {
            Console.Write(message);
        }
    }
}