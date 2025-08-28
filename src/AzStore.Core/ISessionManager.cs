using AzStore.Core.Models;

namespace AzStore.Core;

/// <summary>
/// Provides session management functionality for Azure Blob Storage interactions.
/// </summary>
public interface ISessionManager
{
    /// <summary>
    /// Creates a new session with the specified parameters.
    /// </summary>
    /// <param name="name">The name of the session for user identification.</param>
    /// <param name="directory">The local directory path for downloading files in this session.</param>
    /// <param name="storageAccountName">The Azure Storage account name for this session.</param>
    /// <param name="subscriptionId">The Azure subscription ID associated with this session.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The newly created session.</returns>
    /// <exception cref="ArgumentException">Thrown when any required parameter is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a session with the same name already exists.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the specified directory does not exist and cannot be created.</exception>
    Task<Session> CreateSessionAsync(string name, string directory, string storageAccountName, Guid subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an existing session by name.
    /// </summary>
    /// <param name="name">The name of the session to retrieve.</param>
    /// <returns>The session with the specified name, or null if not found.</returns>
    /// <exception cref="ArgumentException">Thrown when name is null or empty.</exception>
    Session? GetSession(string name);

    /// <summary>
    /// Gets all existing sessions.
    /// </summary>
    /// <returns>A collection of all existing sessions.</returns>
    IEnumerable<Session> GetAllSessions();

    /// <summary>
    /// Updates an existing session with new information.
    /// </summary>
    /// <param name="session">The session to update.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated session.</returns>
    /// <exception cref="ArgumentNullException">Thrown when session is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the session does not exist.</exception>
    Task<Session> UpdateSessionAsync(Session session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an existing session by name.
    /// </summary>
    /// <param name="name">The name of the session to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>true if the session was deleted; false if it did not exist.</returns>
    /// <exception cref="ArgumentException">Thrown when name is null or empty.</exception>
    Task<bool> DeleteSessionAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the active session for the current application instance.
    /// </summary>
    /// <param name="session">The session to set as active.</param>
    /// <exception cref="ArgumentNullException">Thrown when session is null.</exception>
    void SetActiveSession(Session session);

    /// <summary>
    /// Gets the currently active session.
    /// </summary>
    /// <returns>The currently active session, or null if no session is active.</returns>
    Session? GetActiveSession();

    /// <summary>
    /// Clears the currently active session.
    /// </summary>
    void ClearActiveSession();

    /// <summary>
    /// Updates the last accessed timestamp for the specified session.
    /// </summary>
    /// <param name="name">The name of the session to touch.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated session, or null if the session was not found.</returns>
    /// <exception cref="ArgumentException">Thrown when name is null or empty.</exception>
    Task<Session?> TouchSessionAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the session's directory exists and is accessible.
    /// </summary>
    /// <param name="session">The session to validate.</param>
    /// <param name="createIfMissing">Whether to create the directory if it doesn't exist.</param>
    /// <returns>true if the directory is valid and accessible; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when session is null.</exception>
    bool ValidateSessionDirectory(Session session, bool createIfMissing = false);

    /// <summary>
    /// Persists session data to storage (typically called automatically by other methods).
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task SaveSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads session data from storage (typically called automatically during initialization).
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task LoadSessionsAsync(CancellationToken cancellationToken = default);
}