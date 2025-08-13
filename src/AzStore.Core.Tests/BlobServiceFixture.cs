using AzStore.Core;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AzStore.Core.Tests;

public static class BlobServiceFixture
{
    public static BlobService CreateWithMockLogger()
    {
        var logger = Substitute.For<ILogger<BlobService>>();
        return new BlobService(logger);
    }
}