using System;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net.NetworkInformation;

namespace MultiFactor.IIS.Adapter.Services
{
    /// <summary>
    /// Simple AD client
    /// </summary>
    public class ActiveDirectoryService
    {
        private CacheService _cache;

        public ActiveDirectoryService(CacheService cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// Only group members should 2fa
        /// </summary>
        public bool ValidateMembership(string samAccountName)
        {
            var cachedMembership = _cache.GetMembership(samAccountName);
            if (cachedMembership != null)
            {
                return cachedMembership.Value;
            }

            var isMember = ValidateMembershipInternal(samAccountName) ?? true; //let unknown result will be true
            _cache.SetMembership(samAccountName, isMember);

            return isMember;
        }

        private bool? ValidateMembershipInternal(string samAccountName)
        {
            var groupName = Configuration.Current.ActiveDirectory2FaGroup;

            try
            {
                var domain = IPGlobalProperties.GetIPGlobalProperties().DomainName;
                var baseDn = Fqdn2Dn(domain);

                using (var ldap = new LdapConnection(domain))
                {
                    ldap.SessionOptions.RootDseCache = true;
                    ldap.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
                    ldap.Bind(); //as current user

                    var groupDn = _cache.GetGroupDn(groupName);
                    if (groupDn == null)
                    {
                        groupDn = GetGroupDn(ldap, groupName, baseDn);
                        if (groupDn != null)
                        {
                            _cache.SetGroupDn(groupName, groupDn);
                        }
                    }

                    if (groupDn == null)
                    {
                        Logger.Owa.Warn($"Security group {groupName} not exists");
                        return null; //group not exists
                    }

                    var searchFilter = $"(&(sAMAccountName={samAccountName})(memberOf:1.2.840.113556.1.4.1941:={groupDn}))";

                    var response = Query(ldap, baseDn, searchFilter, SearchScope.Subtree, "DistinguishedName");

                    var isInGroup = response.Entries.Count > 0;

                    return isInGroup;
                }
            }
            catch (Exception ex)
            {
                Logger.Owa.Error(ex.ToString());
            }

            return null;
        }

        /// <summary>
        /// Search group distinguished name
        /// </summary>
        private string GetGroupDn(LdapConnection connection, string name, string baseDn)
        {
            var searchFilter = $"(&(objectCategory=group)(name={name}))";
            var response = Query(connection, baseDn, searchFilter, SearchScope.Subtree, "DistinguishedName");
            
            if (response.Entries.Count > 0)
            {
                return response.Entries[0].DistinguishedName;
            }

            return null;
        }

        /// <summary>
        /// Query to LDAP
        /// </summary>
        private SearchResponse Query(LdapConnection connection, string baseDn, string filter, SearchScope scope, params string[] attributes)
        {
            var searchRequest = new SearchRequest
                (baseDn,
                 filter,
                 scope,
                 attributes);

            var response = (SearchResponse)connection.SendRequest(searchRequest);
            return response;
        }

        /// <summary>
        /// Converts domain.local to DC=domain,DC=local
        /// </summary>
        private string Fqdn2Dn(string name)
        {
            var portIndex = name.IndexOf(":");
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