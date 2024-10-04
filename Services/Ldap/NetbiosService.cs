using MultiFactor.IIS.Adapter.Interop;
using System;

namespace MultiFactor.IIS.Adapter.Services.Ldap
{
    public class NetbiosService
    {
        public readonly Logger _logger;

        public NetbiosService(Logger logger)
        {
            _logger = logger;
        }

        public UserSearchContext ConvertToUpnUser(LdapIdentity user, string domain)
        {
            var upnUserName = ResolveUserByNetBios(user.RawName, user.NetBiosName, domain);
            return upnUserName;
        }

        private UserSearchContext ResolveUserByNetBios(string fullUserName, string netBiosName, string domain)
        {
            _logger.Info($"Trying to resolve domain by netbios {netBiosName}, user: {fullUserName}.");
            try
            {
                using (var nameTranslator = new NameTranslator(domain, _logger))
                {
                    // first try a strict domain resolving method
                    var searchContext = nameTranslator.Translate(fullUserName);
                    if (!string.IsNullOrEmpty(searchContext.Domain))
                    {
                        _logger.Info($"Success find {searchContext} by {fullUserName}");
                        return searchContext;
                    }
                    throw new Exception("NetbiosName not found");
                }
            }
            catch (Exception e)
            {
                _logger.Warn($"Error during translate netbios name {fullUserName}");
                throw;
            }
        }
    }
}