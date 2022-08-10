using System;
using System.Configuration;

namespace MultiFactor.IIS.Adapter
{
    public class Configuration
    {
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public string ApiUrl { get; set; }
        public string ApiProxy { get; set; }
        public string ActiveDirectory2FaGroup { get; set; }
        public int? ActiveDirectory2FaGroupMembershipCacheTimout { get; set; }
        
        public bool UseUpnAsIdentity { get; set; }

        public static Configuration Current { get; set; }


        public static void Load()
        {
            var appSettings = ConfigurationManager.AppSettings;

            var apiUrlSetting = appSettings["multifactor:api-url"];
            var apiKeySetting = appSettings["multifactor:api-key"];
            var apiSecretSetting = appSettings["multifactor:api-secret"];
            var apiProxySetting = appSettings["multifactor:api-proxy"];
            var activeDirectory2FaGroupSetting = appSettings["multifactor:active-directory-2fa-group"];
            var activeDirectory2FaGroupMembershipCacheTimout = appSettings["multifactor:active-directory-2fa-group-membership-cache-timeout"];
            var useUpnAsIdentitySetting = appSettings["multifactor:use-upn-as-identity"];

            if (string.IsNullOrEmpty(apiUrlSetting))
            {
                throw new Exception("Configuration error: 'multifactor:api-url' element not found or empty");
            }
            if (string.IsNullOrEmpty(apiKeySetting))
            {
                throw new Exception("Configuration error: 'multifactor:api-key' element not found or empty");
            }
            if (string.IsNullOrEmpty(apiSecretSetting))
            {
                throw new Exception("Configuration error: 'multifactor:api-secret' element not found or empty");
            }

            Current = new Configuration
            {
                ApiUrl = apiUrlSetting,
                ApiKey = apiKeySetting,
                ApiSecret = apiSecretSetting,
                ApiProxy = apiProxySetting,
                ActiveDirectory2FaGroup = activeDirectory2FaGroupSetting
            };

            if (int.TryParse(activeDirectory2FaGroupMembershipCacheTimout, out var ttl))
            {
                Current.ActiveDirectory2FaGroupMembershipCacheTimout = ttl;
            }

            if (bool.TryParse(useUpnAsIdentitySetting, out var useUpnAsIdentity))
            {
                Current.UseUpnAsIdentity = useUpnAsIdentity;
            }
        }
    }
}