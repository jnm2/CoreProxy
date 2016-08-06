namespace jnm2.CoreProxy.Console
{
    using System;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using Console = System.Console;

    public class Program
    {
        public static void Main(string[] args)
        {
            var from = new IPEndPoint(IPAddress.Any, 1234);
            var to = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 32179);
            
            using (var service = new ClientSslTcpProxyService(from, to, new X509Certificate("test.pfx", "test"), Console.WriteLine))
            {
                service.Start();
                Console.WriteLine($"Proxying all TCP traffic from {from} to {to}.");
                Console.WriteLine("Press Q to quit.");

                while (Console.ReadKey(true).Key != ConsoleKey.Q) ;
            }
        }
    }
}
