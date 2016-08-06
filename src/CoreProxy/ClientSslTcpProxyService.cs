using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace jnm2.CoreProxy
{
    public class ClientSslTcpProxyService : TcpProxyService
    {
        private readonly X509Certificate certificate;

        public ClientSslTcpProxyService(IPEndPoint from, IPEndPoint to, X509Certificate certificate, Action<string> log) : base(from, to, log)
        {
            this.certificate = certificate;
        }

        protected override async Task<Stream> TryGetClientStream(NetworkStream tcpStream)
        {
            var r = new SslStream(tcpStream);
            try
            {
                await r.AuthenticateAsServerAsync(certificate);
            }
            catch (IOException ex) // Client auth issue
            {
                Log.Invoke(ex.Message);
                return null;
            }
            return r;
        }
    }
}
