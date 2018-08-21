using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Codecs;

namespace Codeex.Codecs.Protobuf
{
    public class ThriftCodecException : CodecException
    {
        public ThriftCodecException(Exception exception)
            : this(null, exception)
        {
        }

        public ThriftCodecException(string message, Exception exception = null)
            : base(message, exception)
        {
        }
    }
}
