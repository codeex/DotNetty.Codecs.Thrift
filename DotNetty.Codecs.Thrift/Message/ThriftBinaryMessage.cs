using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;

namespace DotNetty.Codecs.Thrift.Message
{
    /// <summary>
    /// TBinaryProtocol message 
    /// </summary>
    public class ThriftBinaryMessage : DefaultByteBufferHolder, IBinaryContent
    {
        public ThriftBinaryMessage(IByteBuffer content)
            : base(content)
        {
        }

        public override string ToString() =>
           new StringBuilder(StringUtil.SimpleClassName(this))
               .Append('[')
               .Append("content=")
               .Append(this.Content)
               .Append(']')
               .ToString();
        public override IByteBufferHolder Replace(IByteBuffer content) => new ThriftBinaryMessage(content);

    }
}
