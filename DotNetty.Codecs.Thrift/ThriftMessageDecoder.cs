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
    /// 按照 TBinaryProtocol ， 解析 Thrift 消息
    /// </summary>
    public class ThriftMessageDecoder : MessageToMessageDecoder<IByteBuffer>
    {
        private ThriftOptions _opts;
        public ThriftMessageDecoder(ThriftOptions options = null)
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

            try
            {                
                output.Add(new ThriftBinaryMessage((IByteBuffer)message.Retain()));
            }
            catch (Exception exception)
            {
                throw new CodecException(exception);
            }           
        }
    }
}
