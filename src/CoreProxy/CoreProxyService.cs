using System;
using System.Collections.Generic;
using jnm2.CoreProxy.Config;
using jnm2.CoreProxy.Proxies;

namespace jnm2.CoreProxy
{
    public sealed class CoreProxyService : IDisposable
    {
        private readonly CoreProxyServiceConfiguration config;
        private readonly Action<string> log;
        private readonly List<IProxyService> proxyServices = new List<IProxyService>();

        public CoreProxyService(CoreProxyServiceConfiguration config, Action<string> log)
        {
            this.config = config;
            this.log = log;
        }

        public void StartAll()
        {
            foreach (var proxyConfig in config.Proxies)
            {
                var proxyLog = new Action<string>(message => log.Invoke($"{proxyConfig.Name}: {message}"));
                try
                {
                    var proxyService = ProxyServiceResolver.Create(proxyConfig.From, proxyConfig.To, proxyLog);
                    proxyServices.Add(proxyService);
                    proxyService.Start();
                }
                catch (Exception ex)
                {
                    proxyLog.Invoke(ex.Message);
                }
            }

            log.Invoke(proxyServices.Count == 1 ? "1 proxy started." : $"{proxyServices.Count} proxies started.");
        }

        public void Dispose()
        {
            foreach (var service in proxyServices)
                service.Dispose();
            proxyServices.Clear();
        }
    }
}
