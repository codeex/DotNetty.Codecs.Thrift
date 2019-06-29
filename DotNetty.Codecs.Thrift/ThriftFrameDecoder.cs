// Copyright (c) CodeEx.cn & webmote. All rights reserved.
// Licensed under theApache License. 
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Codecs.Thrift.Message;
using DotNetty.Codecs.Thrift.Protocol;
using DotNetty.Codecs.Thrift.Transport;
using DotNetty.Transport.Channels;
using Thrift.Transport;

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
    /// or UnFrame Message
    ///  Thrift Data      ----->  探索发现结束
    ///
    ///
    public class ThriftMessageDecoder : ByteToMessageDecoder
    {        
        private ThriftOptions _opts;
        private readonly TProtocolFactory _inputProtocolFactory;
        // todo: maxFrameLength + safe skip + fail-fast option (just like LengthFieldBasedFrameDecoder)
        public ThriftMessageDecoder(TProtocolFactory inputProtocolFactory,ThriftOptions options = null)
        {
            this._opts = options ?? ThriftOptions.DefaultOptions;
            this._inputProtocolFactory = inputProtocolFactory;
        }

        protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            Guard.ArgumentNotNull(context, nameof(context));
            Guard.ArgumentNotNull(input, nameof(input));
            Guard.ArgumentNotNull(output, nameof(output));

            var decoded = this.Decode(context, input);

            if (decoded != null)
            {
                output.Add(decoded);
            }            
        }


        protected  ThriftMessage Decode(IChannelHandlerContext ctx, IByteBuffer buffer)
        {
            if (!buffer.IsReadable())
            {
                return null;
            }

            ushort firstByte = buffer.GetUnsignedShort(0);
            if (firstByte >= 0x80)
            {
                IByteBuffer messageBuffer = this.TryDecodeUnframedMessage(ctx, ctx.Channel, buffer, _inputProtocolFactory);

                if (messageBuffer == null)
                {
                    return null;
                }
                // A non-zero MSB for the first byte of the message implies the message starts with a
                // protocol id (and thus it is unframed).
                return new ThriftMessage(messageBuffer, ThriftTransportType.Unframed);
            }
            else if (buffer.ReadableBytes < this._opts.MessageFrameSize)
            {
                // Expecting a framed message, but not enough bytes available to read the frame size
                return null;
            }
            else
            {
                IByteBuffer messageBuffer = this.TryDecodeFramedMessage(ctx, ctx.Channel, buffer, true);

                if (messageBuffer == null)
                {
                    return null;
                }
                // Messages with a zero MSB in the first byte are framed messages
                return new ThriftMessage(messageBuffer, ThriftTransportType.Framed);
            }
        }

        protected IByteBuffer TryDecodeFramedMessage(IChannelHandlerContext ctx,
                                                   IChannel channel,
                                                   IByteBuffer buffer,
                                                   bool stripFraming)
        {
            // Framed messages are prefixed by the size of the frame (which doesn't include the
            // framing itself).

            int messageStartReaderIndex = buffer.ReaderIndex;
            int messageContentsOffset;

            if (stripFraming)
            {
                messageContentsOffset = messageStartReaderIndex + this._opts.MessageFrameSize;
            }
            else
            {
                messageContentsOffset = messageStartReaderIndex;
            }

            // The full message is larger by the size of the frame size prefix
            int messageLength = buffer.GetInt(messageStartReaderIndex) + this._opts.MessageFrameSize;
            int messageContentsLength = messageStartReaderIndex + messageLength - messageContentsOffset;

            if (messageContentsLength > this._opts.MaxFrameSize)
            {
                ctx.FireExceptionCaught(
                        new TooLongFrameException("Maximum frame size of " + this._opts.MaxFrameSize +
                                                  " exceeded")
                );
            }

            if (messageLength == 0)
            {
                // Zero-sized frame: just ignore it and return nothing
                buffer.SetReaderIndex(messageContentsOffset);
                return null;
            }
            else if (buffer.ReadableBytes < messageLength)
            {
                // Full message isn't available yet, return nothing for now
                return null;
            }
            else
            {
                // Full message is available, return it
                IByteBuffer messageBuffer = ExtractFrame(buffer,
                                                           messageContentsOffset,
                                                           messageContentsLength);
                buffer.SetReaderIndex(messageStartReaderIndex + messageLength);
                return messageBuffer;
            }
        }

        protected IByteBuffer TryDecodeUnframedMessage(IChannelHandlerContext ctx,
                                                     IChannel channel,
                                                     IByteBuffer buffer,
                                                     TProtocolFactory inputProtocolFactory)
        {
            // Perform a trial decode, skipping through
            // the fields, to see whether we have an entire message available.

            int messageLength = 0;
            int messageStartReaderIndex = buffer.ReaderIndex;

            try
            {
                using (TThriftTransport decodeAttemptTransport = new TThriftTransport(channel, buffer, ThriftTransportType.Unframed))
                {
                    int initialReadBytes = decodeAttemptTransport.GetReadByteCount();
                    using (TProtocol inputProtocol =
                            inputProtocolFactory.GetProtocol(decodeAttemptTransport))
                    {

                        // Skip through the message
                        inputProtocol.ReadMessageBegin();
                        TProtocolUtil.Skip(inputProtocol, TType.Struct);
                        inputProtocol.ReadMessageEnd();

                        messageLength = decodeAttemptTransport.GetReadByteCount() - initialReadBytes;
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                // No complete message was decoded: ran out of bytes
                return null;
            }
            catch (TTransportException)
            {
                // No complete message was decoded: ran out of bytes
                return null;
            }
            finally
            {
                if (buffer.ReaderIndex - messageStartReaderIndex > this._opts.MaxFrameSize)
                {
                    ctx.FireExceptionCaught(new TooLongFrameException("Maximum frame size of " + this._opts.MaxFrameSize + " exceeded"));
                }

                buffer.SetReaderIndex(messageStartReaderIndex);
            }

            if (messageLength <= 0)
            {
                return null;
            }

            // We have a full message in the read buffer, slice it off
            IByteBuffer messageBuffer =
                    ExtractFrame(buffer, messageStartReaderIndex, messageLength);
            buffer.SetReaderIndex(messageStartReaderIndex + messageLength);
            return messageBuffer;
        }

        protected IByteBuffer ExtractFrame(IByteBuffer buffer, int index, int length)
        {
            // Slice should be sufficient here (and avoids the copy in LengthFieldBasedFrameDecoder)
            // because we know no one is going to modify the contents in the read buffers.
            //防止 buffer 被释放，在
            buffer.Retain();
            return buffer.Slice(index, length);
        }
    }
}
