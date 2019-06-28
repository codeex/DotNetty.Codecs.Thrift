using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Codecs.Thrift;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace TestThriftClient
{
    public class ThriftClientHandler : ChannelInitializer<TcpSocketChannel>
    {
        protected override void InitChannel(TcpSocketChannel channel)
        {
            channel.Pipeline.AddLast("thrift-frm-dec", new ThriftFrameDecoder());
            channel.Pipeline.AddLast("thrift-msg-dec", new ThriftMessageDecoder());
            channel.Pipeline.AddLast(new ThriftClientHandlerAdapter());
        }
    }

    public class ThriftClientHandlerAdapter : ChannelHandlerAdapter
    {

        //public FrameThriftMessage WriteMessage()
        //{
        //    FrameThriftMessage msg = new FrameThriftMessage(ThriftTransportType.Framed,
        //        Unpooled.WrappedBuffer(new byte[2048]));

        //    var str = "{\"m\":\"GetInfo\",\"v\":{\"ModelName\":\"InspectionCertificateDetail\",\"SysId\":\"OMS\",\"TemplateGuid\":\"\"},\"lg\":\"zh_cn\",\"tk\":\"adc05daa-8b06-4f8a-8ce7-d2e7aaf69a34\"}";
        //    msg.WriteMessageBegin(new TMessage("Send", TMessageType.Call, 0));
        //    TStruct struc = new TStruct("Send_args");
        //    msg.WriteStructBegin(struc);
        //    TField field = new TField();
        //    field.Name = "my_args";
        //    field.Type = TType.String;
        //    field.ID = 1;
        //    msg.WriteFieldBegin(field);
        //    msg.WriteString(str);
        //    msg.WriteFieldEnd();
        //    msg.WriteFieldStop();
        //    msg.WriteStructEnd();
        //    msg.WriteMessageEnd();
        //    return msg;
        //}
        //readonly IByteBuffer initialMessage;

        public ThriftClientHandlerAdapter()
        {
        }

        //重写基类方法，当链接上服务器后，马上发送Hello World消息到服务端
        public override void ChannelActive(IChannelHandlerContext context)
        {
            context.WriteAndFlushAsync("");
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {

            Console.WriteLine("Received from server: ");
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {

            string Success = string.Empty;

            context.Flush();
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Console.WriteLine("Exception: " + exception);
            context.CloseAsync();
        }
    }

}
