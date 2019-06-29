using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using DotNetty.Codecs.Thrift;
using DotNetty.Codecs.Thrift.Protocol;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace TestThriftServer
{
    public class ThriftServer
    {
        public void Bind(int port)
        {
            //配置服务端线程组
            IEventLoopGroup bossGroup = new MultithreadEventLoopGroup();
            IEventLoopGroup workerGroup = new MultithreadEventLoopGroup();
            try
            {
                ServerBootstrap b = new ServerBootstrap();
                b.Group(bossGroup, workerGroup)
                    .Channel<TcpServerSocketChannel>()
                    .Option(ChannelOption.SoBacklog, 1024)
                    .ChildHandler(new ChildChannelHandler());
                //绑定端口，同步等待成功
                var f = b.BindAsync(port).Result;

                Console.ReadKey();
                //等待端口关闭
                f.CloseAsync().Wait();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            finally
            {
                //优雅退出，释放线程池资源
                bossGroup.ShutdownGracefullyAsync();
                workerGroup.ShutdownGracefullyAsync();
            }
        }

        private class ChildChannelHandler : ChannelInitializer<TcpSocketChannel>
        {
            protected override void InitChannel(TcpSocketChannel channel)
            {
                var pf = new TBinaryProtocol.Factory();
                channel.Pipeline.AddLast("thrift-msg-dec", new ThriftMessageDecoder(pf));
                channel.Pipeline.AddLast("thrift-msg-enc", new ThriftMessageEncoder());
                channel.Pipeline.AddLast(new ThriftServerHandler());
            }
        }
    }
}
