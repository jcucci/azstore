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

        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException($"Session name contains invalid characters: {name}", nameof(name));

        if (!IsValidStorageAccountName(storageAccountName))
            throw new ArgumentException($"Storage account name is invalid. Must be 3-24 characters, lowercase letters and numbers only: {storageAccountName}", nameof(storageAccountName));

        _logger.LogInformation("Creating session: {SessionName} for storage account: {StorageAccount}", name, storageAccountName);

        if (_sessions.ContainsKey(name))
            throw new InvalidOperationException($"Session '{name}' already exists");

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
    public IEnumerable<Session> GetAllSessions() => _sessions.Values;

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

    /// <inheritdoc/>
    public async Task<(int SessionsRemoved, int DirectoriesDeleted)> CleanupOldSessionsAsync(TimeSpan maxAge, bool deleteDirectories = false, CancellationToken cancellationToken = default)
    {
        if (maxAge < TimeSpan.Zero)
            throw new ArgumentException("Max age cannot be negative", nameof(maxAge));

        var cutoffDate = DateTime.UtcNow - maxAge;
        var sessionsToRemove = _sessions.Values
            .Where(s => s.LastAccessedAt < cutoffDate)
            .ToList();

        _logger.LogInformation("Cleaning up {SessionCount} sessions older than {MaxAge} days", sessionsToRemove.Count, maxAge.TotalDays);

        var directoriesDeleted = 0;
        var activeSessionName = _activeSession?.Name;

        foreach (var session in sessionsToRemove)
        {
            // Don't remove the currently active session
            if (session.Name == activeSessionName)
            {
                _logger.LogDebug("Skipping active session: {SessionName}", session.Name);
                continue;
            }

            if (deleteDirectories && Directory.Exists(session.Directory))
            {
                try
                {
                    Directory.Delete(session.Directory, recursive: true);
                    directoriesDeleted++;
                    _logger.LogDebug("Deleted session directory: {Directory}", session.Directory);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete session directory: {Directory}", session.Directory);
                }
            }

            _sessions.Remove(session.Name);
            _logger.LogDebug("Removed session: {SessionName}", session.Name);
        }

        if (sessionsToRemove.Count > 0)
        {
            await SaveSessionsAsync(cancellationToken);
        }

        _logger.LogInformation("Session cleanup completed: {SessionsRemoved} sessions removed, {DirectoriesDeleted} directories deleted",
            sessionsToRemove.Count, directoriesDeleted);

        return (sessionsToRemove.Count, directoriesDeleted);
    }

    /// <inheritdoc/>
    public SessionStatistics GetSessionStatistics()
    {
        if (!_sessions.Any())
            return SessionStatistics.Empty;

        var now = DateTime.UtcNow;
        var sevenDaysAgo = now.AddDays(-7);
        var thirtyDaysAgo = now.AddDays(-30);

        var sessions = _sessions.Values.ToList();
        var activeSessions = sessions.Count(s => s.LastAccessedAt >= sevenDaysAgo);
        var oldSessions = sessions.Count(s => s.LastAccessedAt < thirtyDaysAgo);

        var sessionAges = sessions.Select(s => (now - s.CreatedAt).TotalDays).ToList();
        var averageAge = sessionAges.Average();
        var oldestAge = sessionAges.Max();

        var storageAccountsCount = sessions.Select(s => s.StorageAccountName).Distinct().Count();

        return new SessionStatistics(
            TotalSessions: sessions.Count,
            ActiveSessions: activeSessions,
            OldSessions: oldSessions,
            AverageAge: averageAge,
            OldestSessionAge: oldestAge,
            StorageAccountsCount: storageAccountsCount);
    }

    /// <inheritdoc/>
    public async Task<int> ValidateAndCleanupSessionsAsync(bool fixDirectories = false, CancellationToken cancellationToken = default)
    {
        var invalidSessions = new List<string>();
        var activeSessionName = _activeSession?.Name;

        _logger.LogInformation("Validating {SessionCount} sessions", _sessions.Count);

        foreach (var session in _sessions.Values.ToList())
        {
            var isValid = true;

            // Don't validate the currently active session's directory (it might be temporarily unavailable)
            if (session.Name != activeSessionName)
            {
                if (!ValidateSessionDirectory(session, createIfMissing: fixDirectories))
                {
                    _logger.LogWarning("Session has invalid directory: {SessionName} -> {Directory}",
                        session.Name, session.Directory);
                    isValid = false;
                }
            }

            if (string.IsNullOrWhiteSpace(session.Name) || session.Name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                _logger.LogWarning("Session has invalid name: {SessionName}", session.Name);
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(session.StorageAccountName) || !IsValidStorageAccountName(session.StorageAccountName))
            {
                _logger.LogWarning("Session has invalid storage account name: {SessionName} -> {StorageAccount}", session.Name, session.StorageAccountName);
                isValid = false;
            }

            if (!isValid)
            {
                invalidSessions.Add(session.Name);
            }
        }

        // Remove invalid sessions
        foreach (var sessionName in invalidSessions)
        {
            _sessions.Remove(sessionName);
            _logger.LogInformation("Removed invalid session: {SessionName}", sessionName);

            // Clear active session if it was invalid
            if (_activeSession?.Name == sessionName)
            {
                _activeSession = null;
                _logger.LogInformation("Cleared active session due to validation failure");
            }
        }

        if (invalidSessions.Count > 0)
        {
            await SaveSessionsAsync(cancellationToken);
        }

        _logger.LogInformation("Session validation completed: {InvalidCount} invalid sessions removed", invalidSessions.Count);

        return invalidSessions.Count;
    }

    private static bool IsValidStorageAccountName(string name)
    {
        // Azure Storage account names must be 3-24 characters, lowercase letters and numbers only
        if (name.Length < 3 || name.Length > 24)
            return false;

        return name.All(c => char.IsLetter(c) && char.IsLower(c) || char.IsDigit(c));
    }
}