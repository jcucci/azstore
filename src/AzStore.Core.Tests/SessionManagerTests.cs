using AzStore.Core.Models.Session;
using AzStore.Core.Services.Implementations;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AzStore.Core.Tests;

[Trait("Category", "Unit")]
public class SessionManagerTests : IDisposable
{
    private readonly ILogger<SessionManager> _logger;
    private readonly string _tempDirectory;
    private readonly SessionManager _sessionManager;

    public SessionManagerTests()
    {
        _logger = Substitute.For<ILogger<SessionManager>>();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "azstore-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);

        // Override the sessions file path to use our temp directory
        Environment.SetEnvironmentVariable("APPDATA", _tempDirectory);
        Environment.SetEnvironmentVariable("HOME", _tempDirectory);

        _sessionManager = new SessionManager(_logger);
    }

    [Fact]
    public async Task CreateSessionAsync_WithValidParameters_CreatesSession()
    {
        // Arrange
        var name = "test-session";
        var directory = Path.Combine(_tempDirectory, "session-dir");
        var storageAccount = "mystorageaccount";
        var subscriptionId = Guid.NewGuid();

        // Act
        var result = await _sessionManager.CreateSessionAsync(name, directory, storageAccount, subscriptionId);

        // Assert
        Assert.Equal(name, result.Name);
        Assert.Equal(Path.GetFullPath(directory), result.Directory);
        Assert.Equal(storageAccount, result.StorageAccountName);
        Assert.Equal(subscriptionId, result.SubscriptionId);
        Assert.True(Directory.Exists(result.Directory));
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
        Assert.True(result.LastAccessedAt <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData("", "dir", "account")]
    [InlineData(" ", "dir", "account")]
    public async Task CreateSessionAsync_WithInvalidName_ThrowsArgumentException(string? name, string directory, string storageAccount)
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sessionManager.CreateSessionAsync(name!, directory, storageAccount, subscriptionId));
    }

    [Fact]
    public async Task CreateSessionAsync_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sessionManager.CreateSessionAsync(null!, "dir", "account", subscriptionId));
    }

    [Theory]
    [InlineData("name", "", "account")]
    [InlineData("name", " ", "account")]
    public async Task CreateSessionAsync_WithInvalidDirectory_ThrowsArgumentException(string name, string? directory, string storageAccount)
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sessionManager.CreateSessionAsync(name, directory!, storageAccount, subscriptionId));
    }

    [Fact]
    public async Task CreateSessionAsync_WithNullDirectory_ThrowsArgumentNullException()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sessionManager.CreateSessionAsync("name", null!, "account", subscriptionId));
    }

    [Theory]
    [InlineData("name", "dir", "")]
    [InlineData("name", "dir", " ")]
    public async Task CreateSessionAsync_WithInvalidStorageAccount_ThrowsArgumentException(string name, string directory, string? storageAccount)
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sessionManager.CreateSessionAsync(name, directory, storageAccount!, subscriptionId));
    }

    [Fact]
    public async Task CreateSessionAsync_WithNullStorageAccount_ThrowsArgumentNullException()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sessionManager.CreateSessionAsync("name", "dir", null!, subscriptionId));
    }

    [Fact]
    public async Task CreateSessionAsync_WithExistingSessionName_ThrowsInvalidOperationException()
    {
        // Arrange
        var name = "duplicate-session";
        var directory1 = Path.Combine(_tempDirectory, "dir1");
        var directory2 = Path.Combine(_tempDirectory, "dir2");
        var storageAccount = "mystorageaccount";
        var subscriptionId = Guid.NewGuid();

        await _sessionManager.CreateSessionAsync(name, directory1, storageAccount, subscriptionId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sessionManager.CreateSessionAsync(name, directory2, storageAccount, subscriptionId));

        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task GetSession_WithExistingSession_ReturnsSession()
    {
        // Arrange
        var name = "test-session";
        var directory = Path.Combine(_tempDirectory, "session-dir");
        var storageAccount = "mystorageaccount";
        var subscriptionId = Guid.NewGuid();

        var session = await _sessionManager.CreateSessionAsync(name, directory, storageAccount, subscriptionId);

        // Act
        var result = _sessionManager.GetSession(name);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(session.Name, result.Name);
        Assert.Equal(session.Directory, result.Directory);
    }

    [Fact]
    public void GetSession_WithNonExistentSession_ReturnsNull()
    {
        // Act
        var result = _sessionManager.GetSession("non-existent");

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void GetSession_WithInvalidName_ThrowsArgumentException(string? name)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _sessionManager.GetSession(name!));
    }

    [Fact]
    public void GetSession_WithNullName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _sessionManager.GetSession(null!));
    }

    [Fact]
    public void GetAllSessions_WithNoSessions_ReturnsEmptyCollection()
    {
        // Act
        var result = _sessionManager.GetAllSessions();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllSessions_WithMultipleSessions_ReturnsAllSessions()
    {
        // Arrange
        var session1 = await _sessionManager.CreateSessionAsync("session1", Path.Combine(_tempDirectory, "dir1"), "account1", Guid.NewGuid());
        var session2 = await _sessionManager.CreateSessionAsync("session2", Path.Combine(_tempDirectory, "dir2"), "account2", Guid.NewGuid());

        // Act
        var result = _sessionManager.GetAllSessions().ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, s => s.Name == session1.Name);
        Assert.Contains(result, s => s.Name == session2.Name);
    }

    [Fact]
    public async Task UpdateSessionAsync_WithValidSession_UpdatesAndPersists()
    {
        // Arrange
        var name = "test-session";
        var directory = Path.Combine(_tempDirectory, "session-dir");
        var storageAccount = "mystorageaccount";
        var subscriptionId = Guid.NewGuid();

        var originalSession = await _sessionManager.CreateSessionAsync(name, directory, storageAccount, subscriptionId);
        var modifiedSession = originalSession with { StorageAccountName = "newstorageaccount" };

        // Act
        var result = await _sessionManager.UpdateSessionAsync(modifiedSession);

        // Assert
        Assert.Equal("newstorageaccount", result.StorageAccountName);
        Assert.True(result.LastAccessedAt > originalSession.LastAccessedAt);

        var retrievedSession = _sessionManager.GetSession(name);
        Assert.Equal("newstorageaccount", retrievedSession!.StorageAccountName);
    }

    [Fact]
    public async Task UpdateSessionAsync_WithNonExistentSession_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = new Session("non-existent", "/path", "account", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sessionManager.UpdateSessionAsync(session));

        Assert.Contains("does not exist", exception.Message);
    }

    [Fact]
    public async Task UpdateSessionAsync_WithNullSession_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sessionManager.UpdateSessionAsync(null!));
    }

    [Fact]
    public async Task DeleteSessionAsync_WithExistingSession_DeletesAndReturnsTrue()
    {
        // Arrange
        var name = "test-session";
        var directory = Path.Combine(_tempDirectory, "session-dir");
        var storageAccount = "mystorageaccount";
        var subscriptionId = Guid.NewGuid();

        await _sessionManager.CreateSessionAsync(name, directory, storageAccount, subscriptionId);

        // Act
        var result = await _sessionManager.DeleteSessionAsync(name);

        // Assert
        Assert.True(result);
        Assert.Null(_sessionManager.GetSession(name));
    }

    [Fact]
    public async Task DeleteSessionAsync_WithNonExistentSession_ReturnsFalse()
    {
        // Act
        var result = await _sessionManager.DeleteSessionAsync("non-existent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteSessionAsync_WithActiveSession_ClearsActiveSession()
    {
        // Arrange
        var name = "active-session";
        var directory = Path.Combine(_tempDirectory, "session-dir");
        var storageAccount = "mystorageaccount";
        var subscriptionId = Guid.NewGuid();

        var session = await _sessionManager.CreateSessionAsync(name, directory, storageAccount, subscriptionId);
        _sessionManager.SetActiveSession(session);

        // Act
        var result = await _sessionManager.DeleteSessionAsync(name);

        // Assert
        Assert.True(result);
        Assert.Null(_sessionManager.GetActiveSession());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task DeleteSessionAsync_WithInvalidName_ThrowsArgumentException(string? name)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sessionManager.DeleteSessionAsync(name!));
    }

    [Fact]
    public async Task DeleteSessionAsync_WithNullName_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sessionManager.DeleteSessionAsync(null!));
    }

    [Fact]
    public async Task SetActiveSession_WithValidSession_SetsActiveSession()
    {
        // Arrange
        var name = "test-session";
        var directory = Path.Combine(_tempDirectory, "session-dir");
        var storageAccount = "mystorageaccount";
        var subscriptionId = Guid.NewGuid();

        var session = await _sessionManager.CreateSessionAsync(name, directory, storageAccount, subscriptionId);

        // Act
        _sessionManager.SetActiveSession(session);

        // Assert
        var activeSession = _sessionManager.GetActiveSession();
        Assert.NotNull(activeSession);
        Assert.Equal(session.Name, activeSession.Name);
    }

    [Fact]
    public void SetActiveSession_WithNullSession_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _sessionManager.SetActiveSession(null!));
    }

    [Fact]
    public void GetActiveSession_WithNoActiveSession_ReturnsNull()
    {
        // Act
        var result = _sessionManager.GetActiveSession();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ClearActiveSession_WithActiveSession_ClearsActiveSession()
    {
        // Arrange
        var name = "test-session";
        var directory = Path.Combine(_tempDirectory, "session-dir");
        var storageAccount = "mystorageaccount";
        var subscriptionId = Guid.NewGuid();

        var session = await _sessionManager.CreateSessionAsync(name, directory, storageAccount, subscriptionId);
        _sessionManager.SetActiveSession(session);

        // Act
        _sessionManager.ClearActiveSession();

        // Assert
        Assert.Null(_sessionManager.GetActiveSession());
    }

    [Fact]
    public async Task TouchSessionAsync_WithExistingSession_UpdatesLastAccessedTime()
    {
        // Arrange
        var name = "test-session";
        var directory = Path.Combine(_tempDirectory, "session-dir");
        var storageAccount = "mystorageaccount";
        var subscriptionId = Guid.NewGuid();

        var originalSession = await _sessionManager.CreateSessionAsync(name, directory, storageAccount, subscriptionId);
        await Task.Delay(10); // Ensure time difference

        // Act
        var result = await _sessionManager.TouchSessionAsync(name);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.LastAccessedAt > originalSession.LastAccessedAt);
    }

    [Fact]
    public async Task TouchSessionAsync_WithNonExistentSession_ReturnsNull()
    {
        // Act
        var result = await _sessionManager.TouchSessionAsync("non-existent");

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task TouchSessionAsync_WithInvalidName_ThrowsArgumentException(string? name)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sessionManager.TouchSessionAsync(name!));
    }

    [Fact]
    public async Task TouchSessionAsync_WithNullName_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sessionManager.TouchSessionAsync(null!));
    }

    [Fact]
    public async Task ValidateSessionDirectory_WithExistingDirectory_ReturnsTrue()
    {
        // Arrange
        var name = "test-session";
        var directory = Path.Combine(_tempDirectory, "session-dir");
        var storageAccount = "mystorageaccount";
        var subscriptionId = Guid.NewGuid();

        var session = await _sessionManager.CreateSessionAsync(name, directory, storageAccount, subscriptionId);

        // Act
        var result = _sessionManager.ValidateSessionDirectory(session);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateSessionDirectory_WithMissingDirectoryAndCreateFalse_ReturnsFalse()
    {
        // Arrange
        var missingDirectory = Path.Combine(_tempDirectory, "missing-dir");
        var session = new Session("test", missingDirectory, "account", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow);

        // Act
        var result = _sessionManager.ValidateSessionDirectory(session, createIfMissing: false);

        // Assert
        Assert.False(result);
        Assert.False(Directory.Exists(missingDirectory));
    }

    [Fact]
    public void ValidateSessionDirectory_WithMissingDirectoryAndCreateTrue_CreatesDirAndReturnsTrue()
    {
        // Arrange
        var missingDirectory = Path.Combine(_tempDirectory, "missing-dir");
        var session = new Session("test", missingDirectory, "account", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow);

        // Act
        var result = _sessionManager.ValidateSessionDirectory(session, createIfMissing: true);

        // Assert
        Assert.True(result);
        Assert.True(Directory.Exists(missingDirectory));
    }

    [Fact]
    public void ValidateSessionDirectory_WithNullSession_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _sessionManager.ValidateSessionDirectory(null!));
    }

    [Fact]
    public async Task SaveAndLoadSessionsAsync_PersistsSessionsCorrectly()
    {
        // Arrange
        var session1 = await _sessionManager.CreateSessionAsync("session1", Path.Combine(_tempDirectory, "dir1"), "account1", Guid.NewGuid());
        var session2 = await _sessionManager.CreateSessionAsync("session2", Path.Combine(_tempDirectory, "dir2"), "account2", Guid.NewGuid());

        // Create a new SessionManager instance to test loading
        var newSessionManager = new SessionManager(_logger);

        // Act
        await newSessionManager.LoadSessionsAsync();

        // Assert
        var loadedSessions = newSessionManager.GetAllSessions().ToList();
        Assert.Equal(2, loadedSessions.Count);

        var loadedSession1 = loadedSessions.FirstOrDefault(s => s.Name == "session1");
        var loadedSession2 = loadedSessions.FirstOrDefault(s => s.Name == "session2");

        Assert.NotNull(loadedSession1);
        Assert.NotNull(loadedSession2);
        Assert.Equal(session1.StorageAccountName, loadedSession1.StorageAccountName);
        Assert.Equal(session2.StorageAccountName, loadedSession2.StorageAccountName);
    }

    [Fact]
    public async Task LoadSessionsAsync_WithNoSessionFile_DoesNotThrow()
    {
        // Arrange
        var newTempDir = Path.Combine(Path.GetTempPath(), "azstore-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(newTempDir);

        Environment.SetEnvironmentVariable("APPDATA", newTempDir);
        Environment.SetEnvironmentVariable("HOME", newTempDir);

        var newSessionManager = new SessionManager(_logger);

        // Act & Assert
        await newSessionManager.LoadSessionsAsync(); // Should not throw

        var sessions = newSessionManager.GetAllSessions();
        Assert.Empty(sessions);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }
}