using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Codecs;

namespace Codeex.Codecs.Protobuf
{
    public class ThriftDecoderException : DecoderException
    {
        public ThriftDecoderException(Exception exception)
            : this(null, exception)
        {
        }

        public ThriftDecoderException(string message, Exception exception = null)
            : base(message, exception)
        {
        }
    }
}
