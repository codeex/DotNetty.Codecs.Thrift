// Copyright (c) CodeEx.cn & webmote. All rights reserved.
// Licensed under theApache License. 
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using ThriftSharp.Models;

namespace DotNetty.Codecs.Thrift
{
    ///
    /// A decoder that splits the received {@link ByteBuf}s dynamically by the
    /// value of the Thrift Frame Buffers
    /// Base 1 integer length field in the message. 
    /// For example:
    /// 
    /// BEFORE DECODE (304 bytes)       AFTER DECODE (300 bytes)
    /// +------------+---------------+      +---------------+
    /// | Length     | Thrift   Data |----->| Thrift   Data |
    /// | 0x0000AC02 |  (300 bytes)  |      |  (300 bytes)  |
    /// +------------+---------------+      +---------------+
    ///
    public class ThriftFrameDecoder : ByteToMessageDecoder
    {
        private ThriftOptions _opts;
        // todo: maxFrameLength + safe skip + fail-fast option (just like LengthFieldBasedFrameDecoder)
        public ThriftFrameDecoder(ThriftOptions options = null)
        {
            _opts = options ?? ThriftOptions.DefaultOptions;
        }

        protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            input.MarkReaderIndex();

            int preIndex = input.ReaderIndex;
            int length = ReadRawVarint32(input);

            if (preIndex == input.ReaderIndex)
            {
                return;
            }

            if (length < 0)
            {
                throw new CorruptedFrameException($"Negative length: {length}");
            }

            if (input.ReadableBytes < length)
            {
                input.ResetReaderIndex();
            }
            else
            {
                if (length > _opts.MaxFrameSize)
                {
                    input.SetReaderIndex(length);
                    context.FireExceptionCaught(new TooLongFrameException($"frame length ({length}) exceeds the allowed maximum ({_opts.MaxFrameSize})"));
                }
                IByteBuffer byteBuffer = input.ReadSlice(length);
                output.Add(byteBuffer.Retain());
            }
        }

        private int ReadRawVarint32(IByteBuffer buffer)
        {
            Contract.Requires(buffer != null);

            if (!buffer.IsReadable())
            {
                return 0;
            }

            buffer.MarkReaderIndex();
            if (buffer.ReadableBytes >= 4)
            {
                var size = buffer.ReadInt();
                string name;
                ThriftMessageType type;
                if (size < 0)
                {
                    uint version = (uint)size & _opts.VERSION_MASK;
                    if (version != _opts.VERSION_1)
                    {
                        throw new CorruptedFrameException("ThriftProtocolExceptionType.InvalidProtocol");
                    }

                    var len = buffer.ReadInt();
                    name = buffer.ReadString(len, Encoding.UTF8);
                    type = (ThriftMessageType)(size & 0xFF);
                }
                else
                {
                    // Old protocol version
                    name = buffer.ReadString(size, Encoding.UTF8);
                    type = (ThriftMessageType)buffer.ReadByte();
                }

                var seq = buffer.ReadInt(); // Message sequence ID

                return size;
            }
            else
            {
                buffer.ResetReaderIndex();
                return 0;
            }
        }
    }
}
