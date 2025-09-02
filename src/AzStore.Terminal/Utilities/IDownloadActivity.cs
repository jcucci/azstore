namespace AzStore.Terminal.Utilities;

public interface IDownloadActivity
{
    int ActiveCount { get; }
    bool HasActiveDownloads { get; }

    /// <summary>
    /// Marks the beginning of a download scope. Dispose the returned object to decrement.
    /// </summary>
    IDisposable Begin();
}

