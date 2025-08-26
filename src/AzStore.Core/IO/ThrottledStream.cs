namespace AzStore.Core.IO;

/// <summary>
/// A wrapper stream that limits the rate of data transfer to implement bandwidth throttling.
/// </summary>
public class ThrottledStream : Stream
{
    private readonly Stream _baseStream;
    private readonly long _maxBytesPerSecond;
    private readonly Lock _lock = new();
    private long _totalBytesTransferred;
    private readonly DateTime _startTime;

    /// <summary>
    /// Initializes a new instance of the ThrottledStream.
    /// </summary>
    /// <param name="baseStream">The underlying stream to throttle.</param>
    /// <param name="maxBytesPerSecond">Maximum bytes per second to allow.</param>
    public ThrottledStream(Stream baseStream, long maxBytesPerSecond)
    {
        _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
        _maxBytesPerSecond = maxBytesPerSecond > 0 ? maxBytesPerSecond : throw new ArgumentOutOfRangeException(nameof(maxBytesPerSecond));
        _startTime = DateTime.UtcNow;
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
        await ThrottleIfNeededAsync(count, cancellationToken);
        await _baseStream.WriteAsync(buffer, offset, count, cancellationToken);
        
        lock (_lock)
        {
            _totalBytesTransferred += count;
        }
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await ThrottleIfNeededAsync(buffer.Length, cancellationToken);
        await _baseStream.WriteAsync(buffer, cancellationToken);
        
        lock (_lock)
        {
            _totalBytesTransferred += buffer.Length;
        }
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrottleIfNeeded(count);
        _baseStream.Write(buffer, offset, count);
        
        lock (_lock)
        {
            _totalBytesTransferred += count;
        }
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await ThrottleIfNeededAsync(count, cancellationToken);
        var bytesRead = await _baseStream.ReadAsync(buffer, offset, count, cancellationToken);
        
        lock (_lock)
        {
            _totalBytesTransferred += bytesRead;
        }
        
        return bytesRead;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await ThrottleIfNeededAsync(buffer.Length, cancellationToken);
        var bytesRead = await _baseStream.ReadAsync(buffer, cancellationToken);
        
        lock (_lock)
        {
            _totalBytesTransferred += bytesRead;
        }
        
        return bytesRead;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrottleIfNeeded(count);
        var bytesRead = _baseStream.Read(buffer, offset, count);
        
        lock (_lock)
        {
            _totalBytesTransferred += bytesRead;
        }
        
        return bytesRead;
    }

    public override void Flush() => _baseStream.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => _baseStream.FlushAsync(cancellationToken);

    public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);
    public override void SetLength(long value) => _baseStream.SetLength(value);

    private async Task ThrottleIfNeededAsync(int bytesToTransfer, CancellationToken cancellationToken)
    {
        var delay = CalculateDelay(bytesToTransfer);
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, cancellationToken);
        }
    }

    private void ThrottleIfNeeded(int bytesToTransfer)
    {
        var delay = CalculateDelay(bytesToTransfer);
        if (delay > TimeSpan.Zero)
        {
            Thread.Sleep(delay);
        }
    }

    private TimeSpan CalculateDelay(int bytesToTransfer)
    {
        lock (_lock)
        {
            var elapsed = DateTime.UtcNow - _startTime;
            var expectedTimeForActualBytes = TimeSpan.FromSeconds((double)_totalBytesTransferred / _maxBytesPerSecond);
            var expectedTimeForNewBytes = TimeSpan.FromSeconds((double)bytesToTransfer / _maxBytesPerSecond);
            var totalExpectedTime = expectedTimeForActualBytes + expectedTimeForNewBytes;
            
            return totalExpectedTime > elapsed ? totalExpectedTime - elapsed : TimeSpan.Zero;
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