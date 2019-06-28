using System;

namespace TestThriftServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            int port = 9090;
            if (args != null && args.Length > 0)
            {
                int.TryParse(args[0], out port);
            }

            new ThriftServer().Bind(port);
        }
    }
}
