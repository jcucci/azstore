using Xunit;

namespace AzStore.Core.Tests;

public class BlobServiceTests
{
    [Fact]
    public void BlobService_CanBeInstantiated()
    {
        var service = BlobServiceFixture.CreateWithMockLogger();
        
        BlobServiceAssertions.InstanceCreated(service);
    }
}