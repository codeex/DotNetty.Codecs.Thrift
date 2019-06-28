using System;

namespace TestThriftClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello Thrift!");
            int port = 8080;
            if (args != null && args.Length > 0)
            {
                int.TryParse(args[0], out port);
            }
            new ThriftClient().Connect(port, "127.0.0.1");
        }
    }
}
