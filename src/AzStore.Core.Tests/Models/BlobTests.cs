using AzStore.Core.Models.Storage;
using Xunit;

namespace AzStore.Core.Tests.Models;

public class BlobTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldReturnBlob()
    {
        var name = "document.pdf";
        var path = "https://storage.blob.core.windows.net/container/document.pdf";
        var containerName = "documents";
        var blobType = BlobType.BlockBlob;
        var size = 1024L;

        var blob = Blob.Create(name, path, containerName, blobType, size);

        Assert.Equal(name, blob.Name);
        Assert.Equal(path, blob.Path);
        Assert.Equal(containerName, blob.ContainerName);
        Assert.Equal(blobType, blob.BlobType);
        Assert.Equal(size, blob.Size);
    }

    [Fact]
    public void GetExtension_WithExtension_ShouldReturnExtension()
    {
        var blob = Blob.Create("document.pdf", "path", "container");

        var extension = blob.GetExtension();

        Assert.Equal(".pdf", extension);
    }

    [Fact]
    public void GetExtension_WithoutExtension_ShouldReturnEmpty()
    {
        var blob = Blob.Create("document", "path", "container");

        var extension = blob.GetExtension();

        Assert.Equal("", extension);
    }

    [Theory]
    [InlineData(0, "0.0 B")]
    [InlineData(512, "512.0 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1.0 MB")]
    [InlineData(1073741824, "1.0 GB")]
    [InlineData(1099511627776, "1.0 TB")]
    public void GetFormattedSize_WithSize_ShouldReturnFormattedString(long size, string expected)
    {
        var blob = Blob.Create("test", "path", "container", size: size);

        var formatted = blob.GetFormattedSize();

        Assert.Equal(expected, formatted);
    }

    [Fact]
    public void GetFormattedSize_WithoutSize_ShouldReturnUnknown()
    {
        var blob = Blob.Create("test", "path", "container");

        var formatted = blob.GetFormattedSize();

        Assert.Equal("Unknown", formatted);
    }

    [Fact]
    public void ToString_WithSizeAndTier_ShouldReturnFormattedString()
    {
        var blob = new Blob
        {
            Name = "document.pdf",
            Path = "path",
            ContainerName = "container",
            BlobType = BlobType.BlockBlob,
            Size = 1024,
            AccessTier = BlobAccessTier.Hot
        };

        var result = blob.ToString();

        Assert.Equal("Blob: document.pdf (1.0 KB) [Hot]", result);
    }

    [Fact]
    public void ToString_WithoutSizeAndTier_ShouldReturnBasicString()
    {
        var blob = Blob.Create("document.pdf", "path", "container");

        var result = blob.ToString();

        Assert.Equal("Blob: document.pdf", result);
    }

    [Fact]
    public void Equals_WithSamePath_ShouldReturnTrue()
    {
        var blob1 = new Blob
        {
            Name = "test1.txt",
            Path = "/path/test.txt",
            ContainerName = "container",
            BlobType = BlobType.BlockBlob,
            ETag = "etag123"
        };

        var blob2 = new Blob
        {
            Name = "test2.txt", // Different name
            Path = "/path/test.txt", // Same path
            ContainerName = "container",
            BlobType = BlobType.BlockBlob,
            ETag = "etag123" // Same ETag
        };

        Assert.Equal(blob1, blob2);
    }

    [Fact]
    public void Equals_WithDifferentPath_ShouldReturnFalse()
    {
        var blob1 = new Blob
        {
            Name = "test.txt",
            Path = "/path1/test.txt",
            ContainerName = "container",
            BlobType = BlobType.BlockBlob,
            ETag = "etag123"
        };

        var blob2 = new Blob
        {
            Name = "test.txt",
            Path = "/path2/test.txt",
            ContainerName = "container",
            BlobType = BlobType.BlockBlob,
            ETag = "etag123"
        };

        Assert.NotEqual(blob1, blob2);
    }

    [Fact]
    public void GetHashCode_WithSamePathAndETag_ShouldReturnSameValue()
    {
        var blob1 = new Blob
        {
            Name = "test1.txt",
            Path = "/PATH/test.txt", // Case difference
            ContainerName = "container",
            BlobType = BlobType.BlockBlob,
            ETag = "etag123"
        };

        var blob2 = new Blob
        {
            Name = "test2.txt",
            Path = "/path/test.txt", // Case difference
            ContainerName = "container",
            BlobType = BlobType.BlockBlob,
            ETag = "etag123"
        };

        Assert.Equal(blob1.GetHashCode(), blob2.GetHashCode());
    }

    [Fact]
    public void Equals_WithSamePathAndBothNullETags_ShouldReturnTrue()
    {
        var blob1 = new Blob
        {
            Name = "test1.txt",
            Path = "/path/test.txt",
            ContainerName = "container",
            BlobType = BlobType.BlockBlob,
            ETag = null
        };

        var blob2 = new Blob
        {
            Name = "test2.txt",
            Path = "/path/test.txt",
            ContainerName = "container",
            BlobType = BlobType.BlockBlob,
            ETag = null
        };

        Assert.Equal(blob1, blob2);
    }

    [Fact]
    public void Equals_WithSamePathButOneNullETag_ShouldReturnFalse()
    {
        var blob1 = new Blob
        {
            Name = "test.txt",
            Path = "/path/test.txt",
            ContainerName = "container",
            BlobType = BlobType.BlockBlob,
            ETag = "etag123"
        };

        var blob2 = new Blob
        {
            Name = "test.txt",
            Path = "/path/test.txt",
            ContainerName = "container",
            BlobType = BlobType.BlockBlob,
            ETag = null
        };

        Assert.NotEqual(blob1, blob2);
    }

    [Fact]
    public void Equals_WithCaseInsensitivePath_ShouldReturnTrue()
    {
        var blob1 = new Blob
        {
            Name = "test.txt",
            Path = "/PATH/Test.txt",
            ContainerName = "container",
            BlobType = BlobType.BlockBlob,
            ETag = "etag123"
        };

        var blob2 = new Blob
        {
            Name = "test.txt",
            Path = "/path/test.txt",
            ContainerName = "container",
            BlobType = BlobType.BlockBlob,
            ETag = "etag123"
        };

        Assert.Equal(blob1, blob2);
    }

    [Fact]
    public void GetHashCode_WithBothNullETags_ShouldReturnSameValue()
    {
        var blob1 = new Blob
        {
            Name = "test1.txt",
            Path = "/PATH/test.txt",
            ContainerName = "container",
            BlobType = BlobType.BlockBlob,
            ETag = null
        };

        var blob2 = new Blob
        {
            Name = "test2.txt",
            Path = "/path/test.txt", // Case difference should still match
            ContainerName = "container",
            BlobType = BlobType.BlockBlob,
            ETag = null
        };

        Assert.Equal(blob1.GetHashCode(), blob2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentETags_ShouldReturnDifferentValues()
    {
        var blob1 = new Blob
        {
            Name = "test.txt",
            Path = "/path/test.txt",
            ContainerName = "container",
            BlobType = BlobType.BlockBlob,
            ETag = "etag123"
        };

        var blob2 = new Blob
        {
            Name = "test.txt",
            Path = "/path/test.txt",
            ContainerName = "container",
            BlobType = BlobType.BlockBlob,
            ETag = "etag456"
        };

        Assert.NotEqual(blob1.GetHashCode(), blob2.GetHashCode());
    }
}