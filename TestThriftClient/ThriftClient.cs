using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace TestThriftClient
{
    public class ThriftClient
    {
        public void Connect(int port, string host)
        {
            IEventLoopGroup group = new MultithreadEventLoopGroup();
            try
            {
                Bootstrap b = new Bootstrap();
                b.Group(group)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Handler(new ThriftClientHandler());
                var f = b.ConnectAsync(host, port).Result;
                Console.WriteLine("Press any key exist.");
                Console.ReadKey();
                f.CloseAsync().Wait();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            finally
            {
                group.ShutdownGracefullyAsync();
            }
        }
    }
}
