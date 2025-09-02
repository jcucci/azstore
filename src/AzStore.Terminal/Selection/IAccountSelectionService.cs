using AzStore.Core.Models.Authentication;

namespace AzStore.Terminal.Selection;

public interface IAccountSelectionService
{
    Task<StorageAccountInfo?> PickAsync(
        IReadOnlyList<StorageAccountInfo> accounts,
        CancellationToken cancellationToken = default);
}

