using AzStore.Core.Models;
using Xunit;

namespace AzStore.Core.Tests.Models;

public class ContainerTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldReturnContainer()
    {
        var name = "documents";
        var path = "https://storage.blob.core.windows.net/documents";
        var accessLevel = ContainerAccessLevel.Blob;

        var container = Container.Create(name, path, accessLevel);

        Assert.Equal(name, container.Name);
        Assert.Equal(path, container.Path);
        Assert.Equal(accessLevel, container.AccessLevel);
    }

    [Fact]
    public void Create_WithDefaultAccessLevel_ShouldReturnContainerWithNoneAccess()
    {
        var name = "private-docs";
        var path = "https://storage.blob.core.windows.net/private-docs";

        var container = Container.Create(name, path);

        Assert.Equal(ContainerAccessLevel.None, container.AccessLevel);
    }

    [Fact]
    public void ToString_WithAccessLevelAndBlobCount_ShouldReturnFormattedString()
    {
        var container = new Container
        {
            Name = "public-images",
            Path = "path",
            AccessLevel = ContainerAccessLevel.Container,
            BlobCount = 42
        };

        var result = container.ToString();

        Assert.Equal("Container: public-images [Container] (42 blobs)", result);
    }

    [Fact]
    public void ToString_WithoutAccessLevelAndBlobCount_ShouldReturnBasicString()
    {
        var container = new Container
        {
            Name = "private-docs",
            Path = "path",
            AccessLevel = ContainerAccessLevel.None
        };

        var result = container.ToString();

        Assert.Equal("Container: private-docs", result);
    }

    [Fact]
    public void ToString_WithAccessLevelOnly_ShouldIncludeAccessLevel()
    {
        var container = new Container
        {
            Name = "shared-files",
            Path = "path",
            AccessLevel = ContainerAccessLevel.Blob
        };

        var result = container.ToString();

        Assert.Equal("Container: shared-files [Blob]", result);
    }

    [Fact]
    public void ToString_WithBlobCountOnly_ShouldIncludeBlobCount()
    {
        var container = new Container
        {
            Name = "logs",
            Path = "path",
            AccessLevel = ContainerAccessLevel.None,
            BlobCount = 1000
        };

        var result = container.ToString();

        Assert.Equal("Container: logs (1000 blobs)", result);
    }

    [Fact]
    public void Equals_WithSamePath_ShouldReturnTrue()
    {
        var container1 = new Container
        {
            Name = "container1",
            Path = "/path/container",
            AccessLevel = ContainerAccessLevel.None,
            ETag = "etag123"
        };

        var container2 = new Container
        {
            Name = "container2", // Different name
            Path = "/path/container", // Same path
            AccessLevel = ContainerAccessLevel.Blob, // Different access level
            ETag = "etag123" // Same ETag
        };

        Assert.Equal(container1, container2);
    }

    [Fact]
    public void Equals_WithDifferentPath_ShouldReturnFalse()
    {
        var container1 = new Container
        {
            Name = "container",
            Path = "/path1/container",
            AccessLevel = ContainerAccessLevel.None,
            ETag = "etag123"
        };

        var container2 = new Container
        {
            Name = "container",
            Path = "/path2/container",
            AccessLevel = ContainerAccessLevel.None,
            ETag = "etag123"
        };

        Assert.NotEqual(container1, container2);
    }

    [Fact]
    public void GetHashCode_WithSamePathAndETag_ShouldReturnSameValue()
    {
        var container1 = new Container
        {
            Name = "container1",
            Path = "/PATH/container", // Case difference
            AccessLevel = ContainerAccessLevel.None,
            ETag = "etag123"
        };

        var container2 = new Container
        {
            Name = "container2",
            Path = "/path/container", // Case difference
            AccessLevel = ContainerAccessLevel.Blob,
            ETag = "etag123"
        };

        Assert.Equal(container1.GetHashCode(), container2.GetHashCode());
    }

    [Fact]
    public void Equals_WithSamePathAndBothNullETags_ShouldReturnTrue()
    {
        var container1 = new Container
        {
            Name = "container1",
            Path = "/path/container",
            AccessLevel = ContainerAccessLevel.None,
            ETag = null
        };

        var container2 = new Container
        {
            Name = "container2",
            Path = "/path/container",
            AccessLevel = ContainerAccessLevel.Blob,
            ETag = null
        };

        Assert.Equal(container1, container2);
    }

    [Fact]
    public void Equals_WithSamePathButOneNullETag_ShouldReturnFalse()
    {
        var container1 = new Container
        {
            Name = "container",
            Path = "/path/container",
            AccessLevel = ContainerAccessLevel.None,
            ETag = "etag123"
        };

        var container2 = new Container
        {
            Name = "container",
            Path = "/path/container",
            AccessLevel = ContainerAccessLevel.None,
            ETag = null
        };

        Assert.NotEqual(container1, container2);
    }

    [Fact]
    public void Equals_WithCaseInsensitivePath_ShouldReturnTrue()
    {
        var container1 = new Container
        {
            Name = "container",
            Path = "/PATH/Container",
            AccessLevel = ContainerAccessLevel.None,
            ETag = "etag123"
        };

        var container2 = new Container
        {
            Name = "container",
            Path = "/path/container",
            AccessLevel = ContainerAccessLevel.None,
            ETag = "etag123"
        };

        Assert.Equal(container1, container2);
    }

    [Fact]
    public void GetHashCode_WithBothNullETags_ShouldReturnSameValue()
    {
        var container1 = new Container
        {
            Name = "container1",
            Path = "/PATH/container",
            AccessLevel = ContainerAccessLevel.None,
            ETag = null
        };

        var container2 = new Container
        {
            Name = "container2",
            Path = "/path/container", // Case difference should still match
            AccessLevel = ContainerAccessLevel.Blob,
            ETag = null
        };

        Assert.Equal(container1.GetHashCode(), container2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentETags_ShouldReturnDifferentValues()
    {
        var container1 = new Container
        {
            Name = "container",
            Path = "/path/container",
            AccessLevel = ContainerAccessLevel.None,
            ETag = "etag123"
        };

        var container2 = new Container
        {
            Name = "container",
            Path = "/path/container",
            AccessLevel = ContainerAccessLevel.None,
            ETag = "etag456"
        };

        Assert.NotEqual(container1.GetHashCode(), container2.GetHashCode());
    }

    [Theory]
    [InlineData(ContainerAccessLevel.None)]
    [InlineData(ContainerAccessLevel.Blob)]
    [InlineData(ContainerAccessLevel.Container)]
    public void AccessLevel_AllValues_ShouldBeValid(ContainerAccessLevel accessLevel)
    {
        var container = new Container
        {
            Name = "test",
            Path = "path",
            AccessLevel = accessLevel
        };

        Assert.Equal(accessLevel, container.AccessLevel);
    }
}