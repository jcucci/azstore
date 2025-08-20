using AzStore.CLI;
using Xunit;

namespace AzStore.CLI.Tests;

[Trait("Category", "Unit")]
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAzStoreServices_RegistersRequiredServices()
    {
        var services = ServiceCollectionExtensionsFixture.CreateServiceCollection();
        
        services.AddAzStoreServices();
        
        ServiceCollectionExtensionsAssertions.ServicesRegistered(services);
    }
}