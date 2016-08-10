using System;
using System.Collections;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;

namespace CoreProxy.Windows.Service
{
    public static class SelfServiceInstaller
    {
        public static bool HandleDefault(Assembly assembly, string[] commandLineArgs)
        {
            if (commandLineArgs.Length == 0) return false;
            
            switch (commandLineArgs[0])
            {
                case "-i":
                    Install(assembly, commandLineArgs.Skip(1).ToArray());
                    return true;
                case "-u":
                    Uninstall(assembly, commandLineArgs.Skip(1).ToArray());
                    return true;
                default:
                    Console.WriteLine("Usage: -i to install, -u to uninstall.");
                    return true;
            }
        }

        public static void Install(Assembly assembly, string[] args) => Install(false, assembly, args);
        public static void Uninstall(Assembly assembly, string[] args) => Install(true, assembly, args);

        private static void Install(bool uninstall, Assembly assembly, string[] args)
        {
            Console.WriteLine(uninstall ? "Uninstalling..." : "Installing...");
            using (var inst = new AssemblyInstaller(assembly, args) { UseNewContext = true })
            {
                var state = new Hashtable();
                try
                {
                    if (uninstall)
                    {
                        inst.Uninstall(state);
                    }
                    else
                    {
                        inst.Install(state);
                        inst.Commit(state);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Installation error: " + ex.Message);
                    Console.WriteLine("Rolling back...");
                    try
                    {
                        inst.Rollback(state);
                    }
                    catch (Exception rollbackEx)
                    {
                        Console.WriteLine("Rollback error: " + rollbackEx.Message);
                    }
                    throw;
                }
            }

            Console.WriteLine("Successfully completed.");
        }
    }
}
