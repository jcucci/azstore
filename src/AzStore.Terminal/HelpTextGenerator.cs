using AzStore.Configuration;
using AzStore.Core.Models.Navigation;
using System.Text;

namespace AzStore.Terminal;

/// <summary>
/// Generates formatted help text for key bindings and commands.
/// </summary>
public class HelpTextGenerator
{
    private readonly KeyBindings _keyBindings;

    /// <summary>
    /// Initializes a new instance of the HelpTextGenerator class.
    /// </summary>
    /// <param name="keyBindings">The key bindings configuration.</param>
    public HelpTextGenerator(KeyBindings keyBindings)
    {
        _keyBindings = keyBindings ?? throw new ArgumentNullException(nameof(keyBindings));
    }

    /// <summary>
    /// Generates a full help screen with all available key bindings.
    /// </summary>
    /// <returns>A formatted help text string.</returns>
    public string GenerateFullHelp()
    {
        var sb = new StringBuilder();

        sb.AppendLine("AzStore - Azure Blob Storage Terminal");
        sb.AppendLine("====================================");
        sb.AppendLine();

        sb.AppendLine("NAVIGATION:");
        sb.AppendLine($"  {_keyBindings.MoveDown,-8} Move down");
        sb.AppendLine($"  {_keyBindings.MoveUp,-8} Move up");
        sb.AppendLine($"  {_keyBindings.Enter,-8} Navigate into selected item");
        sb.AppendLine($"  {_keyBindings.Back,-8} Navigate back/up one level");
        sb.AppendLine($"  {_keyBindings.Top,-8} Jump to top of list");
        sb.AppendLine($"  {_keyBindings.Bottom,-8} Jump to bottom of list");
        sb.AppendLine($"  ↑/↓      Arrow keys (alternative navigation)");
        sb.AppendLine($"  Enter    Alternative to '{_keyBindings.Enter}' for navigation");
        sb.AppendLine();

        sb.AppendLine("ACTIONS:");
        sb.AppendLine($"  {_keyBindings.Download,-8} Download selected blob");
        sb.AppendLine($"  {_keyBindings.Info,-8} Show item details/information");
        sb.AppendLine($"  {_keyBindings.Refresh,-8} Refresh current view");
        sb.AppendLine($"  {_keyBindings.Help,-8} Show this help screen");
        sb.AppendLine();

        sb.AppendLine("MODES:");
        sb.AppendLine($"  {_keyBindings.Search,-8} Enter search mode");
        sb.AppendLine($"  {_keyBindings.Command,-8} Enter command mode");
        sb.AppendLine($"  Escape   Exit mode/cancel operation");
        sb.AppendLine();

        sb.AppendLine("COMMANDS (accessed via ':'):");
        sb.AppendLine("  :help    Show available commands");
        sb.AppendLine("  :exit    Exit application");
        sb.AppendLine("  :q       Quick exit");
        sb.AppendLine("  :ls      List items in current location");
        sb.AppendLine("  :download <item> Download specific item");
        sb.AppendLine();

        sb.AppendLine("TIPS:");
        sb.AppendLine("• Use VIM-like navigation for fast browsing");
        sb.AppendLine("• Multi-character keys (like 'gg') have a timeout");
        sb.AppendLine("• Press 'Escape' to cancel any ongoing operation");
        sb.AppendLine("• File sizes and progress are shown during downloads");
        sb.AppendLine();

        sb.AppendLine("Press any key to return to navigation...");

        return sb.ToString();
    }

    /// <summary>
    /// Generates a compact help summary for status line display.
    /// </summary>
    /// <returns>A single-line help summary.</returns>
    public string GenerateCompactHelp()
    {
        return $"{_keyBindings.MoveDown}/{_keyBindings.MoveUp}:nav {_keyBindings.Enter}:enter {_keyBindings.Back}:back {_keyBindings.Download}:download {_keyBindings.Info}:info {_keyBindings.Refresh}:refresh {_keyBindings.Help}:help {_keyBindings.Command}:cmd";
    }

    /// <summary>
    /// Generates context-specific help based on current navigation level.
    /// </summary>
    /// <param name="level">The current navigation level.</param>
    /// <returns>Context-appropriate help text.</returns>
    public string GenerateContextualHelp(NavigationLevel level)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Context: {GetLevelDescription(level)}");
        sb.AppendLine(new string('-', 40));

        switch (level)
        {
            case NavigationLevel.StorageAccount:
                sb.AppendLine("Available actions:");
                sb.AppendLine($"• {_keyBindings.Enter} - Browse container contents");
                sb.AppendLine($"• {_keyBindings.Info} - View container properties");
                sb.AppendLine($"• {_keyBindings.Refresh} - Refresh container list");
                break;

            case NavigationLevel.Container:
                sb.AppendLine("Available actions:");
                sb.AppendLine($"• {_keyBindings.Enter} - Navigate into folder or view file");
                sb.AppendLine($"• {_keyBindings.Download} - Download selected blob");
                sb.AppendLine($"• {_keyBindings.Info} - View blob properties");
                sb.AppendLine($"• {_keyBindings.Back} - Return to containers");
                sb.AppendLine($"• {_keyBindings.Refresh} - Refresh blob list");
                break;

            case NavigationLevel.BlobPrefix:
                sb.AppendLine("Available actions:");
                sb.AppendLine($"• {_keyBindings.Enter} - Navigate into subfolder or view file");
                sb.AppendLine($"• {_keyBindings.Download} - Download selected blob");
                sb.AppendLine($"• {_keyBindings.Info} - View blob properties");
                sb.AppendLine($"• {_keyBindings.Back} - Return to parent folder");
                sb.AppendLine($"• {_keyBindings.Refresh} - Refresh current view");
                break;
        }

        sb.AppendLine();
        sb.AppendLine($"Press {_keyBindings.Help} for full help or any other key to continue...");

        return sb.ToString();
    }

    /// <summary>
    /// Generates help text for download operations.
    /// </summary>
    /// <returns>Download-specific help text.</returns>
    public string GenerateDownloadHelp()
    {
        var sb = new StringBuilder();

        sb.AppendLine("Download Operations");
        sb.AppendLine("==================");
        sb.AppendLine();

        sb.AppendLine("Key Bindings:");
        sb.AppendLine($"  {_keyBindings.Download,-8} Download selected blob");
        sb.AppendLine("  Escape   Cancel ongoing download");
        sb.AppendLine();

        sb.AppendLine("Download Process:");
        sb.AppendLine("1. Select a blob file");
        sb.AppendLine($"2. Press '{_keyBindings.Download}' to initiate download");
        sb.AppendLine("3. Confirm download when prompted");
        sb.AppendLine("4. Monitor progress in status area");
        sb.AppendLine("5. Handle conflicts if file exists");
        sb.AppendLine();

        sb.AppendLine("Conflict Resolution:");
        sb.AppendLine("  O - Overwrite existing file");
        sb.AppendLine("  R - Rename new file");
        sb.AppendLine("  S - Skip download (default)");
        sb.AppendLine();

        sb.AppendLine("Press any key to continue...");

        return sb.ToString();
    }

    /// <summary>
    /// Gets a human-readable description of a navigation level.
    /// </summary>
    /// <param name="level">The navigation level.</param>
    /// <returns>A descriptive string.</returns>
    private static string GetLevelDescription(NavigationLevel level) => level switch
    {
        NavigationLevel.StorageAccount => "Storage Account (browsing containers)",
        NavigationLevel.Container => "Container (browsing blobs and folders)",
        NavigationLevel.BlobPrefix => "Folder (browsing subfolder contents)",
        _ => "Unknown level"
    };

    /// <summary>
    /// Generates a quick reference card with the most common operations.
    /// </summary>
    /// <returns>Quick reference text.</returns>
    public string GenerateQuickReference()
    {
        var sb = new StringBuilder();

        sb.AppendLine("Quick Reference");
        sb.AppendLine("===============");
        sb.AppendLine();

        sb.AppendLine("Essential Keys:");
        sb.AppendLine($"  {_keyBindings.MoveDown}/{_keyBindings.MoveUp}    Navigate up/down");
        sb.AppendLine($"  {_keyBindings.Enter}      Enter/select");
        sb.AppendLine($"  {_keyBindings.Back}      Go back");
        sb.AppendLine($"  {_keyBindings.Download}     Download file");
        sb.AppendLine($"  {_keyBindings.Help}      Help (this screen)");
        sb.AppendLine();

        sb.AppendLine("Quick Actions:");
        sb.AppendLine($"  {_keyBindings.Top}     Jump to top");
        sb.AppendLine($"  {_keyBindings.Bottom}      Jump to bottom");
        sb.AppendLine($"  {_keyBindings.Info}      Show details");
        sb.AppendLine($"  {_keyBindings.Refresh}      Refresh view");
        sb.AppendLine();

        sb.AppendLine($"Commands: {_keyBindings.Command} | Search: {_keyBindings.Search} | Exit: Escape");

        return sb.ToString();
    }

    /// <summary>
    /// Generates help for command mode operations.
    /// </summary>
    /// <returns>Command mode help text.</returns>
    public string GenerateCommandModeHelp()
    {
        var sb = new StringBuilder();

        sb.AppendLine("Command Mode (accessed via ':')");
        sb.AppendLine("===============================");
        sb.AppendLine();

        sb.AppendLine("Available Commands:");
        sb.AppendLine("  help         Show command help");
        sb.AppendLine("  exit, quit   Exit application");
        sb.AppendLine("  q            Quick exit");
        sb.AppendLine("  ls, list     List current items");
        sb.AppendLine("  download     Download selected or specified item");
        sb.AppendLine("  session      Show session information");
        sb.AppendLine();

        sb.AppendLine("Usage Examples:");
        sb.AppendLine("  :help");
        sb.AppendLine("  :download myfile.pdf");
        sb.AppendLine("  :ls");
        sb.AppendLine("  :exit");
        sb.AppendLine();

        sb.AppendLine("Press Escape to exit command mode");

        return sb.ToString();
    }
}