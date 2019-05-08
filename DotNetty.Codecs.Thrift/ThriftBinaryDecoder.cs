// Copyright (c) CodeEx.cn & webmote. All rights reserved.
// Licensed under theApache License. 
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Thrift.Protocol;
using Thrift.Transport;

namespace DotNetty.Codecs.Thrift
{
    /// <summary>
    /// 按照 TBinaryProtocol ， 解析 Thrift 消息
    /// </summary>
    public class ThriftBinaryDecoder : MessageToMessageDecoder<IByteBuffer>
    {
        private ThriftOptions _opts;
        public ThriftBinaryDecoder(ThriftOptions options = null)
        {
            _opts = options ?? ThriftOptions.DefaultOptions;
        }
        protected override void Decode(IChannelHandlerContext context, IByteBuffer message, List<object> output)
        {
            Contract.Requires(context != null);
            Contract.Requires(message != null);
            Contract.Requires(output != null);

            int length = message.ReadableBytes;
            if (length <= 0)
            {
                return;
            }

            Stream inputStream = null;
            try
            {
                TBinaryProtocol protocol = new TBinaryProtocol(new TMemoryBufferClientTransport());
                if (message.IoBufferCount == 1)
                {
                    ArraySegment<byte> bytes = message.GetIoBuffer(message.ReaderIndex, length);
                    codedInputStream = CodedInputStream.CreateInstance(bytes.Array, bytes.Offset, length);
                }
                else
                {
                    inputStream = new ReadOnlyByteBufferStream(message, false);
                    codedInputStream = CodedInputStream.CreateInstance(inputStream);
                }
                 

                IBuilderLite newBuilder = this.protoType.WeakCreateBuilderForType();
                IBuilderLite messageBuilder = this.extensionRegistry == null
                    ? newBuilder.WeakMergeFrom(codedInputStream)
                    : newBuilder.WeakMergeFrom(codedInputStream, this.extensionRegistry);

                IMessageLite decodedMessage = messageBuilder.WeakBuild();
                if (decodedMessage != null)
                {
                    output.Add(decodedMessage);
                }
            }
            catch (Exception exception)
            {
                throw new CodecException(exception);
            }
            finally
            {
                inputStream?.Dispose();
            }
        }
    }
}
