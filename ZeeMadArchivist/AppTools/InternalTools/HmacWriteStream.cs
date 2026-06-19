using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CyberFeedForward.TheMadArchivist.AppTools.InternalTools
{
    internal sealed partial class HmacWriteStream(Stream inner, HMAC hmac) : Stream
    {
        private readonly Stream _inner = inner;
        private readonly HMAC _hmac = hmac;
        private bool _hashFinalized;

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => throw new NotSupportedException();
        }

        public override void Flush() => _inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => _inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_hashFinalized)
            {
                throw new InvalidOperationException("Cannot write after hash has been finalized.");
            }

            _hmac.TransformBlock(buffer, offset, count, null, 0);
            _inner.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (_hashFinalized)
            {
                throw new InvalidOperationException("Cannot write after hash has been finalized.");
            }

            if (buffer.Length == 0)
            {
                return;
            }

            var temp = buffer.ToArray();
            _hmac.TransformBlock(temp, 0, temp.Length, null, 0);
            _inner.Write(buffer);
        }

        public void FinalizeHash()
        {
            if (_hashFinalized)
            {
                return;
            }

            _hashFinalized = true;
            _hmac.TransformFinalBlock([], 0, 0);

            if (_hmac.Hash is null)
            {
                throw new CryptographicException("HMAC hash was not computed.");
            }

            _inner.Write(_hmac.Hash, 0, _hmac.Hash.Length);
            _inner.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_hashFinalized)
            {
                _hmac.TransformFinalBlock([], 0, 0);
            }

            base.Dispose(disposing);
        }
    }
}
