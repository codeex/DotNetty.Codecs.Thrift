// Copyright (c) CodeEx.cn & webmote. All rights reserved.
// Licensed under theApache License. 
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Codecs.Thrift.Message;
using DotNetty.Transport.Channels;
using Thrift.Protocol;
using Thrift.Transport;

namespace DotNetty.Codecs.Thrift
{
    /// <summary>
    /// 按照 TBinaryProtocol ， 编码 Thrift 消息
    /// </summary>
    public class ThriftMessageEncoder : MessageToByteEncoder<ThriftMessage>
    {
        private ThriftOptions _opts;
        public ThriftMessageEncoder(ThriftOptions options = null)
        {
            _opts = options ?? ThriftOptions.DefaultOptions;
        }
        protected IByteBuffer Encode(IChannelHandlerContext context, ThriftMessage message)
        {
            int frameSize = message.Content.ReadableBytes;

            if (message.Content.ReadableBytes > this._opts.MaxFrameSize)
            {
                context.FireExceptionCaught(new TooLongFrameException(
                        String.Format(
                                "Frame size exceeded on encode: frame was {0:d} bytes, maximum allowed is {1:d} bytes",
                                frameSize,
                                this._opts.MaxFrameSize)));
                return null;
            }

            switch (message.TransportType)
            {
                case ThriftTransportType.Unframed:
                    return message.Content;

                case ThriftTransportType.Framed:
                    var buffer = Unpooled.Buffer(this._opts.MessageFrameSize + message.Content.ReadableBytes, this._opts.MessageFrameSize + message.Content.ReadableBytes);
                    buffer.WriteInt(message.Content.ReadableBytes);
                    buffer.WriteBytes(message.Content, message.Content.ReadableBytes);
                    return buffer;
                //return Buffers.WrappedBuffer(context.Allocator, buffer, message.Buffer);                

                default:
                    throw new NotSupportedException("Unrecognized transport type");
            }
        }

        protected override void Encode(IChannelHandlerContext context, ThriftMessage message, IByteBuffer output)
        {
            Guard.ArgumentNotNull(context, nameof(context));
            Guard.ArgumentNotNull(message, nameof(message));
            Guard.ArgumentNotNull(output, nameof(output));

            IByteBuffer buffer = null;

            try
            {
                if (context.Channel.IsWritable)
                {
                    buffer = this.Encode(context, message);
                    //output.Add(buffer);
                    output.WriteBytes(buffer);
                    //context.Channel.WriteAsync(buffer);
                    buffer = null; //如果执行成功，下面的 Release 将无效。
                }
            }
            finally
            {
                buffer?.Release();
            }
        }
    }
}
