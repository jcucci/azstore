using AzStore.Configuration;
using AzStore.Terminal.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzStore.Terminal;

public class ReplEngine
{
    private readonly ThemeSettings _theme;
    private readonly ILogger<ReplEngine> _logger;
    private readonly ICommandRegistry _commandRegistry;

    public ReplEngine(IOptions<AzStoreSettings> settings, ILogger<ReplEngine> logger, ICommandRegistry commandRegistry)
    {
        _theme = settings.Value.Theme;
        _logger = logger;
        _commandRegistry = commandRegistry;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting REPL session");
        WriteStatus("AzStore CLI - Azure Blob Storage Terminal");
        WriteStatus("Type :help for commands or :exit to quit");

        while (!cancellationToken.IsCancellationRequested)
        {
            WritePrompt("> ");

            string? input;
            try
            {
                input = await Console.In.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (cancellationToken.IsCancellationRequested)
                break;

            if (string.IsNullOrWhiteSpace(input))
                continue;

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
                    
                    if (result.ShouldExit)
                        break;
                }
                else
                {
                    _logger.LogWarning("User entered unknown command: {Command}", input);
                    WriteError($"Unknown command: {input}");
                }
            }
            else
            {
                _logger.LogWarning("User entered non-command input: {Input}", input);
                WriteError("Commands must start with ':'. Type :help for available commands.");
            }
        }

        _logger.LogInformation("REPL session ended");
    }

    private static string[] ParseCommandArgs(string input)
    {
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[1..] : Array.Empty<string>();
    }
    private void WritePrompt(string message)
    {
        WriteColored(message, _theme.PromptColor);
    }

    private void WriteStatus(string message)
    {
        WriteColored(message, _theme.StatusMessageColor);
    }

    private void WriteInfo(string message)
    {
        Console.WriteLine(message);
    }

    private void WriteError(string message)
    {
        WriteColored(message, nameof(ConsoleColor.Red));
    }

    private void WriteColored(string message, string colorName)
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