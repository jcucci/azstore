using AzStore.Core.Models.Session;
using Xunit;

namespace AzStore.Core.Tests.Models;

[Trait("Category", "Unit")]
public class SessionTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldReturnSession()
    {
        var name = "test-session";
        var directory = "/home/user/downloads";
        var storageAccountName = "mystorage";
        var subscriptionId = Guid.NewGuid();

        var session = Session.Create(name, directory, storageAccountName, subscriptionId);

        Assert.Equal(name, session.Name);
        Assert.Equal(directory, session.Directory);
        Assert.Equal(storageAccountName, session.StorageAccountName);
        Assert.Equal(subscriptionId, session.SubscriptionId);
        Assert.True(Math.Abs((DateTime.UtcNow - session.CreatedAt).TotalSeconds) < 1);
        Assert.True(Math.Abs((DateTime.UtcNow - session.LastAccessedAt).TotalSeconds) < 1);
    }

    [Fact]
    public void Touch_ShouldUpdateLastAccessedAt()
    {
        var originalSession = Session.Create("test", "/path", "storage", Guid.NewGuid());
        var originalLastAccessed = originalSession.LastAccessedAt;

        Thread.Sleep(10); // Ensure time difference

        var touchedSession = originalSession.Touch();

        Assert.True(touchedSession.LastAccessedAt > originalLastAccessed);
        Assert.Equal(originalSession.Name, touchedSession.Name);
        Assert.Equal(originalSession.Directory, touchedSession.Directory);
        Assert.Equal(originalSession.StorageAccountName, touchedSession.StorageAccountName);
        Assert.Equal(originalSession.SubscriptionId, touchedSession.SubscriptionId);
        Assert.Equal(originalSession.CreatedAt, touchedSession.CreatedAt);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        var session = Session.Create("my-session", "/downloads", "mystorage", Guid.NewGuid());

        var result = session.ToString();

        Assert.Equal("Session 'my-session' -> mystorage (/downloads)", result);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateSession()
    {
        var name = "test";
        var directory = "/path";
        var storageAccountName = "storage";
        var subscriptionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var session = new Session(name, directory, storageAccountName, subscriptionId, now, now);

        Assert.Equal(name, session.Name);
        Assert.Equal(directory, session.Directory);
        Assert.Equal(storageAccountName, session.StorageAccountName);
        Assert.Equal(subscriptionId, session.SubscriptionId);
        Assert.Equal(now, session.CreatedAt);
        Assert.Equal(now, session.LastAccessedAt);
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        var subscriptionId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var lastAccessedAt = DateTime.UtcNow;

        var session1 = new Session("test", "/path", "storage", subscriptionId, createdAt, lastAccessedAt);
        var session2 = new Session("test", "/path", "storage", subscriptionId, createdAt, lastAccessedAt);

        Assert.Equal(session1, session2);
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        var subscriptionId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var lastAccessedAt = DateTime.UtcNow;

        var session1 = new Session("test1", "/path", "storage", subscriptionId, createdAt, lastAccessedAt);
        var session2 = new Session("test2", "/path", "storage", subscriptionId, createdAt, lastAccessedAt);

        Assert.NotEqual(session1, session2);
    }
}