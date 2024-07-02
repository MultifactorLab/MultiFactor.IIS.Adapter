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
        public bool BypassSecondFactorWhenApiUnreachable { get; private set; }

        public string ActiveDirectory2FaGroup { get; private set; }
        public TimeSpan ActiveDirectoryCacheTimout { get; private set; }
        public TimeSpan ApiLifeCheckInterval { get; private set; }
        public bool UseUpnAsIdentity { get; private set; }
        public string[] PhoneAttributes { get; private set; } = new string[0];

        /// <summary>
        /// Active Directory Domain
        /// </summary>
        public string ActiveDirectoryDomain { get; set; }

        public string[] SplittedActiveDirectoryDomains =>
            (ActiveDirectoryDomain ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .ToArray();

        private static readonly Lazy<Configuration> _current = new Lazy<Configuration>(Load);
        public static Configuration Current => _current.Value;

        protected Configuration() { }

        protected static Configuration Load()
        {
            var appSettings = ConfigurationManager.AppSettings;

            var apiUrlSetting = appSettings[ConfigurationKeys.ApiUrl];
            var apiKeySetting = appSettings[ConfigurationKeys.ApiKey];
            var apiSecretSetting = appSettings[ConfigurationKeys.ApiSecret];
            var apiProxySetting = appSettings[ConfigurationKeys.ApiProxy];

            var activeDirectory2FaGroupSetting = appSettings[ConfigurationKeys.ActiveDirectory2FAGroup];
            var activeDirectoryDomain = appSettings[ConfigurationKeys.ActiveDirectoryDomain];
            var useUpnAsIdentitySetting = appSettings[ConfigurationKeys.UseUpnAsIdentity];

            var domain = System.DirectoryServices.ActiveDirectory.Domain.GetComputerDomain().Name;
            if (!string.IsNullOrWhiteSpace(activeDirectoryDomain))
            {
                domain = activeDirectoryDomain;
            }

            if (string.IsNullOrEmpty(apiUrlSetting))
            {
                throw new Exception($"Configuration error: '{ConfigurationKeys.ApiUrl}' element not found or empty");
            }
            if (string.IsNullOrEmpty(apiKeySetting))
            {
                throw new Exception($"Configuration error: '{ConfigurationKeys.ApiKey}' element not found or empty");
            }
            if (string.IsNullOrEmpty(apiSecretSetting))
            {
                throw new Exception($"Configuration error: '{ConfigurationKeys.ApiSecret}' element not found or empty");
            }

            var config = new Configuration
            {
                ApiUrl = apiUrlSetting,
                ApiKey = apiKeySetting,
                ApiSecret = apiSecretSetting,
                ApiProxy = apiProxySetting,
                ActiveDirectoryDomain = domain,
                ActiveDirectory2FaGroup = activeDirectory2FaGroupSetting
            };

            if (bool.TryParse(useUpnAsIdentitySetting, out var useUpnAsIdentity))
            {
                config.UseUpnAsIdentity = useUpnAsIdentity;
            }

            ReadActiveDirectoryCacheTimoutSetting(appSettings, config);
            ReadPhoneAttributeSetting(appSettings, config);
            ReadBypassWhenApiUnreachableSetting(appSettings, config);
            ReadApiLifeCheckIntervalSetting(appSettings, config);

            return config;
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