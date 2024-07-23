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
        private readonly Configuration _config;
        private readonly Logger _logger;

        public ActiveDirectoryService(CacheAdapter cache, Logger logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = Configuration.Current;
        }

        public ILdapProfile GetProfile(string samAccountName)
        {
            if (samAccountName is null)
            {
                throw new ArgumentNullException(nameof(samAccountName));
            }

            var profile = _cache.GetProfile(samAccountName);
            if (profile != null)
            {
                return profile;
            }

            foreach (var domain in _config.ActiveDirectoryDomains)
            {
                try
                {
                    _logger.Info($"Try load profile from {domain}");
                    using (var adapter = LdapConnectionAdapter.Create(domain, _logger))
                    {
                        var loader = new ProfileLoader(adapter, _config);
                        profile = loader.Load(samAccountName);
                        if (profile == null)
                        {
                            continue;
                        } 
                        
                        _logger.Info($"Profile loaded for user '{profile.SamAccountName}'");
                        
                        _cache.SetProfile(samAccountName, profile);
                        return profile;
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
            }
            return null;
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

            var isMember = ValidateMembershipInternal(samAccountName);
            _cache.SetMembership(samAccountName, isMember);

            return isMember;
        }

        private bool ValidateMembershipInternal(string samAccountName)
        {
            var groupName = _config.ActiveDirectory2FaGroup;

            try
            {
                foreach (var domain in _config.ActiveDirectoryDomains)
                {
                    using (var adapter = LdapConnectionAdapter.Create(domain, _logger))
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
                            return true; //group not exists, let unknown result will be true
                        }

                        var searchFilter = $"(&(sAMAccountName={samAccountName})(memberOf:1.2.840.113556.1.4.1941:={groupDn}))";
                        var response = adapter.Search(baseDn, searchFilter, SearchScope.Subtree, "DistinguishedName");

                        if (response.Entries.Count != 0)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (LdapException ex)
            {
                _logger.Error($"{ex}\r\nLDAPErrorCode={ex.ErrorCode}, ServerErrorMessage={ex.ServerErrorMessage}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
            }

            return true; //let unknown result will be true
        }

        /// <summary>
        /// Search group distinguished name
        /// </summary>
        private string GetGroupDn(LdapConnectionAdapter adapter, string name, string baseDn)
        {
            var searchFilter = $"(&(objectCategory=group)(name={name}))";
            var response = adapter.Search(baseDn, searchFilter, SearchScope.Subtree, "DistinguishedName");
            return response.Entries.Count == 0 
                ? null 
                : response.Entries[0].DistinguishedName;
        }
    }
}