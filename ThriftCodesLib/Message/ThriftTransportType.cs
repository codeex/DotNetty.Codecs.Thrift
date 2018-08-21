using System;
using System.Collections.Generic;
using System.Text;

namespace Codeex.Codecs.Protobuf.Message
{
    /// <summary>
    /// Thrift Transport Type
    /// </summary>
    public enum ThriftTransportType
    {
        Unframed,
        Framed,
        Http,
        Header
    }
}
