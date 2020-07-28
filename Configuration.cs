using System;
using System.Configuration;

namespace MultiFactor.IIS.Adapter
{
    public class Configuration
    {
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public string ApiUrl { get; set; }

        public static Configuration Current { get; set; }


        public static void Load()
        {
            var appSettings = ConfigurationManager.AppSettings;

            var apiUrlSetting = appSettings["multifactor:api-url"];
            var apiKeySetting = appSettings["multifactor:api-key"];
            var apiSecretSetting = appSettings["multifactor:api-secret"];

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
                ApiSecret = apiSecretSetting
            };
        }
    }
}