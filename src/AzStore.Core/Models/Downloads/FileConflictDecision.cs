namespace AzStore.Core.Models.Downloads;

/// <summary>
/// Result of a conflict resolution decision.
/// </summary>
public sealed record FileConflictDecision(bool Skip, string? ResolvedPath, ConflictResolution ChosenMode, bool ApplyToAll, bool RememberForSession)
{
    public static FileConflictDecision SkipOnce() => new(true, null, ConflictResolution.Skip, false, false);

    public static FileConflictDecision UsePath(string path, ConflictResolution mode, bool applyToAll = false, bool rememberForSession = false)
        => new(false, path, mode, applyToAll, rememberForSession);
}

