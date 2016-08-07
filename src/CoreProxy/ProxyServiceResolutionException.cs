using System;

namespace jnm2.CoreProxy
{
    internal sealed class ProxyServiceResolutionException : Exception
    {
        public ProxyServiceResolutionException(string message) : base(message)
        {
        }
    }
}