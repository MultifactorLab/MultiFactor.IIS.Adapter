using System;
using System.Configuration;
using Xunit;

namespace MultiFactor.IIS.Adapter.Tests
{
    public class ConfigurationTests
    {
        private static void ResetSettings()
        {
            ConfigurationManager.AppSettings[ConfigurationKeys.ApiUrl] = "api.multifactor.ru";
            ConfigurationManager.AppSettings[ConfigurationKeys.ApiKey] = "key";
            ConfigurationManager.AppSettings[ConfigurationKeys.ApiSecret] = "secret";
            ConfigurationManager.AppSettings[ConfigurationKeys.BypassSecondFactorWhenApiUnreachable] = true.ToString();

            ConfigurationManager.AppSettings[ConfigurationKeys.ApiProxy] = null;
            ConfigurationManager.AppSettings[ConfigurationKeys.ActiveDirectory2FAGroup] = null;
            ConfigurationManager.AppSettings[ConfigurationKeys.UseUpnAsIdentity] = null;
            ConfigurationManager.AppSettings[ConfigurationKeys.PhoneAttribute] = null;
            ConfigurationManager.AppSettings[ConfigurationKeys.ActiveDirectory2FAGroupMembershipCacheTimeout] = null;
            ConfigurationManager.AppSettings[ConfigurationKeys.ActiveDirectoryCacheTimeout] = null;
            ConfigurationManager.AppSettings[ConfigurationKeys.ApiLifeCheckInterval] = null;
        }

        [Fact]
        public void AdCache_UseLegacySetting_ShouldReturnLegacyValue()
        {
            ResetSettings();
            ConfigurationManager.AppSettings[ConfigurationKeys.ActiveDirectory2FAGroupMembershipCacheTimeout] = "15";
            var curr = TestableConfiguration.Reload();

            Assert.Equal(15, curr.ActiveDirectoryCacheTimout.TotalMinutes);
        }
        
        [Fact]
        public void AdCache_UseNewSetting_ShouldReturnNewValue()
        {
            ResetSettings();
            ConfigurationManager.AppSettings[ConfigurationKeys.ActiveDirectoryCacheTimeout] = "8";
            var curr = TestableConfiguration.Reload();

            Assert.Equal(8, curr.ActiveDirectoryCacheTimout.TotalMinutes);
        }
        
        [Fact]
        public void AdCache_UseBothNewAndLegacySetting_ShouldReturnNewValue()
        {
            ResetSettings();
            ConfigurationManager.AppSettings[ConfigurationKeys.ActiveDirectoryCacheTimeout] = "8";
            ConfigurationManager.AppSettings[ConfigurationKeys.ActiveDirectory2FAGroupMembershipCacheTimeout] = "15";
            var curr = TestableConfiguration.Reload();

            Assert.Equal(8, curr.ActiveDirectoryCacheTimout.TotalMinutes);
        }
        
        [Fact]
        public void AdCache_UseNothing_ShouldReturnDefaultValue()
        {
            ResetSettings();
            var curr = TestableConfiguration.Reload();

            Assert.Equal(TimeSpan.FromMinutes(15), curr.ActiveDirectoryCacheTimout);
        }
        
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("0")]
        [InlineData("-1")]
        public void ApiLifeCheckInterval_ShouldReturnDefaultValue(string val)
        {
            ResetSettings();
            ConfigurationManager.AppSettings[ConfigurationKeys.ApiLifeCheckInterval] = val;
            var curr = TestableConfiguration.Reload();

            Assert.Equal(TimeSpan.FromMinutes(15), curr.ActiveDirectoryCacheTimout);
        }
    }
}
