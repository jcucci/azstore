using AzStore.Configuration;
using Microsoft.Extensions.Options;

namespace AzStore.Terminal;

public class ReplEngine
{
    private readonly ThemeSettings _theme;

    public ReplEngine(IOptions<AzStoreSettings> settings)
    {
        _theme = settings.Value.Theme;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        WriteStatus("AzStore CLI - Azure Blob Storage Terminal");
        WriteStatus("Type :help for commands or :exit to quit");
        
        while (!cancellationToken.IsCancellationRequested)
        {
            WritePrompt("> ");
            
            string? input;
            try
            {
                input = await Console.In.ReadLineAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            
            if (cancellationToken.IsCancellationRequested)
                break;
                
            if (string.IsNullOrWhiteSpace(input))
                continue;
                
            // TODO: Refactor command handling to separate command processor classes
            if (input == ":exit" || input == ":q")
                break;
                
            if (input == ":help")
            {
                WriteInfo("Available commands:");
                WriteInfo("  :help - Show this help message");
                WriteInfo("  :exit, :q - Exit the application");
                WriteInfo("  :ls, :list - List downloaded files");
                continue;
            }
            
            if (input == ":ls" || input == ":list")
            {
                WriteInfo("No files downloaded yet.");
                continue;
            }
            
            WriteError($"Unknown command: {input}");
        }
    }

    // TODO: Refactor these console writing methods to a shared ConsoleWriter class
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