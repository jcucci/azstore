using AzStore.Terminal.Commands;
using AzStore.Core.Models.Session;
using AzStore.Core.Services.Abstractions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AzStore.Terminal.Tests.Commands;

public class ListCommandTests
{
    [Fact]
    public void Name_ReturnsList()
    {
        var logger = Substitute.For<ILogger<ListCommand>>();
        var sessions = Substitute.For<ISessionManager>();
        var command = new ListCommand(logger, sessions);
        
        Assert.Equal("list", command.Name);
    }

    [Fact]
    public void Aliases_ContainsLs()
    {
        var logger = Substitute.For<ILogger<ListCommand>>();
        var sessions = Substitute.For<ISessionManager>();
        var command = new ListCommand(logger, sessions);
        
        Assert.Contains("ls", command.Aliases);
    }

    [Fact]
    public void Description_IsNotEmpty()
    {
        var logger = Substitute.For<ILogger<ListCommand>>();
        var sessions = Substitute.For<ISessionManager>();
        var command = new ListCommand(logger, sessions);
        
        Assert.False(string.IsNullOrWhiteSpace(command.Description));
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsSuccessResult()
    {
        var logger = Substitute.For<ILogger<ListCommand>>();
        var sessions = Substitute.For<ISessionManager>();
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var session = new Session("test", tempDir.FullName, "account123", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow);
            sessions.GetActiveSession().Returns(session);

            var command = new ListCommand(logger, sessions);
            var result = await command.ExecuteAsync(Array.Empty<string>());
        
            Assert.True(result.Success);
            Assert.False(result.ShouldExit);
            Assert.NotNull(result.Message);
        }
        finally
        {
            tempDir.Delete(true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_LogsDebugMessage()
    {
        var logger = Substitute.For<ILogger<ListCommand>>();
        var sessions = Substitute.For<ISessionManager>();
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var session = new Session("test", tempDir.FullName, "account123", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow);
            sessions.GetActiveSession().Returns(session);

            var command = new ListCommand(logger, sessions);
            await command.ExecuteAsync(Array.Empty<string>());
        
            logger.Received(1).LogDebug("User requested file list");
        }
        finally
        {
            tempDir.Delete(true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_NoActiveSession_ReturnsError()
    {
        var logger = Substitute.For<ILogger<ListCommand>>();
        var sessions = Substitute.For<ISessionManager>();
        sessions.GetActiveSession().Returns((Session?)null);

        var command = new ListCommand(logger, sessions);
        var result = await command.ExecuteAsync(Array.Empty<string>());

        Assert.False(result.Success);
        Assert.Contains("No active session", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_EmptySessionDirectory_ReturnsFriendlyMessage()
    {
        var logger = Substitute.For<ILogger<ListCommand>>();
        var sessions = Substitute.For<ISessionManager>();
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var session = new Session("mysession", tempDir.FullName, "account123", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow);
            sessions.GetActiveSession().Returns(session);

            var command = new ListCommand(logger, sessions);
            var result = await command.ExecuteAsync(Array.Empty<string>());

            Assert.True(result.Success);
            Assert.Contains("No files downloaded yet", result.Message);
        }
        finally
        {
            tempDir.Delete(true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ListsAndSortsByName_WithContainerAndGlob()
    {
        var logger = Substitute.For<ILogger<ListCommand>>();
        var sessions = Substitute.For<ISessionManager>();
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var session = new Session("sessionA", tempDir.FullName, "account123", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow);
            sessions.GetActiveSession().Returns(session);

            // Create session root and files
            var sessionRoot = Path.Combine(tempDir.FullName, "sessionA");
            Directory.CreateDirectory(sessionRoot);
            var cont1 = Path.Combine(sessionRoot, "docs");
            var cont2 = Path.Combine(sessionRoot, "media");
            Directory.CreateDirectory(cont1);
            Directory.CreateDirectory(cont2);

            var f1 = Path.Combine(cont1, "a.txt");
            var f2 = Path.Combine(cont1, "b.log");
            var f3 = Path.Combine(cont2, "clip.mp4");
            await File.WriteAllTextAsync(f1, new string('x', 10));
            await File.WriteAllTextAsync(f2, new string('y', 100));
            await File.WriteAllBytesAsync(f3, new byte[5]);

            File.SetLastWriteTime(f1, DateTime.Now.AddMinutes(-10));
            File.SetLastWriteTime(f2, DateTime.Now.AddMinutes(-5));
            File.SetLastWriteTime(f3, DateTime.Now.AddMinutes(-1));

            var command = new ListCommand(logger, sessions);

            // Container filter
            var resultDocs = await command.ExecuteAsync(["docs"], default);
            Assert.True(resultDocs.Success);
            Assert.Contains("docs" + Path.DirectorySeparatorChar + "a.txt", resultDocs.Message);
            Assert.Contains("docs" + Path.DirectorySeparatorChar + "b.log", resultDocs.Message);
            Assert.DoesNotContain("media", resultDocs.Message);

            // Glob filter
            var resultTxt = await command.ExecuteAsync(["*.txt"], default);
            Assert.True(resultTxt.Success);
            Assert.Contains("a.txt", resultTxt.Message);
            Assert.DoesNotContain("b.log", resultTxt.Message);
        }
        finally
        {
            tempDir.Delete(true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_SortsBySizeAndDate_WithDesc()
    {
        var logger = Substitute.For<ILogger<ListCommand>>();
        var sessions = Substitute.For<ISessionManager>();
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var session = new Session("sess", tempDir.FullName, "account123", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow);
            sessions.GetActiveSession().Returns(session);

            var sessionRoot = Path.Combine(tempDir.FullName, "sess");
            Directory.CreateDirectory(sessionRoot);
            var cont = Path.Combine(sessionRoot, "c1");
            Directory.CreateDirectory(cont);

            var fSmall = Path.Combine(cont, "small.bin");
            var fLarge = Path.Combine(cont, "large.bin");
            await File.WriteAllBytesAsync(fSmall, new byte[10]);
            await File.WriteAllBytesAsync(fLarge, new byte[1000]);
            File.SetLastWriteTime(fSmall, DateTime.Now.AddMinutes(-1));
            File.SetLastWriteTime(fLarge, DateTime.Now.AddMinutes(-5));

            var command = new ListCommand(logger, sessions);

            var bySizeDesc = await command.ExecuteAsync(["--sort", "size", "--desc"], default);
            Assert.True(bySizeDesc.Success);
            var firstLine = bySizeDesc.Message!.Split('\n')[0];
            Assert.Contains("large.bin", firstLine);

            var byDate = await command.ExecuteAsync(["--sort=date"], default);
            Assert.True(byDate.Success);
            // Ascending: older first
            var firstLineDate = byDate.Message!.Split('\n')[0];
            Assert.Contains("large.bin", firstLineDate);
        }
        finally
        {
            tempDir.Delete(true);
        }
    }
}
