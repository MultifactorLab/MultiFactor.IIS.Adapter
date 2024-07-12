using MultiFactor.IIS.Adapter.Services;
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;

namespace MultiFactor.IIS.Adapter
{
    public class Configuration
    {
        private readonly string _activeDirectoryDomain;
        public string[] ActiveDirectoryDomains =>
            (_activeDirectoryDomain ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .ToArray();
        
        public string ApiUrl { get; }
        public string ApiKey { get; }
        public string ApiSecret { get; }
        public string ApiProxy { get; }
        
        public bool BypassSecondFactorWhenApiUnreachable { get; private set; }

        public string ActiveDirectory2FaGroup { get; private set; }
        public TimeSpan ActiveDirectoryCacheTimout { get; private set; }
        public TimeSpan ApiLifeCheckInterval { get; private set; }

        //Lookup for some attribute and use it for 2fa instead of uid
        public bool UseIdentityAttribute => !string.IsNullOrWhiteSpace(TwoFaIdentityAttribute);
        public string TwoFaIdentityAttribute { get; private set; }
        public string[] PhoneAttributes { get; private set; } = new string[0];
        
        
        private static readonly Lazy<Configuration> _current = new Lazy<Configuration>(Load);
        public static Configuration Current => _current.Value;
        
        protected Configuration() {}

        private Configuration(string activeDirectoryDomain, string apiUrl, string apiKey, string apiSecret, string apiProxy)
        {
            _activeDirectoryDomain = activeDirectoryDomain;
            ApiUrl = apiUrl;
            ApiKey = apiKey;
            ApiSecret = apiSecret;
            ApiProxy = apiProxy;
        }

        protected static Configuration Load()
        {
            var appSettings = ConfigurationManager.AppSettings;

            var apiUrlSetting = appSettings[ConfigurationKeys.ApiUrl];
            var apiKeySetting = appSettings[ConfigurationKeys.ApiKey];
            var apiSecretSetting = appSettings[ConfigurationKeys.ApiSecret];
            var apiProxySetting = appSettings[ConfigurationKeys.ApiProxy];

            var activeDirectory2FaGroupSetting = appSettings[ConfigurationKeys.ActiveDirectory2FAGroup];
            var activeDirectoryDomain = appSettings[ConfigurationKeys.ActiveDirectoryDomain];

            var domain = GetDomain(activeDirectoryDomain);

            if (string.IsNullOrWhiteSpace(apiUrlSetting))
            {
                throw new Exception($"Configuration error: '{ConfigurationKeys.ApiUrl}' element not found or empty");
            }
            if (string.IsNullOrWhiteSpace(apiKeySetting))
            {
                throw new Exception($"Configuration error: '{ConfigurationKeys.ApiKey}' element not found or empty");
            }
            if (string.IsNullOrWhiteSpace(apiSecretSetting))
            {
                throw new Exception($"Configuration error: '{ConfigurationKeys.ApiSecret}' element not found or empty");
            }

            var config = new Configuration(domain, apiUrlSetting, apiKeySetting, apiSecretSetting, apiProxySetting);

            if (!string.IsNullOrWhiteSpace(activeDirectory2FaGroupSetting))
            {
                config.ActiveDirectory2FaGroup = activeDirectory2FaGroupSetting;
            }

            ReadTwoFaIdentityAttributeSetting(appSettings, config);
            ReadActiveDirectoryCacheTimoutSetting(appSettings, config);
            ReadPhoneAttributeSetting(appSettings, config);
            ReadBypassWhenApiUnreachableSetting(appSettings, config);
            ReadApiLifeCheckIntervalSetting(appSettings, config);

            return config;
        }

        private static void ReadTwoFaIdentityAttributeSetting(NameValueCollection appSettings, Configuration configuration)
        {
            var useUpnAsIdentitySetting = appSettings[ConfigurationKeys.UseUpnAsIdentity];
            var twoFaIdentityAttributeSetting = appSettings[ConfigurationKeys.TwoFAIdentityAttribyte];
            
            // MUST be before 'use-upn-as-identity' check
            if (!string.IsNullOrWhiteSpace(twoFaIdentityAttributeSetting))
            {
                configuration.TwoFaIdentityAttribute = twoFaIdentityAttributeSetting;
            }

            //legacy settings for 2fa identity
            if (bool.TryParse(useUpnAsIdentitySetting, out var useUpnAsIdentity))
            {
                if (!string.IsNullOrWhiteSpace(twoFaIdentityAttributeSetting))
                {
                    throw new Exception("Configuration error: Using settings 'use-upn-as-identity' and 'use-attribute-as-identity' together is unacceptable. Prefer using 'use-attribute-as-identity'.");
                }

                Logger.Owa.Warn("The setting 'use-upn-as-identity' is deprecated, use 'use-attribute-as-identity' instead");
                if (useUpnAsIdentity)
                {
                    configuration.TwoFaIdentityAttribute = "userPrincipalName";
                }
            }
        }

        private static string GetDomain(string activeDirectoryDomain)
        {
            if (!string.IsNullOrWhiteSpace(activeDirectoryDomain))
            {
                 return activeDirectoryDomain;
            } 
            
            return System.DirectoryServices.ActiveDirectory.Domain.GetComputerDomain().Name;
        }

        private static void ReadPhoneAttributeSetting(NameValueCollection appSettings, Configuration configuration)
        {
            var value = appSettings[ConfigurationKeys.PhoneAttribute];
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var parsed = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(attr => attr.Trim()).ToArray();
            if (parsed.Length != 0) configuration.PhoneAttributes = parsed;
        }

        private static void ReadActiveDirectoryCacheTimoutSetting(NameValueCollection appSettings, Configuration configuration)
        {
            var ttl = TimeSpan.Zero;

            var legacyValue = appSettings[ConfigurationKeys.ActiveDirectory2FAGroupMembershipCacheTimeout];
            if (int.TryParse(legacyValue, out var legVal)) ttl = TimeSpan.FromMinutes(legVal);

            var value = appSettings[ConfigurationKeys.ActiveDirectoryCacheTimeout];
            if (int.TryParse(value, out var val)) ttl = TimeSpan.FromMinutes(val);

            configuration.ActiveDirectoryCacheTimout = ttl > TimeSpan.Zero ? ttl : TimeSpan.FromMinutes(15);
        }

        private static void ReadApiLifeCheckIntervalSetting(NameValueCollection appSettings, Configuration configuration)
        {
            var defaultValue = TimeSpan.FromMinutes(15);

            var value = appSettings[ConfigurationKeys.ApiLifeCheckInterval];
            if (string.IsNullOrEmpty(value))
            {
                configuration.ApiLifeCheckInterval = defaultValue;
                return;
            }

            if (!int.TryParse(value, out var parsed))
            {
                throw new Exception($"Configuration error: '{ConfigurationKeys.ApiLifeCheckInterval}' element has invalid value");
            }

            configuration.ApiLifeCheckInterval = parsed > 0 ? TimeSpan.FromMinutes(parsed) : defaultValue;
        }

        private static void ReadBypassWhenApiUnreachableSetting(NameValueCollection appSettings, Configuration configuration)
        {
            var value = appSettings[ConfigurationKeys.BypassSecondFactorWhenApiUnreachable];
            if (string.IsNullOrEmpty(value))
            {
                configuration.BypassSecondFactorWhenApiUnreachable = true;
                return;
            }

            if (!bool.TryParse(value, out var parsed))
            {
                throw new Exception($"Configuration error: '{ConfigurationKeys.BypassSecondFactorWhenApiUnreachable}' element has invalid value");
            }

            configuration.BypassSecondFactorWhenApiUnreachable = parsed;
        }
    }
}