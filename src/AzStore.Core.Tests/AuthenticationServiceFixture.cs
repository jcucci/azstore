using AzStore.Core.Services.Implementations;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AzStore.Core.Tests;

public static class AuthenticationServiceFixture
{
    public static AuthenticationService CreateWithMockLogger()
    {
        var logger = Substitute.For<ILogger<AuthenticationService>>();
        return new AuthenticationService(logger);
    }
}