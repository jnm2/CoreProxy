using System;

namespace CoreProxy.Windows.Service
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (SelfServiceInstaller.HandleDefault(typeof(Program).Assembly, args)) return;

            System.ServiceProcess.ServiceBase.Run(new CoreProxyService());
        }
    }
}
