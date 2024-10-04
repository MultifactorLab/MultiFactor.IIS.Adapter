using MultiFactor.IIS.Adapter.Extensions;
using MultiFactor.IIS.Adapter.Interop;
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

        public ILdapProfile GetProfile(LdapIdentity identity)
        {
            if (identity is null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            var profile = _cache.GetProfile(identity.RawName);
            if (profile != null)
            {
                return profile;
            }

            foreach (var domain in _config.ActiveDirectoryDomains)
            {
                try
                {
                    _logger.Info($"Try load {identity.RawName} profile from {domain}");
                    UserSearchContext searchContext;
                    if (identity.HasNetbiosName())
                    {
                        searchContext = new NetbiosService(_logger).ConvertToUpnUser(identity, domain);
                    }
                    else
                    {
                        _logger.Warn($"Something strange: user {identity.RawName} has not netbiosname. Identity: {identity.Name}, {identity.TypeName}, {identity.NetBiosName}");
                        searchContext = new UserSearchContext(domain, identity.Name, identity.RawName);
                    }

                    _logger.Info($"Start load user profile in context: {searchContext}");
                    using (var adapter = LdapConnectionAdapter.Create(searchContext.Domain, _logger))
                    {
                        var loader = new ProfileLoader(adapter, _config, _logger);
                        profile = loader.Load(searchContext.UserIdentity);
                        if (profile == null)
                        {
                            continue;
                        } 
                        
                        _logger.Info($"Profile loaded for user '{profile.RawUserName}'");
                        
                        _cache.SetProfile(identity.RawName, profile);
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
                // very noisy, only for debug
                // _logger.Info($"Get profile iteration for {domain} finished");
            }
            return null;
        }

        /// <summary>
        /// Only group members should 2fa
        /// </summary>
        public bool ValidateMembership(LdapIdentity identity)
        {
            var cachedMembership = _cache.GetMembership(identity.RawName);
            if (cachedMembership != null)
            {
                return cachedMembership.Value;
            }

            var isMember = ValidateMembershipInternal(identity);
            _cache.SetMembership(identity.RawName, isMember);

            return isMember;
        }

        private bool ValidateMembershipInternal(LdapIdentity identity)
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

                        _logger.Info($"Try validate membership {identity.RawName} profile from {domain} in {groupDn}");
                        UserSearchContext searchContext;
                        if (identity.HasNetbiosName())
                        {
                            searchContext = new NetbiosService(_logger).ConvertToUpnUser(identity, domain);
                        }
                        else
                        {
                            _logger.Warn($"Something strange: user {identity.RawName} has not netbiosname. Identity: {identity.Name}, {identity.TypeName}, {identity.NetBiosName}");
                            searchContext = new UserSearchContext(domain, identity.Name, identity.RawName);
                        }

                        var searchFilter = $"(&({searchContext.UserIdentity.TypeName}={searchContext.UserIdentity.Name})(memberOf:1.2.840.113556.1.4.1941:={groupDn}))";
                        var response = adapter.Search(baseDn, searchFilter, SearchScope.Subtree, true, "DistinguishedName");

                        if (response.Entries.Count != 0)
                        {
                            _logger.Info($"{identity.RawName} is member of {groupName}");
                            return true;
                        }
                    }
                    // very noisy, only for debug
                    // _logger.Info($"ValidateMembership iteration for {domain} finished");
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
            var response = adapter.Search(baseDn, searchFilter, SearchScope.Subtree, true, "DistinguishedName");
            if(response.Entries.Count != 0)
            {
                var groupDn = response.Entries[0].DistinguishedName;
                _logger.Info($"Group {name} was found:{groupDn}");
                return groupDn;
            }
            else
            {
                _logger.Info($"Group {name} was not found");
                return null;
            }
        }
    }
}