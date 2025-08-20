using AzStore.Core;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AzStore.Core.Tests;

public static class AzureStorageServiceFixture
{
    public static AzureStorageService CreateWithMockDependencies()
    {
        var logger = Substitute.For<ILogger<AzureStorageService>>();
        var authService = Substitute.For<IAuthenticationService>();
        return new AzureStorageService(logger, authService);
    }

    public static AzureStorageService CreateWithMockLogger()
    {
        return CreateWithMockDependencies();
    }
}