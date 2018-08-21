using System;
using System.Collections.Generic;
using System.Text;

namespace Codeex.Codecs.Protobuf.Message
{
    public enum TMessageType
    {
        Call = 1,
        Reply = 2,
        Exception = 3,
        Oneway = 4
    }
}
