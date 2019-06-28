using System;
using System.Collections.Generic;
using System.Text;
using ThriftSharp.Models;

namespace DotNetty.Codecs.Thrift.Message
{
    /// <summary>
    /// 描述Thrift 消息
    /// </summary>
    public class ThriftMessage
    {
        public ThriftMessageHeader Header { get; set; }
    }
}
