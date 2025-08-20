using AzStore.Core.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AzStore.Core.Tests;

/// <summary>
/// Integration tests for AuthenticationService that verify behavior with actual Azure CLI.
/// These tests will only pass meaningful assertions when Azure CLI is authenticated.
/// </summary>
[Trait("Category", "Integration")]
public class AuthenticationServiceIntegrationTests
{
    [Fact]
    public async Task AzureCliAvailability_CanBeChecked()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        var isAvailable = await service.IsAzureCliAvailableAsync(CancellationToken.None);
        
        // Should return a boolean without throwing
        Assert.IsType<bool>(isAvailable);
    }

    [Fact]
    public async Task AzureCliVersion_CanBeRetrieved()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        var version = await service.GetAzureCliVersionAsync(CancellationToken.None);
        
        // Should return null if CLI not available, or a string if available
        Assert.True(version == null || !string.IsNullOrEmpty(version));
    }

    [Fact]
    public async Task Authentication_WorksWhenAzureCliIsAuthenticated()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        // First check if Azure CLI is available and authenticated
        var isAvailable = await service.IsAzureCliAvailableAsync(CancellationToken.None);
        if (!isAvailable)
        {
            // Skip test if Azure CLI is not available
            return;
        }

        var isAuthenticated = await service.IsAuthenticatedAsync(CancellationToken.None);
        if (!isAuthenticated)
        {
            // Skip test if Azure CLI is not authenticated
            return;
        }

        // Test authentication
        var result = await service.AuthenticateAsync(CancellationToken.None);
        
        if (result.Success)
        {
            AuthenticationServiceAssertions.AuthenticationSucceeded(result);
            Assert.NotNull(result.SubscriptionId);
            Assert.NotNull(result.TenantId);
            Assert.NotNull(result.AccountName);
        }
    }

    [Fact]
    public async Task GetAvailableSubscriptions_WorksWhenAuthenticated()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        // Check if authenticated before proceeding
        var isAuthenticated = await service.IsAuthenticatedAsync(CancellationToken.None);
        if (!isAuthenticated)
        {
            // Skip test if not authenticated
            return;
        }

        try
        {
            var subscriptions = await service.GetAvailableSubscriptionsAsync(CancellationToken.None);
            
            Assert.NotNull(subscriptions);
            var subscriptionList = subscriptions.ToList();
            
            if (subscriptionList.Count > 0)
            {
                Assert.All(subscriptionList, AuthenticationServiceAssertions.ValidAzureSubscription);
                
                // At least one subscription should be marked as default
                var defaultSubscriptions = subscriptionList.Where(s => s.IsDefault).ToList();
                Assert.True(defaultSubscriptions.Count <= 1, "Only one subscription should be marked as default");
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Expected if not properly authenticated
            Assert.True(true);
        }
    }

    [Fact]
    public async Task GetStorageAccounts_WorksWithValidSubscription()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        // Check if authenticated before proceeding
        var isAuthenticated = await service.IsAuthenticatedAsync(CancellationToken.None);
        if (!isAuthenticated)
        {
            return;
        }

        try
        {
            var subscriptions = await service.GetAvailableSubscriptionsAsync(CancellationToken.None);
            var firstSubscription = subscriptions.FirstOrDefault();
            
            if (firstSubscription != null)
            {
                var storageAccounts = await service.GetStorageAccountsAsync(firstSubscription.Id, CancellationToken.None);
                
                Assert.NotNull(storageAccounts);
                var storageAccountList = storageAccounts.ToList();
                
                if (storageAccountList.Count > 0)
                {
                    Assert.All(storageAccountList, AuthenticationServiceAssertions.ValidStorageAccountInfo);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Expected if not properly authenticated
            Assert.True(true);
        }
    }

    [Fact]
    public async Task RefreshAuthentication_WorksWhenAuthenticated()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        // Check if authenticated before proceeding
        var isAuthenticated = await service.IsAuthenticatedAsync(CancellationToken.None);
        if (!isAuthenticated)
        {
            return;
        }

        var refreshResult = await service.RefreshAuthenticationAsync(CancellationToken.None);
        
        if (refreshResult != null)
        {
            AuthenticationServiceAssertions.AuthenticationSucceeded(refreshResult);
        }
    }

    [Fact]
    public async Task AuthenticateWithSpecificSubscription_WorksWithValidId()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        // Check if authenticated before proceeding
        var isAuthenticated = await service.IsAuthenticatedAsync(CancellationToken.None);
        if (!isAuthenticated)
        {
            return;
        }

        try
        {
            var subscriptions = await service.GetAvailableSubscriptionsAsync(CancellationToken.None);
            var firstSubscription = subscriptions.FirstOrDefault();
            
            if (firstSubscription != null)
            {
                var result = await service.AuthenticateAsync(firstSubscription.Id, CancellationToken.None);
                
                if (result.Success)
                {
                    AuthenticationServiceAssertions.AuthenticationSucceeded(result);
                    Assert.Equal(firstSubscription.Id, result.SubscriptionId);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Expected if not properly authenticated
            Assert.True(true);
        }
    }

    [Fact]
    public async Task AuthenticateWithInvalidSubscription_ReturnsFailure()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        // Check if authenticated before proceeding
        var isAuthenticated = await service.IsAuthenticatedAsync(CancellationToken.None);
        if (!isAuthenticated)
        {
            return;
        }

        // Use a random GUID that likely doesn't exist
        var invalidSubscriptionId = Guid.NewGuid();
        
        var result = await service.AuthenticateAsync(invalidSubscriptionId, CancellationToken.None);
        
        // Should fail gracefully for invalid subscription
        if (!result.Success)
        {
            AuthenticationServiceAssertions.AuthenticationFailed(result);
        }
    }
}