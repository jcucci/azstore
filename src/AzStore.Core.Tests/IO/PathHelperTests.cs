using System.Runtime.InteropServices;
using AzStore.Core.IO;
using Xunit;

namespace AzStore.Core.Tests.IO;

public class PathHelperTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void SanitizePathComponent_ValidInput_ReturnsInput()
    {
        // Arrange
        var input = "validfilename.txt";

        // Act
        var result = PathHelper.SanitizePathComponent(input);

        // Assert
        Assert.Equal(input, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void SanitizePathComponent_NullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PathHelper.SanitizePathComponent(null!));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void SanitizePathComponent_EmptyInput_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PathHelper.SanitizePathComponent(string.Empty));
        Assert.Throws<ArgumentException>(() => PathHelper.SanitizePathComponent("   "));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void SanitizePathComponent_InvalidCharacters_ReplacesWithUnderscore()
    {
        // Arrange
        var input = "file<name>with|invalid*chars?.txt";
        var expected = "file_name_with_invalid_chars_.txt";

        // Act
        var result = PathHelper.SanitizePathComponent(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void SanitizePathComponent_CustomReplacementChar_UsesCustomChar()
    {
        // Arrange
        var input = "file|name";
        var replacementChar = '-';
        var expected = "file-name";

        // Act
        var result = PathHelper.SanitizePathComponent(input, replacementChar);

        // Assert
        Assert.Equal(expected, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void SanitizePathComponent_WindowsReservedName_AppendsFileExtension()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return; // Skip test on non-Windows platforms

        // Arrange
        var input = "CON.txt";
        var expectedPrefix = "CON_file";

        // Act
        var result = PathHelper.SanitizePathComponent(input);

        // Assert
        Assert.StartsWith(expectedPrefix, result);
        Assert.EndsWith(".txt", result);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("CON")]
    [InlineData("PRN")]
    [InlineData("AUX")]
    [InlineData("NUL")]
    [InlineData("COM1")]
    [InlineData("LPT1")]
    public void SanitizePathComponent_WindowsReservedNames_HandlesReservedNames(string reservedName)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return; // Skip test on non-Windows platforms

        // Act
        var result = PathHelper.SanitizePathComponent(reservedName);

        // Assert
        Assert.NotEqual(reservedName, result);
        Assert.Contains("_file", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void SanitizePathComponent_WindowsTrailingDotsAndSpaces_RemovesTrailing()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return; // Skip test on non-Windows platforms

        // Arrange
        var input = "filename... ";
        var expected = "filename";

        // Act
        var result = PathHelper.SanitizePathComponent(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void SanitizePathComponent_TooLongFilename_TruncatesWithExtension()
    {
        // Arrange
        var longName = new string('a', 250);
        var input = $"{longName}.txt";

        // Act
        var result = PathHelper.SanitizePathComponent(input);

        // Assert
        Assert.True(result.Length <= 255);
        Assert.EndsWith(".txt", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ConvertBlobPathToLocalPath_SimpleFilename_ReturnsFilename()
    {
        // Arrange
        var blobName = "file.txt";

        // Act
        var result = PathHelper.ConvertBlobPathToLocalPath(blobName);

        // Assert
        Assert.Equal("file.txt", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ConvertBlobPathToLocalPath_WithForwardSlashes_ConvertsToLocalPath()
    {
        // Arrange
        var blobName = "folder1/folder2/file.txt";

        // Act
        var result = PathHelper.ConvertBlobPathToLocalPath(blobName);

        // Assert
        var expectedPath = Path.Combine("folder1", "folder2", "file.txt");
        Assert.Equal(expectedPath, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ConvertBlobPathToLocalPath_NullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PathHelper.ConvertBlobPathToLocalPath(null!));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ConvertBlobPathToLocalPath_WithInvalidChars_SanitizesComponents()
    {
        // Arrange
        var blobName = "folder|with*invalid/chars<here>/file?.txt";

        // Act
        var result = PathHelper.ConvertBlobPathToLocalPath(blobName);

        // Assert
        Assert.DoesNotContain("|", result);
        Assert.DoesNotContain("*", result);
        Assert.DoesNotContain("<", result);
        Assert.DoesNotContain(">", result);
        Assert.DoesNotContain("?", result);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("valid/path/file.txt", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void IsValidPath_VariousInputs_ReturnsExpectedResult(string? path, bool expected)
    {
        // Act
        var result = PathHelper.IsValidPath(path!);

        // Assert
        Assert.Equal(expected, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void IsValidPath_PathWithInvalidChars_ReturnsFalse()
    {
        // Arrange - Use characters that are invalid on all platforms
        var invalidPath = "path\0with\x01invalid\x02chars";

        // Act
        var result = PathHelper.IsValidPath(invalidPath);

        // Assert
        Assert.False(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void GetMaxPathLength_ReturnsPositiveValue()
    {
        // Act
        var result = PathHelper.GetMaxPathLength();

        // Assert
        Assert.True(result > 0);
        Assert.True(result >= 260); // At least Windows legacy limit
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("", false)]
    [InlineData("short", false)]
    public void IsPathTooLong_ShortPaths_ReturnsFalse(string path, bool expected)
    {
        // Act
        var result = PathHelper.IsPathTooLong(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateSafeDirectoryName_ValidName_ReturnsName()
    {
        // Arrange
        var containerName = "validcontainer";

        // Act
        var result = PathHelper.CreateSafeDirectoryName(containerName);

        // Assert
        Assert.Equal(containerName, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateSafeDirectoryName_InvalidChars_SanitizesName()
    {
        // Arrange
        var containerName = "container|with*invalid<chars>";

        // Act
        var result = PathHelper.CreateSafeDirectoryName(containerName);

        // Assert
        Assert.DoesNotContain("|", result);
        Assert.DoesNotContain("*", result);
        Assert.DoesNotContain("<", result);
        Assert.DoesNotContain(">", result);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("file.txt", "file.txt")]
    [InlineData("folder/file.txt", "file.txt")]
    [InlineData("folder1/folder2/file.txt", "file.txt")]
    [InlineData("folder1/folder2/folder3/file.ext", "file.ext")]
    public void ExtractFilename_VariousInputs_ReturnsFilename(string blobName, string expected)
    {
        // Act
        var result = PathHelper.ExtractFilename(blobName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ExtractFilename_NullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PathHelper.ExtractFilename(null!));
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("file.txt", null)]
    [InlineData("folder/file.txt", "folder")]
    [InlineData("folder1/folder2/file.txt", "folder1/folder2")]
    public void ExtractVirtualDirectory_VariousInputs_ReturnsExpectedDirectory(string blobName, string? expectedDir)
    {
        // Act
        var result = PathHelper.ExtractVirtualDirectory(blobName);

        // Assert
        if (expectedDir == null)
        {
            Assert.Null(result);
        }
        else
        {
            Assert.NotNull(result);
            var expectedPath = Path.Combine(expectedDir.Split('/'));
            Assert.Equal(expectedPath, result);
        }
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ExtractVirtualDirectory_NullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PathHelper.ExtractVirtualDirectory(null!));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void SafeCombine_MultiplePaths_CombinesPaths()
    {
        // Arrange
        string[] paths = ["path1", "path2", "path3"];

        // Act
        var result = PathHelper.SafeCombine(paths);

        // Assert
        var expected = Path.Combine(paths);
        Assert.Equal(expected, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void SafeCombine_SinglePath_ReturnsPath()
    {
        // Arrange
        string[] paths = ["single"];

        // Act
        var result = PathHelper.SafeCombine(paths);

        // Assert
        Assert.Equal("single", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void SafeCombine_WithNullAndEmpty_IgnoresNullAndEmpty()
    {
        // Arrange
        string?[] paths = ["path1", null, "", "   ", "path2"];

        // Act
        var result = PathHelper.SafeCombine(paths);

        // Assert
        var expected = Path.Combine("path1", "path2");
        Assert.Equal(expected, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void SafeCombine_AllNullOrEmpty_ReturnsEmpty()
    {
        // Arrange
        string?[] paths = [null, "", "   "];

        // Act
        var result = PathHelper.SafeCombine(paths);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void SafeCombine_NoArguments_ReturnsEmpty()
    {
        // Act
        var result = PathHelper.SafeCombine();

        // Assert
        Assert.Equal(string.Empty, result);
    }
}