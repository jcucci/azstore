namespace AzStore.Terminal.Utilities;

public sealed class NullDownloadActivity : IDownloadActivity
{
    private sealed class Nop : IDisposable { public void Dispose() { } }

    public int ActiveCount => 0;
    public bool HasActiveDownloads => false;
    public IDisposable Begin() => new Nop();
}

