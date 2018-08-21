using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Common;

namespace Codeex.Codecs.Protobuf.Message
{
    public class HeaderThriftMessage : IThriftMessage
    {
        public HeaderThriftMessage(ThriftTransportType transportType)
        {
            this.TransportType = transportType;
        }
        public IByteBuffer Payload { get; set; }

        public ThriftTransportType TransportType
        {
            get; private set;
        }
       
        public HeaderThriftMessage CreateMessage()
        {
            return new HeaderThriftMessage(this.TransportType);
        }
       
        public bool IsOrderedResponsesRequired()
        {
            return true;
        }

        public long ProcessStartTimeMillis { get; set; }

       public IReferenceCounted Retain()
        {
            throw new NotImplementedException();
        }

        public IReferenceCounted Retain(int increment)
        {
            throw new NotImplementedException();
        }

        public IReferenceCounted Touch()
        {
            throw new NotImplementedException();
        }

        public IReferenceCounted Touch(object hint)
        {
            throw new NotImplementedException();
        }

        public bool Release()
        {
            throw new NotImplementedException();
        }

        public bool Release(int decrement)
        {
            throw new NotImplementedException();
        }

        public int ReferenceCount { get; }
        public IByteBufferHolder Copy()
        {
            throw new NotImplementedException();
        }

        public IByteBufferHolder Duplicate()
        {
            throw new NotImplementedException();
        }

        public IByteBufferHolder RetainedDuplicate()
        {
            throw new NotImplementedException();
        }

        public IByteBufferHolder Replace(IByteBuffer content)
        {
            throw new NotImplementedException();
        }

        public IByteBuffer Content { get; }
    }
}
