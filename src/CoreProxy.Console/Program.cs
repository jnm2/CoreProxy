using System.IO;
using System.Linq;
using jnm2.CoreProxy.Config;

namespace jnm2.CoreProxy.Console
{
    using System;
    using Console = System.Console;

    public class Program
    {
        public static void Main(string[] args)
        {
            string configPath;
            if (args.Length > 1 || !File.Exists(configPath = args.FirstOrDefault() ?? "CoreProxy.config.json"))
            {
                Console.WriteLine("Usage: CoreProxy.Console [path/to/config.json]");
                Console.WriteLine("If no path is specified, CoreProxy.config.json will be used.");
                return;
            }

            var config = new JsonConfigProvider(configPath).Load();
            
            using (var service = new CoreProxyService(config, Console.WriteLine))
            {
                service.StartAll();

                Console.WriteLine("Press Q to quit.");
                while (Console.ReadKey(true).Key != ConsoleKey.Q) ;
            }
        }
    }
}
