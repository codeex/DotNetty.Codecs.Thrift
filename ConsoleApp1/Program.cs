using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Codeex.Codecs.Protobuf;
using Codeex.Codecs.Protobuf.Message;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Thrift.Protocol;
using Thrift.Transport;
using TMessageType = Thrift.Protocol.TMessageType;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            RunClientAsync().Wait();
        }

        static async Task RunClientAsync()
        {
            var group = new MultithreadEventLoopGroup();

            string targetHost = null;
            try
            {
                var bootstrap = new Bootstrap();
                bootstrap
                    .Group(group)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;

                        pipeline.AddLast(new LoggingHandler());
                        //pipeline.AddLast("frameDecoder", new LengthFieldBasedFrameDecoder(1024 * 1024, 0, 4, 0, 4));
                        pipeline.AddLast("thrift-enc", new ThriftEncoder(1024 * 1024));
                        pipeline.AddLast("thrift-dec", new ThriftDecoder(1024 * 1024, true));

                        pipeline.AddLast("echo", new EchoClientHandler());
                    }));

                IPAddress address = IPAddress.Parse("192.168.100.21");
                IChannel clientChannel = await bootstrap.ConnectAsync(new IPEndPoint(address, 20001));

                Console.ReadLine();
                await clientChannel.CloseAsync();

            }
            finally
            {
                await group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            }
        }
    }


    public class EchoClientHandler : ChannelHandlerAdapter
    {

        public FrameThriftMessage WriteMessage()
        {
            FrameThriftMessage msg = new FrameThriftMessage(ThriftTransportType.Framed,
                Unpooled.WrappedBuffer(new byte[2048]));

            var str = "{\"m\":\"GetInfo\",\"v\":{\"ModelName\":\"InspectionCertificateDetail\",\"SysId\":\"OMS\",\"TemplateGuid\":\"\"},\"lg\":\"zh_cn\",\"tk\":\"adc05daa-8b06-4f8a-8ce7-d2e7aaf69a34\"}";
            msg.WriteMessageBegin(new TMessage("Send", TMessageType.Call, 0));
            TStruct struc = new TStruct("Send_args");
            msg.WriteStructBegin(struc);
            TField field = new TField();
            field.Name = "my_args";
            field.Type = TType.String;
            field.ID = 1;
            msg.WriteFieldBegin(field);
            msg.WriteString(str);
            msg.WriteFieldEnd();
            msg.WriteFieldStop();
            msg.WriteStructEnd();
            msg.WriteMessageEnd();
            return msg;
        }
        readonly IByteBuffer initialMessage;

        public EchoClientHandler()
        {

            var msg = WriteMessage();
            this.initialMessage = Unpooled.Buffer(1024);
            this.initialMessage.WriteBytes(msg.Content.Copy(0, msg.Content.ReadableBytes));
        }

        //重写基类方法，当链接上服务器后，马上发送Hello World消息到服务端
        public override void ChannelActive(IChannelHandlerContext context)
        {
            context.WriteAndFlushAsync(this.initialMessage);
        }

        private List<IByteBuffer> msgs = new List<IByteBuffer>();
        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var byteBuffer = message as FrameThriftMessage;
            if (byteBuffer != null)
            {
                string success = string.Empty;
                var msg = byteBuffer.ReadMessageBegin();
                TField field;
                byteBuffer.ReadStructBegin();
                while (true)
                {
                    field = byteBuffer.ReadFieldBegin();
                    if (field.Type == TType.Stop)
                    {
                        break;
                    }
                    switch (field.ID)
                    {
                        case 0:
                            if (field.Type == TType.String)
                            {
                                success = byteBuffer.ReadString();
                            }
                            else
                            {
                                byteBuffer.Skip(field.Type);
                            }
                            break;

                        default:
                            byteBuffer.Skip(field.Type);
                            break;
                    }
                    byteBuffer.ReadFieldEnd();
                }
                byteBuffer.ReadStructEnd();

                byteBuffer.ReadMessageEnd();

                Console.WriteLine("Received from server: " + success);

            }
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
