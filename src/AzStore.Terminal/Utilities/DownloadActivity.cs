using System.Threading;

namespace AzStore.Terminal.Utilities;

public sealed class DownloadActivity : IDownloadActivity
{
    private int _active;

    public int ActiveCount => Volatile.Read(ref _active);
    public bool HasActiveDownloads => ActiveCount > 0;

    public IDisposable Begin()
    {
        Interlocked.Increment(ref _active);
        return new Scope(this);
    }

    private void End()
    {
        Interlocked.Decrement(ref _active);
    }

    private sealed class Scope : IDisposable
    {
        private DownloadActivity? _owner;

        public Scope(DownloadActivity owner) => _owner = owner;

        public void Dispose()
        {
            var owner = Interlocked.Exchange(ref _owner, null);
            owner?.End();
        }
    }
}

