using AzStore.Core.Models.Authentication;
using AzStore.Core.Services.Implementations;
using Xunit;

namespace AzStore.Core.Tests;

public static class AuthenticationServiceAssertions
{
    public static void InstanceCreated(AuthenticationService service)
    {
        Assert.NotNull(service);
    }

    public static void AuthenticationFailed(AuthenticationResult result)
    {
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.NotEmpty(result.Error);
    }

    public static void AuthenticationSucceeded(AuthenticationResult result)
    {
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Null(result.Error);
        Assert.NotNull(result.AccessToken);
    }

    public static void ValidAzureSubscription(AzureSubscription subscription)
    {
        Assert.NotNull(subscription);
        Assert.NotEqual(Guid.Empty, subscription.Id);
        Assert.NotNull(subscription.Name);
        Assert.NotEmpty(subscription.Name);
        Assert.NotNull(subscription.State);
        Assert.NotEqual(Guid.Empty, subscription.TenantId);
    }

    public static void ValidStorageAccountInfo(StorageAccountInfo storageAccount)
    {
        Assert.NotNull(storageAccount);
        Assert.NotNull(storageAccount.AccountName);
        Assert.NotEmpty(storageAccount.AccountName);
        Assert.NotNull(storageAccount.PrimaryEndpoint);
    }
}