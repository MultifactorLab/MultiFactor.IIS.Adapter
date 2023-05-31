using MultiFactor.IIS.Adapter.Core;
using System;
using System.DirectoryServices.Protocols;
using System.Net.NetworkInformation;

namespace MultiFactor.IIS.Adapter.Services.Ldap
{
    public class LdapConnectionAdapter : IDisposable
    {
        private readonly LdapConnection _connection;
        public FullyQualifiedDomainName Domain { get; }

        private LdapConnectionAdapter(LdapConnection connection, FullyQualifiedDomainName domain)
        {
            _connection = connection;
            Domain = domain;
        }

        public static LdapConnectionAdapter Create()
        {
            var domain = IPGlobalProperties.GetIPGlobalProperties().DomainName;
            var conn = new LdapConnection(domain);

            conn.SessionOptions.RootDseCache = true;
            conn.SessionOptions.ProtocolVersion = 3;
            conn.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
            conn.Bind(); //as current user

            return new LdapConnectionAdapter(conn, new FullyQualifiedDomainName(domain));
        }

        public SearchResponse Search(string baseDn, string filter, SearchScope scope, params string[] attributes)
        {
            if (string.IsNullOrEmpty(baseDn))
            {
                throw new ArgumentException($"'{nameof(baseDn)}' cannot be null or empty.", nameof(baseDn));
            }

            if (string.IsNullOrEmpty(filter))
            {
                throw new ArgumentException($"'{nameof(filter)}' cannot be null or empty.", nameof(filter));
            }

            if (attributes is null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            var searchRequest = new SearchRequest(baseDn, filter, scope, attributes);

            return (SearchResponse)_connection.SendRequest(searchRequest);
        }

        public void Dispose()
        {
            _connection?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}