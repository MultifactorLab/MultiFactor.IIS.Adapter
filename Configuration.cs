using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;

namespace MultiFactor.IIS.Adapter
{
    public class Configuration
    {
        public string ApiKey { get; private set; }
        public string ApiSecret { get; private set; }
        public string ApiUrl { get; private set; }
        public string ApiProxy { get; private set; }
        public string ActiveDirectory2FaGroup { get; private set; }
        public int ActiveDirectoryCacheTimout { get; private set; }        
        public bool UseUpnAsIdentity { get; private set; }
        public string[] PhoneAttributes { get; private set; } = new string[0];

        private static readonly Lazy<Configuration> _current = new Lazy<Configuration>(Load);
        public static Configuration Current => _current.Value;

        private Configuration() { }

        private static Configuration Load()
        {
            var appSettings = ConfigurationManager.AppSettings;

            var apiUrlSetting = appSettings["multifactor:api-url"];
            var apiKeySetting = appSettings["multifactor:api-key"];
            var apiSecretSetting = appSettings["multifactor:api-secret"];
            var apiProxySetting = appSettings["multifactor:api-proxy"];
            var activeDirectory2FaGroupSetting = appSettings["multifactor:active-directory-2fa-group"];
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

            var config = new Configuration
            {
                ApiUrl = apiUrlSetting,
                ApiKey = apiKeySetting,
                ApiSecret = apiSecretSetting,
                ApiProxy = apiProxySetting,
                ActiveDirectory2FaGroup = activeDirectory2FaGroupSetting
            };

            if (bool.TryParse(useUpnAsIdentitySetting, out var useUpnAsIdentity))
            {
                config.UseUpnAsIdentity = useUpnAsIdentity;
            }

            ReadActiveDirectoryCacheTimoutSetting(appSettings, config);
            ReadPhoneAttributeSetting(appSettings, config);

            return config;
        }

        private static void ReadPhoneAttributeSetting(NameValueCollection appSettings, Configuration configuration)
        {
            const string key = "multifactor:phone-attribute";
            var value = appSettings[key];
            if (string.IsNullOrWhiteSpace(value)) return;

            var parsed = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(attr => attr.Trim()).ToArray();
            if (parsed.Length != 0) configuration.PhoneAttributes = parsed;     
        }

        private static void ReadActiveDirectoryCacheTimoutSetting(NameValueCollection appSettings, Configuration configuration)
        {
            const string legacyKey = "multifactor:active-directory-2fa-group-membership-cache-timeout";
            const string key = "multifactor:active-directory-cache-timeout";

            int ttl = -1;

            var legacyValue = appSettings[legacyKey];
            if (int.TryParse(legacyValue, out var legVal)) ttl = legVal;
            
            var value = appSettings[key];
            if (int.TryParse(value, out var val)) ttl = val;
            
            configuration.ActiveDirectoryCacheTimout = ttl;
        }
    }
}