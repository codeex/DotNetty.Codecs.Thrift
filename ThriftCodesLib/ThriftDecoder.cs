using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Codeex.Codecs.Protobuf.Message;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using Thrift.Protocol;

namespace Codeex.Codecs.Protobuf
{
    /// <summary>
    /// 需要组包
    /// </summary>
    public class ThriftDecoder : ByteToMessageDecoder
    {
        protected const uint VERSION_MASK = 0xffff0000;
        protected const uint VERSION_1 = 0x80010000;
        bool discarding;
        int discardedBytes;
        public int MaxFrameSize { get; set; }
        public ThriftDecoder(int maxFrameSize,bool strictRead = true)
        {
            this.MaxFrameSize = maxFrameSize;
            this.StrictRead = strictRead;
        }
        public static int MESSAGE_FRAME_SIZE = 4;

        private int nTotalReadSize = 0;
        public bool StrictRead { get; set; }
        protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            object decoded = this.Decode(context, input);
            if (decoded != null)
                output.Add(decoded);
        }

        protected virtual object Decode(IChannelHandlerContext ctx, IByteBuffer buffer)
        {
            buffer.MarkReaderIndex();
            try
            {
                TMessage message = new TMessage();
                int size = buffer.ReadInt();
                if (size < 0)
                {
                    uint version = (uint) size & VERSION_MASK;
                    if (version != VERSION_1)
                    {
                        Trace.WriteLine($"Bad version in ReadMessageBegin: {version}");
                        buffer.SetReaderIndex(buffer.WriterIndex);
                        ctx.FireExceptionCaught(
                            new ThriftDecoderException($"Bad version in ReadMessageBegin: {version}"));
                        return null;
                    }

                    message.Type = (Thrift.Protocol.TMessageType) (size & 0x000000ff);
                    message.Name = this.ReadString(buffer);
                    message.SeqID = buffer.ReadInt();
                }
                else
                {
                    if (this.StrictRead)
                    {
                        ctx.FireExceptionCaught(
                            new ThriftDecoderException("Missing version in readMessageBegin, old client?"));
                    }

                    message.Name = this.ReadStringBody(buffer, size);
                    message.Type = (Thrift.Protocol.TMessageType) buffer.ReadByte();
                    message.SeqID = buffer.ReadInt();
                }

                var messageSize = buffer.ReaderIndex;
                buffer.MarkReaderIndex();
                var len = GetTypeSize(buffer, TType.Struct);
                if (len >= 0)
                {
                    IByteBuffer frame;

                    if (len > this.MaxFrameSize)
                    {
                        buffer.SetReaderIndex(len);
                        ctx.FireExceptionCaught(
                            new ThriftDecoderException(new TooLongFrameException($"frame length ({len}) exceeds the allowed maximum ({this.MaxFrameSize})")));

                        return null;
                    }

                    buffer.SetReaderIndex(0);
                    frame = buffer.ReadSlice(messageSize + len);
                    frame.Retain();
                    return new FrameThriftMessage(ThriftTransportType.Framed, frame);
                    //return frame.Retain();
                }
                else
                {
                    buffer.SetReaderIndex(0);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (buffer.ReadableBytes < 1024)
                {
                    buffer.ResetReaderIndex();
                    return null;
                }
                else
                {
                    Trace.WriteLine($"Bad Message Format in ReadMessageBegin");
                    buffer.SetReaderIndex(buffer.WriterIndex);
                    ctx.FireExceptionCaught(
                        new ThriftDecoderException($"Bad Message Format in ReadMessageBegin"));
                    return null;

                }
            }
            
        }
       
        public string ReadString(IByteBuffer buffer)
        {
            if (buffer == null)
            {
                return null;
            }

            var buf = ReadBinary(buffer);
            return Encoding.UTF8.GetString(buf, 0, buf.Length);
        }

        public byte[] ReadBinary(IByteBuffer buffer)
        {
            if (buffer == null)
            {
                return null;
            }

            int size = buffer.ReadInt();
            byte[] buf = new byte[size];
            buffer.ReadBytes(buf, 0, size);
            return buf;
        }

        private string ReadStringBody(IByteBuffer buffer, int size)
        {
            if (buffer == null)
            {
                return null;
            }

            byte[] buf = new byte[size];
            buffer.ReadBytes(buf, 0, size);
            return Encoding.UTF8.GetString(buf, 0, buf.Length);
        }

        /// <summary>
        /// 测试类型的长度，如果不完整则返回 -1
        /// 因为该函数有递归检查，因此 buffer的读索引会被改动到类型读完的位置
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        protected int GetTypeSize(IByteBuffer buffer, TType type)
        {
            int nTypeSize = 0;
            var sz = 0;
            var t = TType.Stop;
            var szType = 0;
            var bCheckLen = true;

            switch (type)
            {
                case TType.Bool:
                case TType.Byte:
                    nTypeSize = 1;
                    break;
                case TType.Double:
                case TType.I64:
                    nTypeSize = 8;
                    break;
                case TType.I16:
                    nTypeSize = 2;
                    break;
                case TType.I32:
                    nTypeSize = 4;
                    break;
                case TType.String:
                    nTypeSize = 4;
                    if (buffer.ReadableBytes < nTypeSize)
                    {
                        return -1; //不完整报文
                    }

                    sz = buffer.ReadInt();
                    if (buffer.ReadableBytes < sz)
                    {
                        return -1;
                    }
                    nTypeSize += sz;
                    buffer.SkipBytes(sz);
                    bCheckLen = false;
                    break;
                case TType.Struct:
                    while (true)
                    {
                        if (buffer.ReadableBytes < 1)
                        {
                            return -1; //不完整报文
                        }

                        nTypeSize += 1;
                        t = (TType)buffer.ReadByte();
                        if (t == TType.Stop)
                        {
                            break;
                        }
                        else
                        {
                            if (buffer.ReadableBytes < 2)
                            {
                                return -1; //不完整报文
                            }
                            nTypeSize += 2;
                            buffer.SkipBytes(2);
                            sz = GetTypeSize(buffer, t);
                            if (sz < 0)
                            {
                                return -1;
                            }

                            nTypeSize += sz;
                            // buffer.SkipBytes(sz); GetTypeSize内已经移动过readIndex
                        }

                    }

                    bCheckLen = false;
                    break;
                case TType.Map:
                    if (buffer.ReadableBytes < 6)
                    {
                        return -1; //不完整报文
                    }

                    var kt = buffer.ReadByte();
                    var kv = buffer.ReadByte();
                    var size = buffer.ReadInt();
                    nTypeSize = 6;
                    buffer.MarkReaderIndex();
                    var szKt = GetTypeSize(buffer,(TType)kt);
                    var szKv = GetTypeSize(buffer,(TType)kv);
                    buffer.ResetReaderIndex();
                    if (szKt < 0 || szKv < 0)
                    {
                        return -1;
                    }

                    nTypeSize += size * (szKt + szKv);
                    if (buffer.ReadableBytes < size * (szKt + szKv))
                    {
                        return -1;
                    }

                    buffer.SkipBytes(size * (szKt + szKv));
                    bCheckLen = false;
                    break;
                case TType.Set:
                case TType.List:
                    if (buffer.ReadableBytes < 5)
                    {
                        return -1; //不完整报文
                    }
                    
                    nTypeSize += 5;
                    t = (TType)buffer.ReadByte();
                    sz = buffer.ReadInt();
                    buffer.MarkReaderIndex();
                    szType = GetTypeSize(buffer,t);
                    buffer.ResetReaderIndex();
                    if (szType < 0)
                    {
                        return -1;
                    }
                    nTypeSize += sz * szType;
                    if (buffer.ReadableBytes < sz * szType)
                    {
                        return -1;
                    }
                    buffer.SkipBytes(sz * szType);
                    bCheckLen = false;
                    break;
            }

            if (bCheckLen)
            {
                if (buffer.ReadableBytes < nTypeSize)
                {
                    return -1; //不完整报文
                }

                buffer.SkipBytes(nTypeSize);
            }
            return nTypeSize;

        }

    }
}
