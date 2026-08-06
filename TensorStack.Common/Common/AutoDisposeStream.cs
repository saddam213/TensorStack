using System;
using System.IO;

namespace TensorStack.Common
{
    public sealed class AutoDisposeStream : Stream
    {
        private readonly Stream _underlyingStream;

        public AutoDisposeStream(Stream underlyingStream)
        {
            _underlyingStream = underlyingStream ?? throw new ArgumentNullException(nameof(underlyingStream));
        }

        public override bool CanRead => _underlyingStream.CanRead;
        public override bool CanSeek => _underlyingStream.CanSeek;
        public override bool CanWrite => _underlyingStream.CanWrite;
        public override long Length => _underlyingStream.Length;
        public override long Position { get => _underlyingStream.Position; set => _underlyingStream.Position = value; }
        public override void Flush() => _underlyingStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _underlyingStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _underlyingStream.Seek(offset, origin);
        public override void SetLength(long value) => _underlyingStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _underlyingStream.Write(buffer, offset, count);
        public override void Close()
        {
            _underlyingStream.Dispose();
            base.Close();
        }
    }
}
