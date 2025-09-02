using AzStore.Core.Services.Abstractions;
using AzStore.Terminal.Commands;
using AzStore.Terminal.Utilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AzStore.Terminal.Tests;

public class ExitCommandTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_NoActiveDownloads_ExitsAndSavesSessions()
    {
        var logger = Substitute.For<ILogger<ExitCommand>>();
        var sessions = Substitute.For<ISessionManager>();
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        var activity = Substitute.For<IDownloadActivity>();
        activity.HasActiveDownloads.Returns(false);

        var cmd = new ExitCommand(logger, sessions, lifetime, activity);
        var result = await cmd.ExecuteAsync(Array.Empty<string>());

        Assert.True(result.ShouldExit);
        await sessions.Received(1).SaveSessionsAsync(Arg.Any<CancellationToken>());
        lifetime.Received(1).StopApplication();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_Force_SkipsPromptAndExits()
    {
        var logger = Substitute.For<ILogger<ExitCommand>>();
        var sessions = Substitute.For<ISessionManager>();
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        var activity = Substitute.For<IDownloadActivity>();
        activity.HasActiveDownloads.Returns(true);
        activity.ActiveCount.Returns(3);

        var cmd = new ExitCommand(logger, sessions, lifetime, activity);
        var result = await cmd.ExecuteAsync(new[] { "--force" });

        Assert.True(result.ShouldExit);
        await sessions.Received(1).SaveSessionsAsync(Arg.Any<CancellationToken>());
        lifetime.Received(1).StopApplication();
    }
}

