namespace BaseCleanArchitecture.WebAPI.Middleware;

/// <summary>
/// Write-through wrapper: everything goes to the real response stream immediately, while the first
/// <c>maxBytes</c> bytes are kept and the total size is counted. Never buffers the whole response.
/// </summary>
internal sealed class CapturingStream : Stream
{
    private readonly Stream _inner;
    private readonly int _maxBytes;
    private readonly MemoryStream _captured = new();

    public CapturingStream(Stream inner, int maxBytes)
    {
        _inner = inner;
        _maxBytes = maxBytes;
    }

    public ReadOnlySpan<byte> Captured => _captured.GetBuffer().AsSpan(0, (int)_captured.Length);

    public long TotalBytes { get; private set; }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        _inner.Write(buffer);
        Capture(buffer);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await _inner.WriteAsync(buffer, cancellationToken);
        Capture(buffer.Span);
    }

    public override void Flush() => _inner.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    private void Capture(ReadOnlySpan<byte> buffer)
    {
        TotalBytes += buffer.Length;

        var room = _maxBytes - (int)_captured.Length;
        if (room > 0)
        {
            _captured.Write(buffer[..Math.Min(room, buffer.Length)]);
        }
    }
}
