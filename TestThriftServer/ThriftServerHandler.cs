using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Codecs.Thrift.Message;
using DotNetty.Codecs.Thrift.Protocol;
using DotNetty.Codecs.Thrift.Transport;
using DotNetty.Transport.Channels;

namespace TestThriftServer
{
    internal class ThriftServerHandler : ChannelHandlerAdapter
    {
        public ThriftServerHandler()
        {
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var msg = (ThriftMessage)message;
            var bp = new TBinaryProtocol.Factory();
            var t = new TThriftTransport(context.Channel, msg.Content, ThriftTransportType.Framed);
            var tp = bp.GetProtocol(t);
            var mh = tp.ReadMessageBegin();
           
            tp.ReadMessageEnd();
            var buf = msg.Content;
            byte[] req = new byte[buf.ReadableBytes];
            buf.ReadBytes(req);
            //ReadOnlySpan<byte> bytesBuffer = req;
            //ReadOnlySpan<sbyte> sbytesBuffer = MemoryMarshal.Cast<byte, sbyte>(bytesBuffer);
            //sbyte[] signed = sbytesBuffer.ToArray();
            string body = Encoding.UTF8.GetString(req);
            Trace.WriteLine($"recv order: {body}");
            string currentTime = string.Compare(body, "query time", true) == 0 ? DateTime.Now.ToLongTimeString() : "bad order";
            IByteBuffer resp = Unpooled.CopiedBuffer(currentTime, Encoding.UTF8);
            context.WriteAsync(resp);
            //string body = new string(signed, 0, req.Length, Encoding.UTF8);
            //base.ChannelRead(context, message);
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            context.Flush();
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            //base.ExceptionCaught(context, exception);
            context.CloseAsync();
        }

    }
}