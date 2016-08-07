using System;

namespace jnm2.CoreProxy.Proxies
{
    public interface IProxyService : IDisposable
    {
        void Start();
    }
}