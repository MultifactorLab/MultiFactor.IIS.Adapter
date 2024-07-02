using MultiFactor.IIS.Adapter.Core;
using System;
using System.DirectoryServices.Protocols;

namespace MultiFactor.IIS.Adapter.Services.Ldap
{
    public class LdapConnectionAdapter : IDisposable
    {
        private readonly LdapConnection _connection;
        public FullyQualifiedDomainName Domain { get; }
        private readonly Logger _logger;

        private LdapConnectionAdapter(LdapConnection connection, FullyQualifiedDomainName domain, Logger logger)
        {
            _connection = connection;
            Domain = domain;
            _logger = logger;
        }

        public static LdapConnectionAdapter Create(string domain, Logger logger)
        {
            if (string.IsNullOrWhiteSpace(domain))
            {
                throw new ArgumentNullException(nameof(domain));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            var adDomain = System.DirectoryServices.ActiveDirectory.Domain.GetComputerDomain().Name;
            logger.Info($"Creating ldap connection to server {domain}");
            var conn = new LdapConnection(domain);

            conn.SessionOptions.RootDseCache = true;
            conn.SessionOptions.ProtocolVersion = 3;
            conn.SessionOptions.ReferralChasing = ReferralChasingOptions.None;

            logger.Info($"Binding current user to connection for server {domain}");
            conn.Bind(); //as current user

            return new LdapConnectionAdapter(conn, new FullyQualifiedDomainName(domain), logger);
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

            _logger.Info($"Sending search request with params:\r\nbase={baseDn}\r\nfilter={filter}\r\nscope={scope}\r\nattributes={string.Join(";", attributes)}");
            return (SearchResponse)_connection.SendRequest(searchRequest);
        }

        public void Dispose()
        {
            _connection?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}