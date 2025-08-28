using System.Text.Json;
using AzStore.Core.Models;
using Microsoft.Extensions.Logging;

namespace AzStore.Core;

/// <summary>
/// Provides session management functionality for Azure Blob Storage interactions.
/// Handles session persistence, active session tracking, and directory validation.
/// </summary>
public class SessionManager : ISessionManager
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly ILogger<SessionManager> _logger;
    private readonly Dictionary<string, Session> _sessions = [];

    private Session? _activeSession;
    private readonly string _sessionFilePath;

    /// <summary>
    /// Initializes a new instance of the SessionManager class.
    /// </summary>
    /// <param name="logger">Logger instance for this service.</param>
    public SessionManager(ILogger<SessionManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configPath = Path.Combine(appDataPath, "azstore");
        Directory.CreateDirectory(configPath);
        _sessionFilePath = Path.Combine(configPath, "sessions.json");
    }

    /// <inheritdoc/>
    public async Task<Session> CreateSessionAsync(string name, string directory, string storageAccountName, Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageAccountName);

        _logger.LogInformation("Creating session: {SessionName} for storage account: {StorageAccount}", name, storageAccountName);

        if (_sessions.ContainsKey(name))
        {
            throw new InvalidOperationException($"Session '{name}' already exists");
        }

        var fullPath = Path.GetFullPath(directory);
        if (!Directory.Exists(fullPath))
        {
            try
            {
                Directory.CreateDirectory(fullPath);
                _logger.LogDebug("Created session directory: {Directory}", fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create session directory: {Directory}", fullPath);
                throw new DirectoryNotFoundException($"Could not create session directory: {fullPath}");
            }
        }

        var session = new Session(
            Name: name,
            Directory: fullPath,
            StorageAccountName: storageAccountName,
            SubscriptionId: subscriptionId,
            CreatedAt: DateTime.UtcNow,
            LastAccessedAt: DateTime.UtcNow);

        _sessions[name] = session;
        await SaveSessionsAsync(cancellationToken);

        _logger.LogInformation("Session created successfully: {SessionName}", name);
        return session;
    }

    /// <inheritdoc/>
    public Session? GetSession(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _sessions.GetValueOrDefault(name);
    }

    /// <inheritdoc/>
    public IEnumerable<Session> GetAllSessions()
    {
        return _sessions.Values.ToList();
    }

    /// <inheritdoc/>
    public async Task<Session> UpdateSessionAsync(Session session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!_sessions.ContainsKey(session.Name))
            throw new InvalidOperationException($"Session '{session.Name}' does not exist");

        var updatedSession = session with { LastAccessedAt = DateTime.UtcNow };
        _sessions[session.Name] = updatedSession;
        await SaveSessionsAsync(cancellationToken);

        _logger.LogDebug("Session updated: {SessionName}", session.Name);

        return updatedSession;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteSessionAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!_sessions.Remove(name))
            return false;

        if (_activeSession?.Name == name)
            _activeSession = null;

        await SaveSessionsAsync(cancellationToken);
        _logger.LogInformation("Session deleted: {SessionName}", name);

        return true;
    }

    /// <inheritdoc/>
    public void SetActiveSession(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        _activeSession = session;
        _logger.LogInformation("Active session set to: {SessionName}", session.Name);
    }

    /// <inheritdoc/>
    public Session? GetActiveSession() => _activeSession;

    /// <inheritdoc/>
    public void ClearActiveSession()
    {
        var previousSession = _activeSession?.Name;
        _activeSession = null;

        if (previousSession != null)
        {
            _logger.LogInformation("Active session cleared (was: {SessionName})", previousSession);
        }
    }

    /// <inheritdoc/>
    public async Task<Session?> TouchSessionAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!_sessions.TryGetValue(name, out var session))
            return null;

        var updatedSession = session with { LastAccessedAt = DateTime.UtcNow };
        _sessions[name] = updatedSession;
        await SaveSessionsAsync(cancellationToken);

        return updatedSession;
    }

    /// <inheritdoc/>
    public bool ValidateSessionDirectory(Session session, bool createIfMissing = false)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            if (Directory.Exists(session.Directory))
                return true;

            if (createIfMissing)
            {
                Directory.CreateDirectory(session.Directory);
                _logger.LogDebug("Created missing session directory: {Directory}", session.Directory);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate session directory: {Directory}", session.Directory);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task SaveSessionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(_sessions.Values, JsonOptions);
            await File.WriteAllTextAsync(_sessionFilePath, json, cancellationToken);
            _logger.LogDebug("Sessions saved to: {FilePath}", _sessionFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save sessions to: {FilePath}", _sessionFilePath);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task LoadSessionsAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_sessionFilePath))
        {
            _logger.LogDebug("No session file found, starting with empty session collection");
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_sessionFilePath, cancellationToken);
            var sessions = JsonSerializer.Deserialize<List<Session>>(json);

            _sessions.Clear();
            if (sessions != null)
            {
                foreach (var session in sessions)
                {
                    _sessions[session.Name] = session;
                }
            }

            _logger.LogInformation("Loaded {SessionCount} sessions from: {FilePath}", _sessions.Count, _sessionFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load sessions from: {FilePath}", _sessionFilePath);
            throw;
        }
    }
}