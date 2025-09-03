using AzStore.Core.Models.Downloads;
using AzStore.Core.Services.Implementations;
using Xunit;

namespace AzStore.Core.Tests.Services;

[Trait("Category", "Unit")]
public class AutoRenameFileConflictResolverTests
{
    [Fact]
    public async Task ResolveAsync_NoLocalFile_ReturnsDesiredPath()
    {
        var resolver = new AutoRenameFileConflictResolver();
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "test.txt");

        var info = new FileConflictInfo(
            LocalExists: false,
            LocalSize: null,
            LocalLastModifiedUtc: null,
            LocalChecksumMd5: null,
            RemoteSize: 10,
            RemoteLastModifiedUtc: null,
            RemoteChecksumMd5: null);

        var decision = await resolver.ResolveAsync(temp, ConflictResolution.Overwrite, info);

        Assert.False(decision.Skip);
        Assert.Equal(temp, decision.ResolvedPath);
    }

    [Fact]
    public async Task ResolveAsync_SkipMode_ReturnsSkip()
    {
        var resolver = new AutoRenameFileConflictResolver();
        var tempDir = Directory.CreateTempSubdirectory();
        var path = Path.Combine(tempDir.FullName, "file.txt");
        await File.WriteAllTextAsync(path, "existing");

        var info = new FileConflictInfo(true, new FileInfo(path).Length, File.GetLastWriteTimeUtc(path), null, 5, null, null);
        var decision = await resolver.ResolveAsync(path, ConflictResolution.Skip, info);

        Assert.True(decision.Skip);
        tempDir.Delete(true);
    }

    [Fact]
    public async Task ResolveAsync_RenameMode_GeneratesUniqueName()
    {
        var resolver = new AutoRenameFileConflictResolver();
        var tempDir = Directory.CreateTempSubdirectory();
        var path = Path.Combine(tempDir.FullName, "file.txt");
        await File.WriteAllTextAsync(path, "existing");

        var info = new FileConflictInfo(true, new FileInfo(path).Length, File.GetLastWriteTimeUtc(path), null, 5, null, null);
        var decision = await resolver.ResolveAsync(path, ConflictResolution.Rename, info);

        Assert.False(decision.Skip);
        Assert.NotNull(decision.ResolvedPath);
        Assert.StartsWith(Path.Combine(tempDir.FullName, "file ("), decision.ResolvedPath);
        Assert.EndsWith(")" + Path.GetExtension(path), decision.ResolvedPath);

        tempDir.Delete(true);
    }

    [Fact]
    public async Task ResolveAsync_AskMode_DefaultsToRename()
    {
        var resolver = new AutoRenameFileConflictResolver();
        var tempDir = Directory.CreateTempSubdirectory();
        var path = Path.Combine(tempDir.FullName, "file.txt");
        await File.WriteAllTextAsync(path, "existing");

        var info = new FileConflictInfo(true, new FileInfo(path).Length, File.GetLastWriteTimeUtc(path), null, 5, null, null);
        var decision = await resolver.ResolveAsync(path, ConflictResolution.Ask, info);

        Assert.False(decision.Skip);
        Assert.NotNull(decision.ResolvedPath);

        tempDir.Delete(true);
    }
}

