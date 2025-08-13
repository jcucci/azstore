using AzStore.Core;
using Xunit;

namespace AzStore.Core.Tests;

public static class BlobServiceAssertions
{
    public static void InstanceCreated(BlobService service)
    {
        Assert.NotNull(service);
    }
}