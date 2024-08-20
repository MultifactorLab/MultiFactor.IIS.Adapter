using System;

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

        public string Get(string rawUsername, string postbackUrl)
        {
            var identity = Util.CanonicalizeUserName(rawUsername);
            Logger.API.Info($"Applying identity canonicalization: {rawUsername}->{identity}");
            
            var profile = _activeDirectory.GetProfile(identity);
            if (profile == null)
            {
                // redirect to (custom?) error page
                throw new Exception($"Profile {rawUsername} not found");
            }
            
            if (Configuration.Current.HasTwoFaIdentityAttribute && !string.IsNullOrEmpty(profile.TwoFAIdentity))
            {
                Logger.API.Info($"Applying 2fa identity attribute: {identity}->{profile.TwoFAIdentity}");
                identity = profile.TwoFAIdentity;
            }

            var multiFactorAccessUrl = _api.CreateRequest(identity, rawUsername, postbackUrl, profile?.Phone);
            return multiFactorAccessUrl;
        }
    }
}