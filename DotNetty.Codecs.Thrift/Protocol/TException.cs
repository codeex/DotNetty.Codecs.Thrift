using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetty.Codecs.Thrift.Protocol
{
    public class TException : Exception
    {
        public TException()
        {
        }

        public TException(string message)
            : base(message)
        {
        }

    }
}
