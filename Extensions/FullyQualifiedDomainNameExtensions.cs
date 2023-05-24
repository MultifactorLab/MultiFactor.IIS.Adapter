using MultiFactor.IIS.Adapter.Core;
using System;
using System.Linq;

namespace MultiFactor.IIS.Adapter.Extensions
{
    internal static class FullyQualifiedDomainNameExtensions
    {
        /// <summary>
        /// Converts domain.local to DC=domain,DC=local.
        /// </summary>
        /// <param name="domain">Domain.</param>
        /// <returns>Distinguished Name</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string GetDn(this FullyQualifiedDomainName domain)
        {
            if (domain is null) throw new ArgumentNullException(nameof(domain));

            var name = domain.Value;
            var portIndex = domain.Value.IndexOf(":");
            if (portIndex > 0)
            {
                name = name.Substring(0, portIndex);
            }

            var domains = name.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            var dn = domains.Select(p => $"DC={p}").ToArray();

            return string.Join(",", dn);
        }
    }
}