using AzStore.Configuration;
using AzStore.Core.Models.Authentication;
using AzStore.Core.Models.Session;
using AzStore.Core.Services.Abstractions;
using AzStore.Terminal.Commands;
using AzStore.Terminal.Selection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace AzStore.Terminal.Tests;

public class SessionCommandCreateTests
{
    private static (SessionCommand Cmd, ISessionManager Sessions, IAuthenticationService Auth, IAccountSelectionService Picker)
        Create(
            IEnumerable<StorageAccountInfo> accounts,
            StorageAccountInfo? pickerSelection = null)
    {
        var sessions = Substitute.For<ISessionManager>();
        sessions.CreateSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(new Session(ci.ArgAt<string>(0), "/tmp", ci.ArgAt<string>(1), ci.ArgAt<Guid>(2), DateTime.UtcNow, DateTime.UtcNow)));

        var auth = Substitute.For<IAuthenticationService>();
        var subId = Guid.NewGuid();
        auth.GetCurrentAuthenticationAsync(Arg.Any<CancellationToken>())
            .Returns(AuthenticationResult.Successful("***", subscriptionId: subId));
        auth.GetStorageAccountsAsync(subId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(accounts));

        var logger = Substitute.For<ILogger<SessionCommand>>();
        var settings = Options.Create(new AzStoreSettings());

        var picker = Substitute.For<IAccountSelectionService>();
        picker.PickAsync(Arg.Any<IReadOnlyList<StorageAccountInfo>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(pickerSelection));

        var cmd = new SessionCommand(sessions, auth, logger, settings, picker);
        return (cmd, sessions, auth, picker);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task Create_NoAccounts_ReturnsError()
    {
        var (cmd, _, _, _) = Create(Array.Empty<StorageAccountInfo>());
        var result = await cmd.ExecuteAsync(["create", "mysess"], CancellationToken.None);
        Assert.False(result.Success);
        Assert.Contains("No storage accounts", result.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task Create_SingleAccount_AutoSelects()
    {
        var acc = new StorageAccountInfo("acct1", null, Guid.NewGuid(), "rg", new Uri("https://a/"));
        var (cmd, sessions, _, _) = Create(new[] { acc });
        var result = await cmd.ExecuteAsync(["create", "mysess"], CancellationToken.None);
        Assert.True(result.Success);
        await sessions.Received(1).CreateSessionAsync("mysess", "acct1", Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task Create_MultipleAccounts_PickerSelects_Proceeds()
    {
        var accs = new[]
        {
            new StorageAccountInfo("a1", null, Guid.NewGuid(), "rg1", new Uri("https://a/")),
            new StorageAccountInfo("a2", null, Guid.NewGuid(), "rg2", new Uri("https://b/")),
        };
        var (cmd, sessions, _, _) = Create(accs, accs[1]);
        var result = await cmd.ExecuteAsync(["create", "mysess"], CancellationToken.None);
        Assert.True(result.Success);
        await sessions.Received(1).CreateSessionAsync("mysess", "a2", Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task Create_MultipleAccounts_PickerCancelled_ReturnsOkWithNotice()
    {
        var accs = new[]
        {
            new StorageAccountInfo("a1", null, Guid.NewGuid(), "rg1", new Uri("https://a/")),
            new StorageAccountInfo("a2", null, Guid.NewGuid(), "rg2", new Uri("https://b/")),
        };
        var (cmd, sessions, _, _) = Create(accs, null);
        var result = await cmd.ExecuteAsync(["create", "mysess"], CancellationToken.None);
        Assert.True(result.Success);
        Assert.Contains("Selection cancelled", result.Message);
        await sessions.DidNotReceive().CreateSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
