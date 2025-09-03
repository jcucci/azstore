using AzStore.Core.Models.Downloads;

namespace AzStore.Terminal.UI;

public sealed record ConflictPromptResult(
    ConflictResolution Decision,
    bool ApplyToAll,
    bool RememberForSession);

