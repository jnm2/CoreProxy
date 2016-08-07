using System.Collections.Generic;
using System.Net;

namespace jnm2.CoreProxy.Config
{
    public sealed class CoreProxyServiceConfiguration
    {
        public IList<ProxyConfiguration> Proxies { get; } = new List<ProxyConfiguration>();

    }

    public sealed class ProxyConfiguration
    {
        public string Name { get; set; }
        public IEndPointConfiguration From { get; set; }
        public IEndPointConfiguration To { get; set; }
    }

    public interface IEndPointConfiguration
    {
        IPEndPoint EndPoint { get; set; }
    }

    public class TcpConfiguration : IEndPointConfiguration
    {
        public IPEndPoint EndPoint { get; set; }
    }

    public class TlsConfiguration : TcpConfiguration
    {
        public string Certificate { get; set; }
    }
}
