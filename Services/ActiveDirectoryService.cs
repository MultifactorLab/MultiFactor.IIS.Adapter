using MultiFactor.IIS.Adapter.Extensions;
using MultiFactor.IIS.Adapter.Services.Ldap;
using MultiFactor.IIS.Adapter.Services.Ldap.Profile;
using System;
using System.DirectoryServices.Protocols;

namespace MultiFactor.IIS.Adapter.Services
{
    /// <summary>
    /// Simple AD client
    /// </summary>
    public class ActiveDirectoryService
    {
        private readonly CacheAdapter _cache;
        private readonly Logger _logger;

        public ActiveDirectoryService(CacheAdapter cache, Logger logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ILdapProfile GetProfile(string samAccountName)
        {
            if (samAccountName is null) throw new ArgumentNullException(nameof(samAccountName));

            var profile = _cache.GetProfile(samAccountName);
            if (profile != null) return profile;

            try
            {
                using (var adapter = LdapConnectionAdapter.Create(_logger))
                {
                    var loader = new ProfileLoader(adapter, _logger);
                    profile = loader.Load(samAccountName);

                    _cache.SetProfile(samAccountName, profile);
                    return profile;
                }
            }
            catch (LdapException ex)
            {
                _logger.Error($"{ex}\r\nLDAPErrorCode={ex.ErrorCode}, ServerErrorMessage={ex.ServerErrorMessage}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
                throw;
            }
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
                using (var adapter = LdapConnectionAdapter.Create(_logger))
                {
                    var baseDn = adapter.Domain.GetDn();
                    var groupDn = _cache.GetGroupDn(groupName);
                    if (groupDn == null)
                    {
                        groupDn = GetGroupDn(adapter, groupName, baseDn);
                        if (groupDn != null)
                        {
                            _cache.SetGroupDn(groupName, groupDn);
                        }
                    }

                    if (groupDn == null)
                    {
                        _logger.Warn($"Security group {groupName} not exists");
                        return null; //group not exists
                    }

                    var searchFilter = $"(&(sAMAccountName={samAccountName})(memberOf:1.2.840.113556.1.4.1941:={groupDn}))";
                    var response = adapter.Search(baseDn, searchFilter, SearchScope.Subtree, "DistinguishedName");

                    return response.Entries.Count != 0;
                }
            }
            catch (LdapException ex)
            {
                _logger.Error($"{ex}\r\nLDAPErrorCode={ex.ErrorCode}, ServerErrorMessage={ex.ServerErrorMessage}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
            }

            return null;
        }

        public string SearchUserPrincipalName(string samAccountName)
        {
            const string attr = "UserPrincipalName";

            try
            {
                using (var adapter = LdapConnectionAdapter.Create(_logger))
                {
                    var searchFilter = $"(&(sAMAccountName={samAccountName})(objectClass=user))";
                    var response = adapter.Search(adapter.Domain.GetDn(), searchFilter, SearchScope.Subtree, attr);
                    if (response.Entries.Count == 0) return samAccountName;
                    
                    return response.Entries[0].Attributes[attr]?[0]?.ToString();
                }
            }
            catch (LdapException ex)
            {
                _logger.Error($"{ex}\r\nLDAPErrorCode={ex.ErrorCode}, ServerErrorMessage={ex.ServerErrorMessage}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
            }

            return null;
        }

        /// <summary>
        /// Search group distinguished name
        /// </summary>
        private string GetGroupDn(LdapConnectionAdapter adapter, string name, string baseDn)
        {
            var searchFilter = $"(&(objectCategory=group)(name={name}))";
            var response = adapter.Search(baseDn, searchFilter, SearchScope.Subtree, "DistinguishedName");
            if (response.Entries.Count == 0) return null;

            return response.Entries[0].DistinguishedName;
        }
    }
}