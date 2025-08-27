using AzStore.Core.Exceptions;
using AzStore.Core.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AzStore.Core.Tests;

public class PathServiceTests
{
    private readonly ILogger<PathService> _mockLogger;
    private readonly PathService _pathService;

    public PathServiceTests()
    {
        _mockLogger = Substitute.For<ILogger<PathService>>();
        _pathService = new PathService(_mockLogger);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PathService(null!));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CalculateBlobDownloadPath_ValidInputs_ReturnsExpectedPath()
    {
        // Arrange
        var session = CreateTestSession("test-session", "/home/user/azstore");
        var containerName = "test-container";
        var blobName = "folder/file.txt";

        // Act
        var result = _pathService.CalculateBlobDownloadPath(session, containerName, blobName);

        // Assert
        var expectedPath = Path.Combine("/home/user/azstore", "test-session", "test-container", "folder", "file.txt");
        Assert.Equal(expectedPath, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CalculateBlobDownloadPath_NullSession_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _pathService.CalculateBlobDownloadPath(null!, "container", "blob"));
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CalculateBlobDownloadPath_InvalidContainerName_ThrowsArgumentException(string? containerName)
    {
        // Arrange
        var session = CreateTestSession("test", "/home/user");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _pathService.CalculateBlobDownloadPath(session, containerName!, "blob"));
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CalculateBlobDownloadPath_InvalidBlobName_ThrowsArgumentException(string? blobName)
    {
        // Arrange
        var session = CreateTestSession("test", "/home/user");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _pathService.CalculateBlobDownloadPath(session, "container", blobName!));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CalculateBlobDownloadPath_WithInvalidCharacters_SanitizesPath()
    {
        // Arrange
        var session = CreateTestSession("test|session", "/home/user/azstore");
        var containerName = "test*container";
        var blobName = "folder<with>invalid/file?.txt";

        // Act
        var result = _pathService.CalculateBlobDownloadPath(session, containerName, blobName);

        // Assert
        Assert.DoesNotContain("|", result);
        Assert.DoesNotContain("*", result);
        Assert.DoesNotContain("<", result);
        Assert.DoesNotContain(">", result);
        Assert.DoesNotContain("?", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CalculateContainerDirectoryPath_ValidInputs_ReturnsExpectedPath()
    {
        // Arrange
        var session = CreateTestSession("test-session", "/home/user/azstore");
        var containerName = "test-container";

        // Act
        var result = _pathService.CalculateContainerDirectoryPath(session, containerName);

        // Assert
        var expectedPath = Path.Combine("/home/user/azstore", "test-session", "test-container");
        Assert.Equal(expectedPath, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CalculateContainerDirectoryPath_NullSession_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _pathService.CalculateContainerDirectoryPath(null!, "container"));
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CalculateContainerDirectoryPath_InvalidContainerName_ThrowsArgumentException(string? containerName)
    {
        // Arrange
        var session = CreateTestSession("test", "/home/user");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _pathService.CalculateContainerDirectoryPath(session, containerName!));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void SanitizePathComponent_ValidInput_ReturnsSanitized()
    {
        // Arrange
        var input = "file|with*invalid<chars>";

        // Act
        var result = _pathService.SanitizePathComponent(input);

        // Assert
        Assert.DoesNotContain("|", result);
        Assert.DoesNotContain("*", result);
        Assert.DoesNotContain("<", result);
        Assert.DoesNotContain(">", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void SanitizePathComponent_NullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _pathService.SanitizePathComponent(null!));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task EnsureDirectoryExistsAsync_NullFilePath_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _pathService.EnsureDirectoryExistsAsync(null!));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task EnsureDirectoryExistsAsync_EmptyFilePath_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _pathService.EnsureDirectoryExistsAsync(""));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task EnsureDirectoryExistsAsync_FilePathWithoutDirectory_ReturnsTrue()
    {
        // Arrange
        var filePath = "file.txt"; // No directory component

        // Act
        var result = await _pathService.EnsureDirectoryExistsAsync(filePath);

        // Assert
        Assert.True(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task EnsureDirectoryExistsAsync_ExistingDirectory_ReturnsTrue()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var filePath = Path.Combine(tempDir, "file.txt");

        // Act
        var result = await _pathService.EnsureDirectoryExistsAsync(filePath);

        // Assert
        Assert.True(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task EnsureDirectoryExistsAsync_NewDirectory_CreatesDirectoryAndReturnsTrue()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var newDir = Path.Combine(tempDir, Guid.NewGuid().ToString());
        var filePath = Path.Combine(newDir, "file.txt");

        try
        {
            // Act
            var result = await _pathService.EnsureDirectoryExistsAsync(filePath);

            // Assert
            Assert.True(result);
            Assert.True(Directory.Exists(newDir));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(newDir))
            {
                Directory.Delete(newDir, true);
            }
        }
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task CleanupEmptyDirectoriesAsync_NullFilePath_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _pathService.CleanupEmptyDirectoriesAsync(null!, "/session"));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task CleanupEmptyDirectoriesAsync_NullSessionDirectory_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _pathService.CleanupEmptyDirectoriesAsync("/path/file.txt", null!));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task CleanupEmptyDirectoriesAsync_EmptyDirectories_RemovesEmptyDirs()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var sessionDir = Path.Combine(tempDir, Guid.NewGuid().ToString());
        var subDir1 = Path.Combine(sessionDir, "subdir1");
        var subDir2 = Path.Combine(subDir1, "subdir2");
        var filePath = Path.Combine(subDir2, "file.txt");

        try
        {
            // Create directory structure
            Directory.CreateDirectory(subDir2);
            
            // Act
            var result = await _pathService.CleanupEmptyDirectoriesAsync(filePath, sessionDir);

            // Assert
            Assert.True(result);
            Assert.False(Directory.Exists(subDir2));
            Assert.False(Directory.Exists(subDir1));
            Assert.True(Directory.Exists(sessionDir)); // Session dir should remain
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(sessionDir))
            {
                Directory.Delete(sessionDir, true);
            }
        }
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task CleanupEmptyDirectoriesAsync_DirectoryWithFiles_StopsCleanup()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var sessionDir = Path.Combine(tempDir, Guid.NewGuid().ToString());
        var subDir1 = Path.Combine(sessionDir, "subdir1");
        var subDir2 = Path.Combine(subDir1, "subdir2");
        var filePath = Path.Combine(subDir2, "file.txt");
        var keepFile = Path.Combine(subDir1, "keepfile.txt");

        try
        {
            // Create directory structure with a file
            Directory.CreateDirectory(subDir2);
            File.WriteAllText(keepFile, "test content");

            // Act
            var result = await _pathService.CleanupEmptyDirectoriesAsync(filePath, sessionDir);

            // Assert
            Assert.True(result);
            Assert.False(Directory.Exists(subDir2)); // Empty dir should be removed
            Assert.True(Directory.Exists(subDir1)); // Dir with file should remain
            Assert.True(File.Exists(keepFile));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(sessionDir))
            {
                Directory.Delete(sessionDir, true);
            }
        }
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("valid/path", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void IsValidPath_VariousInputs_ReturnsExpectedResult(string? path, bool expected)
    {
        // Act
        var result = _pathService.IsValidPath(path!);

        // Assert
        Assert.Equal(expected, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void GetMaxPathLength_ReturnsPositiveValue()
    {
        // Act
        var result = _pathService.GetMaxPathLength();

        // Assert
        Assert.True(result > 0);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void PreserveVirtualDirectoryStructure_ValidBlobName_PreservesStructure()
    {
        // Arrange
        var blobName = "folder1/folder2/file.txt";

        // Act
        var result = _pathService.PreserveVirtualDirectoryStructure(blobName);

        // Assert
        var expectedPath = Path.Combine("folder1", "folder2", "file.txt");
        Assert.Equal(expectedPath, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void PreserveVirtualDirectoryStructure_NullBlobName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _pathService.PreserveVirtualDirectoryStructure(null!));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void PreserveVirtualDirectoryStructure_SimpleFilename_ReturnsFilename()
    {
        // Arrange
        var blobName = "file.txt";

        // Act
        var result = _pathService.PreserveVirtualDirectoryStructure(blobName);

        // Assert
        Assert.Equal("file.txt", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task EnsureDirectoryExistsAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        
        var tempDir = Path.GetTempPath();
        var newDir = Path.Combine(tempDir, Guid.NewGuid().ToString());
        var filePath = Path.Combine(newDir, "file.txt");

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => 
            _pathService.EnsureDirectoryExistsAsync(filePath, cancellationTokenSource.Token));
        Assert.True(exception is OperationCanceledException);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task CleanupEmptyDirectoriesAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => 
            _pathService.CleanupEmptyDirectoriesAsync("/path/file.txt", "/session", cancellationTokenSource.Token));
        Assert.True(exception is OperationCanceledException);
    }

    private static Session CreateTestSession(string name, string directory)
    {
        return new Session(
            Name: name,
            Directory: directory,
            StorageAccountName: "testaccount",
            SubscriptionId: Guid.NewGuid(),
            CreatedAt: DateTime.UtcNow,
            LastAccessedAt: DateTime.UtcNow
        );
    }
}