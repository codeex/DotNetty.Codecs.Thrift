using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Codecs.Thrift.Message;
using DotNetty.Transport.Channels;
using Thrift;
using Thrift.Transport;

namespace DotNetty.Codecs.Thrift.Transport
{
    public class TThriftTransport : TTransport
    {
        private IChannel _channel;
        private IByteBuffer _inBuffer;
        private readonly ThriftTransportType _thriftTransportType;
        private IByteBuffer _outBuffer;
        private const int DEFAULT_OUTPUT_BUFFER_SIZE = 1024;
        private readonly int _initialReaderIndex;
        private readonly int _initialBufferPosition;
        private int _bufferPosition;
        private int _bufferEnd;
        private readonly byte[] _buffer;
        private TApplicationException _tApplicationException;
        private bool _needReleaseInputBuffer = false;

        public TThriftTransport(IChannel channel,
                               IByteBuffer inBuffer,
                               ThriftTransportType thriftTransportType, bool needReleaseInputBuffer = false)
        {
            this._channel = channel;
            this._inBuffer = inBuffer;
            this._thriftTransportType = thriftTransportType;
            this._outBuffer = Unpooled.Buffer(DEFAULT_OUTPUT_BUFFER_SIZE);
            this._initialReaderIndex = inBuffer.ReaderIndex;
            this._needReleaseInputBuffer = needReleaseInputBuffer;

            if (!inBuffer.HasArray)
            {
                _buffer = null;
                _bufferPosition = 0;
                _initialBufferPosition = _bufferEnd = -1;
            }
            else
            {
                _buffer = inBuffer.Array;
                _initialBufferPosition = _bufferPosition = inBuffer.ArrayOffset + inBuffer.ReaderIndex;
                _bufferEnd = _bufferPosition + inBuffer.ReadableBytes;
                // Without this, reading from a !in.hasArray() buffer will advance the readerIndex
                // of the buffer, while reading from a in.hasArray() buffer will not advance the
                // readerIndex, and this has led to subtle bugs. This should help to identify
                // those problems by making things more consistent.
                inBuffer.SetReaderIndex(inBuffer.ReaderIndex + inBuffer.ReadableBytes);
            }
        }

        //public TNiftyTransport(IChannel channel, ThriftMessage message, bool needReleaseInputBuffer = false)
        //    : this(channel, message.Buffer, message.TransportType, needReleaseInputBuffer)
        //{ }

        public override bool IsOpen
        {
            get { return _channel.Open; }
        }

        public override void Open()
        {
            // no-op
        }

        public override void Close()
        {
            this._channel = null;
            if ((_inBuffer?.ReferenceCount ?? 0) > 0)
            {
                _inBuffer.Release();
                _inBuffer = null;
            }
            if ((_outBuffer?.ReferenceCount ?? 0) > 0)
            {
                _outBuffer.Release();
            }
            _inBuffer = null;

        }

        public override int Read(byte[] bytes, int offset, int length)
        {
            if (GetBytesRemainingInBuffer() >= 0)
            {
                int _read = Math.Min(GetBytesRemainingInBuffer(), length);
                Array.Copy(GetBuffer(), GetBufferPosition(), bytes, offset, _read);
                ConsumeBuffer(_read);
                return _read;
            }
            else
            {
                int _read = Math.Min(_inBuffer.ReadableBytes, length);
                _inBuffer.ReadBytes(bytes, offset, _read);
                return _read;
            }
        }

        public override void Write(byte[] buf, int off, int len)
        {
            _outBuffer.WriteBytes(buf, off, len);
        }

        public IByteBuffer OutBuffer
        {
            get { return _outBuffer; }
            set { _outBuffer = value; }
        }

        public ThriftTransportType TransportType
        {
            get
            {
                return _thriftTransportType;
            }
        }

        public override void Flush()
        {
            // Flush is a no-op
        }

        public void ConsumeBuffer(int len)
        {
            _bufferPosition += len;
        }

        public byte[] GetBuffer()
        {
            return _buffer;
        }

        public int GetBufferPosition()
        {
            return _bufferPosition;
        }

        public int GetBytesRemainingInBuffer()
        {
            return _bufferEnd - _bufferPosition;
        }

        public int GetReadByteCount()
        {
            if (GetBytesRemainingInBuffer() >= 0)
            {
                return GetBufferPosition() - _initialBufferPosition;
            }
            else
            {
                return _inBuffer.ReaderIndex - _initialReaderIndex;
            }
        }

        public int GetWrittenByteCount()
        {
            return OutBuffer.WriterIndex;
        }

        public void setTApplicationException(TApplicationException e)
        {
            _tApplicationException = e;
        }

        public TApplicationException GetTApplicationException()
        {
            return _tApplicationException;
        }

        /// <summary>
        /// 此处如释放，则读取一个 unframe信息后 则关闭连接
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            //if (disposing)
            //{
            //    this.Close();
            //}
        }
    }

}
