using System;
using System.Collections.Generic;
using jnm2.CoreProxy.Config;
using jnm2.CoreProxy.Proxies;

namespace jnm2.CoreProxy
{
    internal static class ProxyServiceResolver
    {
        private delegate IProxyService ProxyServiceFactory(IEndPointConfiguration from, IEndPointConfiguration to, Action<string> log);

        private static readonly Dictionary<Tuple<Type, Type>, ProxyServiceFactory> ServiceFactoriesByProtocolPair = new Dictionary<Tuple<Type, Type>, ProxyServiceFactory>
        {
            [Tuple.Create(typeof(TcpConfiguration), typeof(TcpConfiguration))] = (from, to, log) => new TcpProxyService(from.EndPoint, to.EndPoint, log),
            [Tuple.Create(typeof(TlsConfiguration), typeof(TcpConfiguration))] = (from, to, log) => new ClientTlsTcpProxyService(from.EndPoint, to.EndPoint, ((TlsConfiguration)from).Certificate, log)
        };

        public static IProxyService Create(IEndPointConfiguration from, IEndPointConfiguration to, Action<string> log)
        {
            ProxyServiceFactory serviceFactory;
            if (!ServiceFactoriesByProtocolPair.TryGetValue(Tuple.Create(from.GetType(), to.GetType()), out serviceFactory))
                throw new ProxyServiceResolutionException($"No proxy service is registered from {from.GetType().Name} to {to.GetType().Name}.");
            return serviceFactory.Invoke(from, to, log);
        }
    }
}