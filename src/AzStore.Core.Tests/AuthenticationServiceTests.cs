using AzStore.Core.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AzStore.Core.Tests;

public class AuthenticationServiceTests
{
    [Fact]
    public void AuthenticationService_CanBeInstantiated()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        AuthenticationServiceAssertions.InstanceCreated(service);
    }

    [Fact]
    public void AuthenticationService_Constructor_ThrowsWhenLoggerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new AuthenticationService(null!));
    }

    [Fact]
    public async Task AuthenticateAsync_WithoutSubscription_ReturnsFailedWhenAzureCliNotAvailable()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        var result = await service.AuthenticateAsync(CancellationToken.None);
        
        // In test environment where Azure CLI may not be installed, this should fail gracefully
        AuthenticationServiceAssertions.AuthenticationFailed(result);
    }

    [Fact]
    public async Task AuthenticateAsync_WithSubscription_ThrowsWhenSubscriptionIdIsEmpty()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        await Assert.ThrowsAsync<ArgumentException>(() => 
            service.AuthenticateAsync(Guid.Empty, CancellationToken.None));
    }

    [Fact]
    public async Task AuthenticateAsync_WithSubscription_ReturnsFailedWhenAzureCliNotAvailable()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        var subscriptionId = Guid.NewGuid();
        
        var result = await service.AuthenticateAsync(subscriptionId, CancellationToken.None);
        
        // In test environment where Azure CLI may not be installed, this should fail gracefully
        AuthenticationServiceAssertions.AuthenticationFailed(result);
    }

    [Fact]
    public async Task IsAuthenticatedAsync_ReturnsFalseWhenNotAuthenticated()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        var isAuthenticated = await service.IsAuthenticatedAsync(CancellationToken.None);
        
        // Should return false when Azure CLI is not available/authenticated
        Assert.False(isAuthenticated);
    }

    [Fact]
    public async Task GetCurrentAuthenticationAsync_ReturnsNullWhenNotAuthenticated()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        var result = await service.GetCurrentAuthenticationAsync(CancellationToken.None);
        
        // Should return null when not authenticated
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAvailableSubscriptionsAsync_ThrowsUnauthorizedWhenNotAuthenticated()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        // Should throw UnauthorizedAccessException when Azure CLI is not authenticated
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            service.GetAvailableSubscriptionsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetStorageAccountsAsync_ThrowsWhenSubscriptionIdIsEmpty()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        await Assert.ThrowsAsync<ArgumentException>(() => 
            service.GetStorageAccountsAsync(Guid.Empty, CancellationToken.None));
    }

    [Fact]
    public async Task GetStorageAccountsAsync_ThrowsUnauthorizedWhenNotAuthenticated()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        var subscriptionId = Guid.NewGuid();
        
        // Should throw when not authenticated
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            service.GetStorageAccountsAsync(subscriptionId, CancellationToken.None));
        
        Assert.NotNull(exception.Message);
    }

    [Fact]
    public async Task RefreshAuthenticationAsync_ReturnsNullWhenRefreshFails()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        var result = await service.RefreshAuthenticationAsync(CancellationToken.None);
        
        // Should return null when refresh fails (Azure CLI not available)
        Assert.Null(result);
    }

    [Fact]
    public async Task ClearAuthenticationAsync_CompletesSuccessfully()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        // Should complete without throwing
        await service.ClearAuthenticationAsync(CancellationToken.None);
        
        Assert.True(true);
    }

    [Fact]
    public async Task IsAzureCliAvailableAsync_DoesNotThrow()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        // Should not throw regardless of Azure CLI availability
        var isAvailable = await service.IsAzureCliAvailableAsync(CancellationToken.None);
        
        // Result should be a boolean (true if CLI available, false otherwise)
        Assert.IsType<bool>(isAvailable);
    }

    [Fact]
    public async Task GetAzureCliVersionAsync_DoesNotThrow()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        // Should not throw regardless of Azure CLI availability
        var version = await service.GetAzureCliVersionAsync(CancellationToken.None);
        
        // Should return null if CLI not available, or a string if available
        Assert.True(version == null || !string.IsNullOrEmpty(version));
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        // Should dispose cleanly
        service.Dispose();
        
        Assert.True(true);
    }

    [Fact]
    public async Task Multiple_ClearAuthentication_CallsDoNotThrow()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        // Multiple clear calls should not throw
        await service.ClearAuthenticationAsync(CancellationToken.None);
        await service.ClearAuthenticationAsync(CancellationToken.None);
        
        Assert.True(true);
    }

    [Fact]
    public void Multiple_Dispose_CallsDoNotThrow()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        // Multiple dispose calls should not throw
        service.Dispose();
        service.Dispose();
        
        Assert.True(true);
    }

    [Fact]
    public async Task AuthenticateAsync_WithCancellation_HandlesGracefully()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        // Should handle cancellation gracefully and return a failed result
        var result = await service.AuthenticateAsync(cts.Token);
        
        AuthenticationServiceAssertions.AuthenticationFailed(result);
    }

    [Fact]
    public async Task AuthenticateAsync_WithSubscriptionAndCancellation_HandlesGracefully()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        var subscriptionId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        // Should handle cancellation gracefully and return a failed result
        var result = await service.AuthenticateAsync(subscriptionId, cts.Token);
        
        AuthenticationServiceAssertions.AuthenticationFailed(result);
    }

    [Fact]
    public async Task IsAuthenticatedAsync_WithCancellation_ReturnsFalse()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        // Should handle cancellation gracefully and return false
        var result = await service.IsAuthenticatedAsync(cts.Token);
        
        Assert.False(result);
    }

    [Fact]
    public async Task GetCurrentAuthenticationAsync_WithCancellation_ReturnsNull()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        // Should handle cancellation gracefully and return null
        var result = await service.GetCurrentAuthenticationAsync(cts.Token);
        
        Assert.Null(result);
    }

    [Fact]
    public async Task Concurrent_AuthenticateAsync_CallsHandledSafely()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        // Multiple concurrent authentication attempts should be handled safely
        var tasks = Enumerable.Range(0, 3)
            .Select(_ => service.AuthenticateAsync(CancellationToken.None))
            .ToArray();
            
        var results = await Task.WhenAll(tasks);
        
        // All calls should complete without throwing
        Assert.Equal(3, results.Length);
        Assert.All(results, result => Assert.NotNull(result));
    }

    [Fact]
    public async Task Concurrent_IsAuthenticatedAsync_CallsHandledSafely()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        // Multiple concurrent authentication checks should be handled safely
        var tasks = Enumerable.Range(0, 3)
            .Select(_ => service.IsAuthenticatedAsync(CancellationToken.None))
            .ToArray();
            
        var results = await Task.WhenAll(tasks);
        
        // All calls should complete without throwing
        Assert.Equal(3, results.Length);
        Assert.All(results, result => Assert.IsType<bool>(result));
    }

    [Fact]
    public async Task Concurrent_ClearAuthentication_CallsHandledSafely()
    {
        var service = AuthenticationServiceFixture.CreateWithMockLogger();
        
        // Multiple concurrent clear calls should be handled safely
        var tasks = Enumerable.Range(0, 3)
            .Select(_ => service.ClearAuthenticationAsync(CancellationToken.None))
            .ToArray();
            
        await Task.WhenAll(tasks);
        
        // All calls should complete without throwing
        Assert.True(true);
    }

    [Fact]
    public void AuthenticationResult_Successful_CreatesValidResult()
    {
        var accessToken = "test-token";
        var subscriptionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var accountName = "test@example.com";
        var expiresOn = DateTime.UtcNow.AddHours(1);

        var result = AuthenticationResult.Successful(accessToken, subscriptionId, tenantId, accountName, expiresOn);

        AuthenticationServiceAssertions.AuthenticationSucceeded(result);
        Assert.Equal(accessToken, result.AccessToken);
        Assert.Equal(subscriptionId, result.SubscriptionId);
        Assert.Equal(tenantId, result.TenantId);
        Assert.Equal(accountName, result.AccountName);
        Assert.Equal(expiresOn, result.ExpiresOn);
    }

    [Fact]
    public void AuthenticationResult_Failed_CreatesValidResult()
    {
        var errorMessage = "Authentication failed";

        var result = AuthenticationResult.Failed(errorMessage);

        AuthenticationServiceAssertions.AuthenticationFailed(result);
        Assert.Equal(errorMessage, result.Error);
        Assert.Null(result.AccessToken);
        Assert.Null(result.SubscriptionId);
        Assert.Null(result.TenantId);
        Assert.Null(result.AccountName);
        Assert.Null(result.ExpiresOn);
    }

    [Fact]
    public void AzureSubscription_ToString_ReturnsFormattedString()
    {
        var subscription = new AzureSubscription(
            Id: Guid.NewGuid(),
            Name: "Test Subscription",
            State: "Enabled",
            IsDefault: true,
            TenantId: Guid.NewGuid()
        );

        var result = subscription.ToString();

        Assert.Contains("Test Subscription", result);
        Assert.Contains(subscription.Id.ToString(), result);
        Assert.Contains("(default)", result);
    }

    [Fact]
    public void AzureSubscription_ToString_WithoutDefault_DoesNotIncludeDefaultIndicator()
    {
        var subscription = new AzureSubscription(
            Id: Guid.NewGuid(),
            Name: "Test Subscription",
            State: "Enabled",
            IsDefault: false,
            TenantId: Guid.NewGuid()
        );

        var result = subscription.ToString();

        Assert.Contains("Test Subscription", result);
        Assert.Contains(subscription.Id.ToString(), result);
        Assert.DoesNotContain("(default)", result);
    }
}