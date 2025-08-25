namespace AzStore.Core.IO;

/// <summary>
/// A wrapper stream that tracks the number of bytes transferred and reports progress.
/// </summary>
public class ProgressTrackingStream : Stream
{
    private readonly Stream _baseStream;
    private readonly Action<long> _progressCallback;
    private long _totalBytesTransferred;

    /// <summary>
    /// Initializes a new instance of the ProgressTrackingStream.
    /// </summary>
    /// <param name="baseStream">The underlying stream to track.</param>
    /// <param name="progressCallback">Callback to invoke with total bytes transferred.</param>
    public ProgressTrackingStream(Stream baseStream, Action<long> progressCallback)
    {
        _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
        _progressCallback = progressCallback ?? throw new ArgumentNullException(nameof(progressCallback));
    }

    public override bool CanRead => _baseStream.CanRead;
    public override bool CanSeek => _baseStream.CanSeek;
    public override bool CanWrite => _baseStream.CanWrite;
    public override long Length => _baseStream.Length;

    public override long Position
    {
        get => _baseStream.Position;
        set => _baseStream.Position = value;
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await _baseStream.WriteAsync(buffer, offset, count, cancellationToken);
        UpdateProgress(count);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _baseStream.Write(buffer, offset, count);
        UpdateProgress(count);
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var bytesRead = await _baseStream.ReadAsync(buffer, offset, count, cancellationToken);
        UpdateProgress(bytesRead);
        return bytesRead;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _baseStream.Read(buffer, offset, count);
        UpdateProgress(bytesRead);
        return bytesRead;
    }

    public override void Flush() => _baseStream.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => _baseStream.FlushAsync(cancellationToken);

    public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);
    public override void SetLength(long value) => _baseStream.SetLength(value);

    private void UpdateProgress(int bytesTransferred)
    {
        if (bytesTransferred > 0)
        {
            _totalBytesTransferred += bytesTransferred;
            _progressCallback(_totalBytesTransferred);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _baseStream.Dispose();
        }
        base.Dispose(disposing);
    }
}