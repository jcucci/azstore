using AzStore.Core;
using Xunit;

namespace AzStore.Core.Tests;

public static class AzureStorageServiceAssertions
{
    public static void InstanceCreated(AzureStorageService service)
    {
        Assert.NotNull(service);
    }

    public static void ConnectionStatusIsFalse(bool status)
    {
        Assert.False(status);
    }

    public static void ConnectionStatusIsTrue(bool status)
    {
        Assert.True(status);
    }
}