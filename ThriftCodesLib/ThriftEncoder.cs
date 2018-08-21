using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using Codeex.Codecs.Protobuf.Message;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;

namespace Codeex.Codecs.Protobuf
{
    public class ThriftEncoder : MessageToMessageEncoder<IThriftMessage>
    {
        public int MaxFrameSize { get; set; }
        public ThriftEncoder(int maxFrameSize)
        {
            this.MaxFrameSize = maxFrameSize;
        }
        protected override void Encode(IChannelHandlerContext context, IThriftMessage message, List<object> output)
        {
            Contract.Requires(context != null);
            Contract.Requires(message != null);
            Contract.Requires(output != null);
            try
            {
                this.WriteThriftMessage(context.Allocator, message, output);
            }
            catch (CodecException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new CodecException(exception);
            }
        }

        void WriteThriftMessage(IByteBufferAllocator allocator, IThriftMessage message, List<object> output)
        {
            int frameSize = message.Content.ReadableBytes;

            if (frameSize > this.MaxFrameSize)
            {
                throw new TooLongFrameException($"Frame size exceeded on encode: frame was {frameSize} bytes, maximum allowed is {this.MaxFrameSize} bytes");
            }
            if (message is FrameThriftMessage frameThriftMessage)
            {
                WriteFrameMessage(allocator, frameThriftMessage, output);
            }
            else if (message is UnframeThriftMessage unframeThriftMessage)
            {
                WriteUnFrameMessage(allocator, unframeThriftMessage, output);
            }
            else if (message is HeaderThriftMessage headerThriftMessage)
            {
                WriteHeaderMessage(allocator, headerThriftMessage, output);
            }
            else if (message is HttpThriftMessage httpThriftMessage)
            {
                WriteHttpFrameMessage(allocator, httpThriftMessage, output);
            }
            else
            {
                throw new CodecException($"Unknown message type: {message}");
            }
        }

        static void WriteFrameMessage(IByteBufferAllocator allocator, FrameThriftMessage msg, List<object> output)
        {
            IByteBuffer buf = null;
            try
            {
                var len = 4 + msg.Content.ReadableBytes;
                buf = allocator.Buffer(len);
                buf.WriteInt(msg.Content.ReadableBytes);
                buf.WriteBytes(msg.Content);
                output.Add(buf.Copy(0,len));
            }
            finally
            {
                buf?.Release();
            }
        }
        static void WriteUnFrameMessage(IByteBufferAllocator allocator, UnframeThriftMessage msg, List<object> output)
        {
            output.Add(msg.Content);
        }
        static void WriteHttpFrameMessage(IByteBufferAllocator allocator, HttpThriftMessage msg, List<object> output)
        {
            throw new NotImplementedException();
        }
        static void WriteHeaderMessage(IByteBufferAllocator allocator, HeaderThriftMessage msg, List<object> output)
        {
            throw new NotImplementedException();
        }
    }
}
