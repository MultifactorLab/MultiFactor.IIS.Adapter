using System.Configuration;
using Xunit;

namespace MultiFactor.IIS.Adapter.Tests
{
    public class ConfigurationTests
    {
        private static void ResetSettings()
        {
            ConfigurationManager.AppSettings["multifactor:api-url"] = "api.multifactor.ru";
            ConfigurationManager.AppSettings["multifactor:api-key"] = "key";
            ConfigurationManager.AppSettings["multifactor:api-secret"] = "secret";
            ConfigurationManager.AppSettings["multifactor:bypass-second-factor-when-api-unreachable"] = true.ToString();

            ConfigurationManager.AppSettings["multifactor:api-proxy"] = null;
            ConfigurationManager.AppSettings["multifactor:active-directory-2fa-group"] = null;
            ConfigurationManager.AppSettings["multifactor:use-upn-as-identity"] = null;
            ConfigurationManager.AppSettings["multifactor:phone-attribute"] = null;
            ConfigurationManager.AppSettings["multifactor:active-directory-2fa-group-membership-cache-timeout"] = null;
            ConfigurationManager.AppSettings["multifactor:active-directory-cache-timeout"] = null;
        }

        [Fact]
        public void AdCache_UseLegacySetting_ShouldReturnLegacyValue()
        {
            ResetSettings();
            ConfigurationManager.AppSettings["multifactor:active-directory-2fa-group-membership-cache-timeout"] = "15";
            var curr = TestableConfiguration.Reload();

            Assert.Equal(15, curr.ActiveDirectoryCacheTimout);
        }
        
        [Fact]
        public void AdCache_UseNewSetting_ShouldReturnNewValue()
        {
            ResetSettings();
            ConfigurationManager.AppSettings["multifactor:active-directory-cache-timeout"] = "8";
            var curr = TestableConfiguration.Reload();

            Assert.Equal(8, curr.ActiveDirectoryCacheTimout);
        }
        
        [Fact]
        public void AdCache_UseBothNewAndLegacySetting_ShouldReturnNewValue()
        {
            ResetSettings();
            ConfigurationManager.AppSettings["multifactor:active-directory-cache-timeout"] = "8";
            ConfigurationManager.AppSettings["multifactor:active-directory-2fa-group-membership-cache-timeout"] = "15";
            var curr = TestableConfiguration.Reload();

            Assert.Equal(8, curr.ActiveDirectoryCacheTimout);
        }
        
        [Fact]
        public void AdCache_UseNothing_ShouldReturnDefaultValue()
        {
            ResetSettings();
            var curr = TestableConfiguration.Reload();

            Assert.Equal(-1, curr.ActiveDirectoryCacheTimout);
        }
    }
}
