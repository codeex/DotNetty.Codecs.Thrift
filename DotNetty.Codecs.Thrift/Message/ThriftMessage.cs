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
    public class ThriftMessage : DefaultByteBufferHolder, IThriftMessage
    {
        public ThriftMessage(IByteBuffer content , ThriftTransportType transportType)
            : base(content)
        {
            this.TransportType = transportType;
        }


        public ThriftTransportType TransportType { get; }

        public override string ToString() =>
           new StringBuilder(StringUtil.SimpleClassName(this))
               .Append('[')
               .Append("content=")
               .Append(this.Content)
               .Append(']')
               .ToString();
        public override IByteBufferHolder Replace(IByteBuffer content) => new ThriftMessage(content, this.TransportType);

        /// <summary>
        ///Standard Thrift clients require ordered responses, so even though Nifty can run multiple
        ///requests from the same client at the same time, the responses have to be held until all
        ///previous responses are ready and have been written.However, through the use of extended
        ///protocols and codecs, a request can indicate that the client understands
        ///out-of-order responses.
        /// </summary>
        /// <returns></returns>
        public bool IsOrderedResponsesRequired
        {
            get { return true; }
        }

        public long ProcessStartTimeTicks { get; set; }

    }
}
