using AzStore.Core.Models.Authentication;
using AzStore.Core.Models.Session;
using AzStore.Core.Services.Abstractions;
using AzStore.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzStore.Terminal.Commands;

/// <summary>
/// Provides session management commands for the REPL environment.
/// Supports creating, listing, switching, and deleting sessions.
/// </summary>
public class SessionCommand : ICommand
{
    private readonly ISessionManager _sessionManager;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<SessionCommand> _logger;
    private readonly IOptions<AzStoreSettings> _settings;
    private readonly AzStore.Terminal.Selection.IAccountSelectionService _accountPicker;

    /// <inheritdoc/>
    public string Name => "session";

    /// <inheritdoc/>
    public string[] Aliases => ["sess"];

    /// <inheritdoc/>
    public string Description => "Manage sessions for Azure Storage accounts";

    /// <summary>
    /// Initializes a new instance of the SessionCommand class.
    /// </summary>
    /// <param name="sessionManager">The session manager service.</param>
    /// <param name="authService">The authentication service.</param>
    /// <param name="logger">Logger instance for this command.</param>
    /// <param name="settings">Configuration settings for the application.</param>
    /// <param name="accountSelectionService">Service to interactively select a storage account when multiple are available.</param>
    public SessionCommand(
        ISessionManager sessionManager,
        IAuthenticationService authService,
        ILogger<SessionCommand> logger,
        IOptions<AzStoreSettings> settings,
        AzStore.Terminal.Selection.IAccountSelectionService accountSelectionService)
    {
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _accountPicker = accountSelectionService ?? throw new ArgumentNullException(nameof(accountSelectionService));
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (args.Length == 0)
        {
            return ShowUsage();
        }

        var subcommand = args[0].ToLowerInvariant();

        return subcommand switch
        {
            "create" => await CreateSessionAsync(args.Skip(1).ToArray(), cancellationToken),
            "list" => ListSessions(),
            "switch" => await SwitchSessionAsync(args.Skip(1).ToArray(), cancellationToken),
            "delete" => await DeleteSessionAsync(args.Skip(1).ToArray(), cancellationToken),
            "current" => ShowCurrentSession(),
            "cleanup" => await CleanupSessionsAsync(args.Skip(1).ToArray(), cancellationToken),
            "stats" => ShowSessionStatistics(),
            "validate" => await ValidateSessionsAsync(args.Skip(1).ToArray(), cancellationToken),
            "help" => ShowUsage(),
            _ => CommandResult.Error($"Unknown session subcommand: {subcommand}. Use :session help for usage.")
        };
    }

    private async Task<CommandResult> CreateSessionAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length < 1)
        {
            return CommandResult.Error("Usage: :session create <name> [storage-account]");
        }

        var sessionName = args[0];
        string? storageAccountName = args.Length > 1 ? args[1] : null;

        try
        {
            _logger.LogInformation("Creating session: {SessionName}", sessionName);

            var authResult = await _authService.GetCurrentAuthenticationAsync(cancellationToken);
            if (authResult == null || !authResult.Success || authResult.SubscriptionId == null)
            {
                return CommandResult.Error("No active authentication found or subscription not available. Please authenticate first.");
            }

            if (string.IsNullOrEmpty(storageAccountName))
            {
                var accounts = (await _authService.GetStorageAccountsAsync(authResult.SubscriptionId.Value, cancellationToken)).ToList();
                if (accounts.Count == 0)
                {
                    return CommandResult.Error("No storage accounts found. Please authenticate first.");
                }

                if (accounts.Count == 1)
                {
                    storageAccountName = accounts[0].AccountName;
                }
                else
                {
                    var selected = await _accountPicker.PickAsync(accounts, cancellationToken);
                    if (selected == null)
                        return CommandResult.Ok("Selection cancelled.");

                    storageAccountName = selected.AccountName;
                }
            }

            var session = await _sessionManager.CreateSessionAsync(
                sessionName,
                storageAccountName,
                authResult.SubscriptionId.Value,
                cancellationToken);

            _sessionManager.SetActiveSession(session);

            return CommandResult.Ok($"Session '{sessionName}' created and activated for storage account '{storageAccountName}'");
        }
        catch (InvalidOperationException ex)
        {
            return CommandResult.Error(ex.Message);
        }
        catch (DirectoryNotFoundException ex)
        {
            return CommandResult.Error(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create session: {SessionName}", sessionName);
            return CommandResult.Error($"Failed to create session: {ex.Message}");
        }
    }

    private CommandResult ListSessions()
    {
        try
        {
            var sessions = _sessionManager.GetAllSessions().ToList();
            var activeSession = _sessionManager.GetActiveSession();

            if (sessions.Count == 0)
            {
                return CommandResult.Ok("No sessions found. Use ':session create' to create a new session.");
            }

            var message = "Available sessions:\n";
            foreach (var session in sessions.OrderByDescending(s => s.LastAccessedAt))
            {
                var isActive = session.Name == activeSession?.Name ? " (active)" : "";
                var lastAccessed = session.LastAccessedAt.ToString("yyyy-MM-dd HH:mm");
                message += $"  {session.Name}{isActive} - {session.StorageAccountName} - {lastAccessed}\n";
                message += $"    Directory: {session.Directory}\n";
            }

            return CommandResult.Ok(message.TrimEnd());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list sessions");
            return CommandResult.Error($"Failed to list sessions: {ex.Message}");
        }
    }

    private async Task<CommandResult> SwitchSessionAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
        {
            return CommandResult.Error("Usage: :session switch <name>");
        }

        var sessionName = args[0];

        try
        {
            var session = _sessionManager.GetSession(sessionName);
            if (session == null)
            {
                return CommandResult.Error($"Session '{sessionName}' not found. Use ':session list' to see available sessions.");
            }

            _sessionManager.ValidateSessionDirectory(session, createIfMissing: true);
            await _sessionManager.TouchSessionAsync(sessionName, cancellationToken);
            _sessionManager.SetActiveSession(session);

            return CommandResult.Ok($"Switched to session '{sessionName}' for storage account '{session.StorageAccountName}'");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch to session: {SessionName}", sessionName);
            return CommandResult.Error($"Failed to switch to session: {ex.Message}");
        }
    }

    private async Task<CommandResult> DeleteSessionAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
        {
            return CommandResult.Error("Usage: :session delete <name>");
        }

        var sessionName = args[0];

        try
        {
            var activeSession = _sessionManager.GetActiveSession();
            if (activeSession?.Name == sessionName)
            {
                _sessionManager.ClearActiveSession();
            }

            var deleted = await _sessionManager.DeleteSessionAsync(sessionName, cancellationToken);
            if (!deleted)
            {
                return CommandResult.Error($"Session '{sessionName}' not found.");
            }

            return CommandResult.Ok($"Session '{sessionName}' deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete session: {SessionName}", sessionName);
            return CommandResult.Error($"Failed to delete session: {ex.Message}");
        }
    }

    private CommandResult ShowCurrentSession()
    {
        try
        {
            var activeSession = _sessionManager.GetActiveSession();
            if (activeSession == null)
            {
                return CommandResult.Ok("No active session. Use ':session create' or ':session switch' to set an active session.");
            }

            var message = $"Current session: {activeSession.Name}\n";
            message += $"Storage account: {activeSession.StorageAccountName}\n";
            message += $"Directory: {activeSession.Directory}\n";
            message += $"Created: {activeSession.CreatedAt:yyyy-MM-dd HH:mm}\n";
            message += $"Last accessed: {activeSession.LastAccessedAt:yyyy-MM-dd HH:mm}";

            return CommandResult.Ok(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show current session");
            return CommandResult.Error($"Failed to show current session: {ex.Message}");
        }
    }

    private async Task<CommandResult> CleanupSessionsAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
        {
            return CommandResult.Error("Usage: :session cleanup <days> [--delete-dirs]");
        }

        if (!int.TryParse(args[0], out var days) || days < 0)
        {
            return CommandResult.Error("Invalid number of days. Please provide a non-negative integer.");
        }

        var deleteDirectories = args.Contains("--delete-dirs", StringComparer.OrdinalIgnoreCase);
        var maxAge = TimeSpan.FromDays(days);

        try
        {
            _logger.LogInformation("Starting session cleanup: sessions older than {Days} days", days);

            var (sessionsRemoved, directoriesDeleted) = await _sessionManager.CleanupOldSessionsAsync(maxAge, deleteDirectories, cancellationToken);

            var message = $"Cleanup completed: {sessionsRemoved} sessions removed";
            if (deleteDirectories)
            {
                message += $", {directoriesDeleted} directories deleted";
            }

            return CommandResult.Ok(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup sessions");
            return CommandResult.Error($"Failed to cleanup sessions: {ex.Message}");
        }
    }

    private CommandResult ShowSessionStatistics()
    {
        try
        {
            var stats = _sessionManager.GetSessionStatistics();
            return CommandResult.Ok(stats.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get session statistics");
            return CommandResult.Error($"Failed to get session statistics: {ex.Message}");
        }
    }

    private async Task<CommandResult> ValidateSessionsAsync(string[] args, CancellationToken cancellationToken)
    {
        var fixDirectories = args.Contains("--fix-dirs", StringComparer.OrdinalIgnoreCase);

        try
        {
            _logger.LogInformation("Starting session validation");

            var invalidCount = await _sessionManager.ValidateAndCleanupSessionsAsync(fixDirectories, cancellationToken);

            var message = $"Validation completed: {invalidCount} invalid sessions removed";
            if (fixDirectories)
            {
                message += " (missing directories were recreated where possible)";
            }

            return CommandResult.Ok(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate sessions");
            return CommandResult.Error($"Failed to validate sessions: {ex.Message}");
        }
    }

    private CommandResult ShowUsage()
    {
        var usage = """
Session Management Commands:

:session create <name> [storage-account]
    Create a new session for the specified storage account.
    Session data directory is {AzStore:SessionsDirectory}/{name}

:session list
    List all available sessions

:session switch <name>
    Switch to an existing session

:session delete <name>
    Delete a session (cannot be undone)

:session current
    Show details of the current active session

:session cleanup <days> [--delete-dirs]
    Clean up sessions not accessed within the specified number of days
    Use --delete-dirs to also delete session directories

:session stats
    Show statistics about session usage and storage

:session validate [--fix-dirs]
    Validate all sessions and remove invalid ones
    Use --fix-dirs to recreate missing directories

:session help
    Show this help message

Examples:
  :session create myapp myappstore
  :session switch myapp
  :session list
  :session cleanup 30
  :session cleanup 7 --delete-dirs
  :session stats
  :session validate --fix-dirs
""";

        return CommandResult.Ok(usage);
    }
}
