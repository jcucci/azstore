using AzStore.Core.Services.Abstractions;
using AzStore.Terminal.UI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzStore.CLI;

public sealed class TuiHostedService : BackgroundService
{
    private readonly ILogger<TuiHostedService> _logger;
    private readonly ITerminalUI _ui;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ISessionManager _sessionManager;
    private readonly IAuthenticationService _authService;
    private readonly string? _sessionName;

    public TuiHostedService(
        ILogger<TuiHostedService> logger,
        ITerminalUI ui,
        IHostApplicationLifetime lifetime,
        ISessionManager sessionManager,
        IAuthenticationService authService,
        SessionNameProvider sessionNameProvider)
    {
        _logger = logger;
        _ui = ui;
        _lifetime = lifetime;
        _sessionManager = sessionManager;
        _authService = authService;
        _sessionName = sessionNameProvider.SessionName;
        _lifetime.ApplicationStopping.Register(TryShutdown);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting AzStore TUI");

            // Initialize session before starting the UI
            await InitializeSessionAsync(stoppingToken);

            // Check if application was stopped during initialization
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            await _ui.RunAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("TUI canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TUI encountered an error");
        }
        finally
        {
            TryShutdown();
            _lifetime.StopApplication();
        }
    }

    private void TryShutdown()
    {
        try
        {
            _ui.Shutdown();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to shutdown Terminal UI");
        }
    }

    private async Task InitializeSessionAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing session");

        // Load existing sessions
        await _sessionManager.LoadSessionsAsync(cancellationToken);

        string? sessionName = _sessionName;

        // If no session name provided via command line, prompt the user
        if (string.IsNullOrWhiteSpace(sessionName))
        {
            sessionName = await PromptForSessionNameAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(sessionName))
            {
                Console.WriteLine("Error: No session name provided. Exiting.");
                _lifetime.StopApplication();
                return;
            }
        }

        // Validate session name format
        if (sessionName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            Console.WriteLine($"Error: Session name contains invalid characters: {sessionName}");
            _lifetime.StopApplication();
            return;
        }

        // Check if session exists
        var existingSession = _sessionManager.GetSession(sessionName);

        if (existingSession != null)
        {
            // Load existing session
            _logger.LogInformation("Loading existing session: {SessionName}", sessionName);
            _sessionManager.ValidateSessionDirectory(existingSession, createIfMissing: true);
            await _sessionManager.TouchSessionAsync(sessionName, cancellationToken);
            _sessionManager.SetActiveSession(existingSession);
            Console.WriteLine($"Session '{sessionName}' loaded successfully.");
        }
        else
        {
            // Create new session
            _logger.LogInformation("Creating new session: {SessionName}", sessionName);
            var session = await CreateNewSessionAsync(sessionName, cancellationToken);
            if (session == null)
            {
                Console.WriteLine($"Error: Failed to create session '{sessionName}'. Exiting.");
                _lifetime.StopApplication();
                return;
            }

            _sessionManager.SetActiveSession(session);
            Console.WriteLine($"Session '{sessionName}' created successfully.");
        }
    }

    private static Task<string?> PromptForSessionNameAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Console.Write("Enter session name (or press Ctrl+C to exit): ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Session name cannot be empty. Please try again.");
                continue;
            }

            if (input.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                Console.WriteLine("Session name contains invalid characters. Please try again.");
                continue;
            }

            return Task.FromResult<string?>(input.Trim());
        }
    }

    private async Task<Core.Models.Session.Session?> CreateNewSessionAsync(string sessionName, CancellationToken cancellationToken)
    {
        try
        {
            // Authenticate to get subscription ID
            var authResult = await _authService.GetCurrentAuthenticationAsync(cancellationToken);
            if (authResult == null || !authResult.Success || authResult.SubscriptionId == null)
            {
                Console.WriteLine("No active authentication found. Authenticating...");
                authResult = await _authService.AuthenticateAsync(cancellationToken);

                if (!authResult.Success || authResult.SubscriptionId == null)
                {
                    Console.WriteLine("Authentication failed. Unable to create session.");
                    return null;
                }
            }

            // Create session without storage account - user will select it later from TUI
            string? storageAccount = null;
            var session = await _sessionManager.CreateSessionAsync(
                sessionName,
                storageAccount,
                authResult.SubscriptionId.Value,
                cancellationToken);

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create session: {SessionName}", sessionName);
            Console.WriteLine($"Error creating session: {ex.Message}");
            return null;
        }
    }
}
