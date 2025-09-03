using AzStore.Core.Models.Downloads;
using AzStore.Core.Services.Abstractions;

namespace AzStore.Core.Services.Implementations;

/// <summary>
/// Default conflict resolver that respects explicit modes and auto-renames when asked.
/// Used when no interactive resolver is registered.
/// </summary>
public sealed class AutoRenameFileConflictResolver : IFileConflictResolver
{
    public Task<FileConflictDecision> ResolveAsync(string desiredPath, ConflictResolution mode, FileConflictInfo info, CancellationToken cancellationToken = default)
    {
        if (!info.LocalExists)
        {
            return Task.FromResult(FileConflictDecision.UsePath(desiredPath, mode));
        }

        return Task.FromResult(mode switch
        {
            ConflictResolution.Overwrite => FileConflictDecision.UsePath(desiredPath, ConflictResolution.Overwrite),
            ConflictResolution.Skip => FileConflictDecision.SkipOnce(),
            ConflictResolution.Rename => FileConflictDecision.UsePath(GenerateUniqueFileName(desiredPath), ConflictResolution.Rename),
            ConflictResolution.Ask => FileConflictDecision.UsePath(GenerateUniqueFileName(desiredPath), ConflictResolution.Rename),
            _ => FileConflictDecision.SkipOnce()
        });
    }

    private static string GenerateUniqueFileName(string originalPath)
    {
        var directory = Path.GetDirectoryName(originalPath) ?? string.Empty;
        var baseName = Path.GetFileNameWithoutExtension(originalPath);
        var extension = Path.GetExtension(originalPath);

        // If already has a (n), extract base
        var normalizedBase = System.Text.RegularExpressions.Regex.Replace(baseName, @" \((\d+)\)$", "");

        var counter = 1;
        string newPath;
        do
        {
            var newFileName = $"{normalizedBase} ({counter}){extension}";
            newPath = Path.Combine(directory, newFileName);
            counter++;
        } while (File.Exists(newPath));

        return newPath;
    }
}

