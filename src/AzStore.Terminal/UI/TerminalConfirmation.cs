using AzStore.Terminal.Utilities;
using Terminal.Gui;

namespace AzStore.Terminal.UI;

/// <summary>
/// Represents the result of a confirmation prompt.
/// </summary>
public enum ConfirmationResult
{
    Yes,
    No,
    Cancelled
}

/// <summary>
/// Handles terminal-based confirmation prompts with single keypress responses.
/// </summary>
public class TerminalConfirmation
{
    /// <summary>
    /// Shows a simple Y/N confirmation prompt and waits for user response.
    /// </summary>
    /// <param name="message">The confirmation message to display.</param>
    /// <param name="defaultChoice">The default choice when Enter is pressed ('Y' or 'N').</param>
    /// <returns>The user's confirmation choice.</returns>
    public static ConfirmationResult ShowConfirmation(string message, char defaultChoice = 'Y')
    {
        var prompt = TerminalProgressRenderer.RenderConfirmationPrompt(message, defaultChoice);
        
        // Display the prompt
        Console.Write(prompt);
        Console.Out.Flush();

        try
        {
            while (true)
            {
                var keyInfo = Console.ReadKey(true);
                
                switch (keyInfo.Key)
                {
                    case ConsoleKey.Y:
                        Console.WriteLine("Y");
                        return ConfirmationResult.Yes;
                        
                    case ConsoleKey.N:
                        Console.WriteLine("N");
                        return ConfirmationResult.No;
                        
                    case ConsoleKey.Enter:
                        // Use default choice
                        Console.WriteLine(defaultChoice);
                        return defaultChoice == 'Y' ? ConfirmationResult.Yes : ConfirmationResult.No;
                        
                    case ConsoleKey.Escape:
                        Console.WriteLine("Cancelled");
                        return ConfirmationResult.Cancelled;
                        
                    default:
                        // Ignore other keys and continue waiting
                        break;
                }
            }
        }
        catch (Exception)
        {
            Console.WriteLine();
            return ConfirmationResult.Cancelled;
        }
    }

    /// <summary>
    /// Shows a confirmation prompt specifically for download operations.
    /// </summary>
    /// <param name="fileName">The name of the file to download.</param>
    /// <param name="fileSize">The size of the file in bytes.</param>
    /// <returns>The user's confirmation choice.</returns>
    public static ConfirmationResult ShowDownloadConfirmation(string fileName, long? fileSize = null)
    {
        var sizeText = fileSize.HasValue ? $" ({TerminalUtils.FormatBytes(fileSize.Value)})" : "";
        var message = $"Download '{fileName}'{sizeText}?";
        
        return ShowConfirmation(message);
    }

    /// <summary>
    /// Shows a confirmation prompt for file overwrite operations.
    /// </summary>
    /// <param name="filePath">The path of the existing file.</param>
    /// <returns>The user's confirmation choice.</returns>
    public static ConfirmationResult ShowOverwriteConfirmation(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var message = $"'{fileName}' already exists. Overwrite?";
        
        return ShowConfirmation(message, 'N'); // Default to No for destructive operations
    }

    /// <summary>
    /// Shows a confirmation prompt with custom options.
    /// </summary>
    /// <param name="message">The confirmation message.</param>
    /// <param name="options">Dictionary of key characters to their descriptions.</param>
    /// <param name="defaultKey">The default key when Enter is pressed.</param>
    /// <returns>The chosen key character, or null if cancelled.</returns>
    public static char? ShowCustomConfirmation(string message, Dictionary<char, string> options, char? defaultKey = null)
    {
        var optionsList = options.Select(kv => 
            kv.Key == defaultKey ? $"[{char.ToUpper(kv.Key)}]" : $"{char.ToLower(kv.Key)}").ToList();
        var optionsText = string.Join("/", optionsList);
        
        var prompt = $"{message} {optionsText}: ";
        Console.Write(prompt);
        Console.Out.Flush();

        try
        {
            while (true)
            {
                var keyInfo = Console.ReadKey(true);
                var pressedChar = char.ToUpper(keyInfo.KeyChar);
                
                if (keyInfo.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine("Cancelled");
                    return null;
                }
                
                if (keyInfo.Key == ConsoleKey.Enter && defaultKey.HasValue)
                {
                    Console.WriteLine(char.ToUpper(defaultKey.Value));
                    return char.ToUpper(defaultKey.Value);
                }
                
                if (options.ContainsKey(pressedChar) || options.ContainsKey(char.ToLower(pressedChar)))
                {
                    Console.WriteLine(pressedChar);
                    return pressedChar;
                }
                
                // Ignore other keys and continue waiting
            }
        }
        catch (Exception)
        {
            Console.WriteLine();
            return null;
        }
    }

    /// <summary>
    /// Shows a conflict resolution prompt for file downloads.
    /// </summary>
    /// <param name="fileName">The name of the conflicting file.</param>
    /// <returns>The chosen resolution option.</returns>
    public static char? ShowConflictResolutionPrompt(string fileName)
    {
        var options = new Dictionary<char, string>
        {
            { 'O', "Overwrite" },
            { 'R', "Rename" },
            { 'S', "Skip" }
        };
        
        var message = $"'{fileName}' already exists.";
        return ShowCustomConfirmation(message, options, 'S'); // Default to Skip
    }


    /// <summary>
    /// Waits for any key press and returns the pressed key.
    /// </summary>
    /// <param name="message">Optional message to display before waiting.</param>
    /// <returns>The pressed key information.</returns>
    public static ConsoleKeyInfo WaitForAnyKey(string message = "Press any key to continue...")
    {
        Console.WriteLine(message);
        return Console.ReadKey(true);
    }

    /// <summary>
    /// Clears the current line in the console.
    /// </summary>
    public static void ClearCurrentLine()
    {
        Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
    }
}