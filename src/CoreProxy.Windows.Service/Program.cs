namespace CoreProxy.Windows.Service
{
    public static class Program
    {
        public static void Main() => System.ServiceProcess.ServiceBase.Run(new CoreProxyService());
    }
}
