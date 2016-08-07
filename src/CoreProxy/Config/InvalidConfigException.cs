using System;

namespace jnm2.CoreProxy.Config
{
    public class InvalidConfigException : Exception
    {
        public InvalidConfigException(string message) : base(message)
        {
        }
    }
}
