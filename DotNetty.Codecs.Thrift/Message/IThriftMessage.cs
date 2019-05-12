using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;

namespace DotNetty.Codecs.Thrift.Message
{
    /// <summary>
    /// Thrift message 基类
    /// </summary>
    public interface IThriftMessage : IByteBufferHolder
    {
    }
}
