using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using jnm2.CoreProxy.Config;

namespace jnm2.CoreProxy.Proxies
{
    public class ClientTlsTcpProxyService : TcpProxyService
    {
        private readonly X509Certificate certificate;
        private readonly bool ownsCertificate;

        private static readonly Oid ServerAuthenticationKeyUsage = Oid.FromOidValue("1.3.6.1.5.5.7.3.1", OidGroup.EnhancedKeyUsage);

        public ClientTlsTcpProxyService(IPEndPoint from, IPEndPoint to, X509Certificate2 certificate, bool ownsCertificate, Action<string> log) : base(from, to, log)
        {
            var localTime = DateTime.Now;
            if (localTime < certificate.NotBefore)
            {
                if (ownsCertificate) certificate.Dispose();
                throw new InvalidConfigException($"The certificate cannot be used until {certificate.NotBefore}.");
            }
            if (certificate.NotAfter < localTime)
            {
                if (ownsCertificate) certificate.Dispose();
                throw new InvalidConfigException($"The certificate cannot be used after {certificate.NotAfter}.");
            }
            if (!certificate.IsExtendedKeyUsageAllowed(ServerAuthenticationKeyUsage))
            {
                if (ownsCertificate) certificate.Dispose();
                throw new InvalidConfigException("The certificate cannot be used for server authentication.");
            }
            if (!certificate.HasPrivateKey)
            {
                if (ownsCertificate) certificate.Dispose();
                throw new InvalidConfigException("The certificate must contain the private key.");
            }

            this.certificate = certificate;
            this.ownsCertificate = ownsCertificate;
        }
        public ClientTlsTcpProxyService(IPEndPoint from, IPEndPoint to, string certificateFriendlyName, Action<string> log) : this(from, to, LoadCertificate(certificateFriendlyName), true, log)
        {
        }

        private static X509Certificate2 LoadCertificate(string certificateFriendlyName)
        {
            X509Certificate2 certificate;
            using (var store = new X509Store("My", StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                try
                {
                    certificate = store.Certificates.GetSingleByFriendlyName(certificateFriendlyName);
                }
                catch (InvalidOperationException)
                {
                    throw new InvalidConfigException($"Multiple certificates in Local Machine/My have the the friendly name \"{certificateFriendlyName}\".");
                }
                if (certificate == null) throw new InvalidConfigException($"Cannot find certificate in Local Machine/My with the friendly name \"{certificateFriendlyName}\".");
            }

            return certificate;
        }

        protected override async Task<Stream> TryGetClientStream(NetworkStream tcpStream)
        {
            var r = new SslStream(tcpStream, false, UserCertificateValidationCallback);
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

        private bool UserCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true; // We are the server with the private key, we inherently trust our config even if our cert is self-signed or not in the system trust list.
        }

        protected override string StartLogMessage() => $"Proxying all TLS traffic from {Listener.LocalEndpoint} to TCP at {To}.";

        public override void Dispose()
        {
            base.Dispose();
            if (ownsCertificate) certificate.Dispose();
        }
    }
}
