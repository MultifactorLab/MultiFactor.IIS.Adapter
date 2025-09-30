using MultiFactor.IIS.Adapter.Dto;
using MultiFactor.IIS.Adapter.Services.Ldap;
using System;
using System.Threading.Tasks;

namespace MultiFactor.IIS.Adapter.Services
{
    public class AccessUrl
    {
        private readonly ActiveDirectoryService _activeDirectory;
        private readonly MultiFactorApiClient _api;

        public AccessUrl(ActiveDirectoryService activeDirectory, MultiFactorApiClient api)
        {
            _activeDirectory = activeDirectory ?? throw new ArgumentNullException(nameof(activeDirectory));
            _api = api ?? throw new ArgumentNullException(nameof(api));
        }

        public string Get(LdapIdentity identity, string postbackUrl)
        {

            var profile = _activeDirectory.GetProfile(identity);
            if (profile == null)
            {
                // redirect to (custom?) error page
                throw new Exception($"Profile {identity.RawName} not found");
            }

            var twoFAIdentity = identity.Name; // canonicalizated name
            Logger.API.Info($"Applying identity canonicalization: {identity.RawName}->{twoFAIdentity}");
            
            if (Configuration.Current.HasTwoFaIdentityAttribute && !string.IsNullOrEmpty(profile.Custom2FAIdentity))
            {
                Logger.API.Info($"Applying 2fa identity attribute: {identity.RawName}->{profile.Custom2FAIdentity}");
                twoFAIdentity = profile.Custom2FAIdentity;
            }

            var multiFactorAccessUrl = _api.CreateRequest(twoFAIdentity, identity.RawName, postbackUrl, profile?.Phone);
            return multiFactorAccessUrl;
        }
        public async Task<ScopeSupportInfoDto> Info()
        {
            var multiFactorAccessUrl = _api.GetScopeSupportInfoAsync();
            return await multiFactorAccessUrl;
        }
    }
}