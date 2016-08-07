using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace jnm2.CoreProxy
{
    public static class X509Extensions
    {
        public static X509Certificate2 GetSingleByFriendlyName(this X509Certificate2Collection collection, string friendlyName)
        {
            var found = (X509Certificate2)null;

            foreach (var certificate in collection)
            {
                if (string.Equals(friendlyName, certificate.FriendlyName, StringComparison.OrdinalIgnoreCase))
                {
                    if (found == null)
                    {
                        found = certificate;
                    }
                    else
                    {
                        found.Dispose();
                        certificate.Dispose();
                        throw new InvalidOperationException($"Multiple certificates were found with the name {friendlyName}.");
                    }
                }
                else
                {
                    certificate.Dispose();
                }
            }

            return found;
        }

        public static bool IsExtendedKeyUsageAllowed(this X509Certificate2 certificate, Oid extendedKeyUsage, bool allowNonCritical = false)
        {
            var enhancedKeyUsage = certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>().SingleOrDefault();
            return enhancedKeyUsage == null || (allowNonCritical && !enhancedKeyUsage.Critical) || enhancedKeyUsage.EnhancedKeyUsages[extendedKeyUsage.Value] != null;
        }
    }
}
