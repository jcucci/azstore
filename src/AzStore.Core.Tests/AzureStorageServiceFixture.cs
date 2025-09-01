using AzStore.Core;
using AzStore.Core.Services.Abstractions;
using AzStore.Core.Services.Implementations;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AzStore.Core.Tests;

public static class AzureStorageServiceFixture
{
    public static AzureStorageService CreateWithMockDependencies()
    {
        var logger = Substitute.For<ILogger<AzureStorageService>>();
        var authService = Substitute.For<IAuthenticationService>();
        var pathService = Substitute.For<IPathService>();
        var sessionManager = Substitute.For<ISessionManager>();
        return new AzureStorageService(logger, authService, pathService, sessionManager);
    }

    public static AzureStorageService CreateWithMockLogger()
    {
        return CreateWithMockDependencies();
    }
}