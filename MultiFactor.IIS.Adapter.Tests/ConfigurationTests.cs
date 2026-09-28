using System;
using System.Collections.Specialized;
using MultiFactor.IIS.Adapter.Core;
using Xunit;

namespace MultiFactor.IIS.Adapter.Tests
{
    public class ConfigurationTests
    {
        private static NameValueCollection Settings()
        {
            return new NameValueCollection
            {
                { ConfigurationKeys.ActiveDirectoryDomain, "domain.local" },
                { ConfigurationKeys.ApiUrl, "api.multifactor.ru" },
                { ConfigurationKeys.ApiKey, "key" },
                { ConfigurationKeys.ApiSecret, "secret" },
                { ConfigurationKeys.BypassSecondFactorWhenApiUnreachable, true.ToString() },
                { ConfigurationKeys.ActiveDirectoryDomain, "example.test" }
            };
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void PrivacyMode_EmptyValue_ShouldUseNoneMode(string value)
        {
            var settings = Settings();
            settings[ConfigurationKeys.PrivacyMode] = value;
            
            var current = TestableConfiguration.Reload(settings);

            Assert.Equal(PrivacyMode.None, current.PrivacyMode.Mode);
        }

        [Fact]
        public void PrivacyMode_ConfiguredValue_ShouldBeParsed()
        {
            var settings = Settings();
            settings[ConfigurationKeys.PrivacyMode] = "Partial:Phone";

            var current = TestableConfiguration.Reload(settings);

            Assert.Equal(PrivacyMode.Partial, current.PrivacyMode.Mode);
            Assert.True(current.PrivacyMode.HasField("phone"));
        }

        [Fact]
        public void PrivacyMode_InvalidValue_ShouldUseNoneMode()
        {
            var settings = Settings();
            settings[ConfigurationKeys.PrivacyMode] = "Invalid";
            
            var current = TestableConfiguration.Reload(settings);

            Assert.Equal(PrivacyMode.None, current.PrivacyMode.Mode);
        }

        [Fact]
        public void AdCache_UseLegacySetting_ShouldReturnLegacyValue()
        {
            var settings = Settings();
            settings[ConfigurationKeys.ActiveDirectory2FAGroupMembershipCacheTimeout] = "15";

            var curr = TestableConfiguration.Reload(settings);

            Assert.Equal(15, curr.ActiveDirectoryCacheTimout.TotalMinutes);
        }

        [Fact]
        public void AdCache_UseNewSetting_ShouldReturnNewValue()
        {
            var settings = Settings();
            settings[ConfigurationKeys.ActiveDirectoryCacheTimeout] = "8";

            var curr = TestableConfiguration.Reload(settings);

            Assert.Equal(8, curr.ActiveDirectoryCacheTimout.TotalMinutes);
        }

        [Fact]
        public void AdCache_UseBothNewAndLegacySetting_ShouldReturnNewValue()
        {
            var settings = Settings();
            settings[ConfigurationKeys.ActiveDirectoryCacheTimeout] = "8";
            settings[ConfigurationKeys.ActiveDirectory2FAGroupMembershipCacheTimeout] = "15";

            var curr = TestableConfiguration.Reload(settings);

            Assert.Equal(8, curr.ActiveDirectoryCacheTimout.TotalMinutes);
        }

        [Fact]
        public void AdCache_UseNothing_ShouldReturnDefaultValue()
        {
            var curr = TestableConfiguration.Reload(Settings());

            Assert.Equal(TimeSpan.FromMinutes(15), curr.ActiveDirectoryCacheTimout);
        }

        [Fact]
        public void AdGroups_SeveralGroups_ShouldSplitBySemicolon()
        {
            var settings = Settings();
            settings[ConfigurationKeys.ActiveDirectory2FAGroup] = "owa-2fa;Users;Users";

            var curr = TestableConfiguration.Reload(settings);

            Assert.Equal(new[] { "owa-2fa", "Users", "Users" }, curr.ActiveDirectory2FaGroups);
        }

        [Fact]
        public void AdGroups_SingleGroup_ShouldRemainSupported()
        {
            var settings = Settings();
            settings[ConfigurationKeys.ActiveDirectory2FAGroup] = "owa-2fa";

            var curr = TestableConfiguration.Reload(settings);

            Assert.Equal(new[] { "owa-2fa" }, curr.ActiveDirectory2FaGroups);
        }
        
        [Theory]
        [InlineData(null, Constants.DEFAULT_SESSION_LIFE_TIME_IN_HOURS)]
        [InlineData("", Constants.DEFAULT_SESSION_LIFE_TIME_IN_HOURS)]
        [InlineData("invalid", Constants.DEFAULT_SESSION_LIFE_TIME_IN_HOURS)]
        [InlineData("0", Constants.DEFAULT_SESSION_LIFE_TIME_IN_HOURS)]
        [InlineData("-1", Constants.DEFAULT_SESSION_LIFE_TIME_IN_HOURS)]
        [InlineData("1", Constants.MIN_SESSION_LIFE_TIME_IN_HOURS)]
        [InlineData("2", 2)]
        [InlineData("24", Constants.MAX_SESSION_LIFE_TIME_IN_HOURS)]
        [InlineData("25", Constants.MAX_SESSION_LIFE_TIME_IN_HOURS)]
        [InlineData("8760", Constants.MAX_SESSION_LIFE_TIME_IN_HOURS)]
        [InlineData("2147483647", Constants.MAX_SESSION_LIFE_TIME_IN_HOURS)]
        [InlineData("2147483648", Constants.DEFAULT_SESSION_LIFE_TIME_IN_HOURS)]
        public void SessionLifeTime_ShouldUseBoundedValueOrDefault(string value, int expected)
        {
            var settings = Settings();
            settings[ConfigurationKeys.SessionLifeTimeInHours] = value;

            var curr = TestableConfiguration.Reload(settings);

            Assert.Equal(expected, curr.SessionLifeTimeInHours);
        }

        [Theory]
        [InlineData(null, Constants.DEFAULT_SECOND_FACTOR_RE_REQUEST_DELAY_IN_MINUTES)]
        [InlineData("", Constants.DEFAULT_SECOND_FACTOR_RE_REQUEST_DELAY_IN_MINUTES)]
        [InlineData("invalid", Constants.DEFAULT_SECOND_FACTOR_RE_REQUEST_DELAY_IN_MINUTES)]
        [InlineData("0", Constants.DEFAULT_SECOND_FACTOR_RE_REQUEST_DELAY_IN_MINUTES)]
        [InlineData("-1", Constants.DEFAULT_SECOND_FACTOR_RE_REQUEST_DELAY_IN_MINUTES)]
        [InlineData("1", Constants.MIN_SECOND_FACTOR_RE_REQUEST_DELAY_IN_MINUTES)]
        [InlineData("10", 10)]
        [InlineData("60", Constants.MAX_SECOND_FACTOR_RE_REQUEST_DELAY_IN_MINUTES)]
        [InlineData("61", Constants.MAX_SECOND_FACTOR_RE_REQUEST_DELAY_IN_MINUTES)]
        [InlineData("525600", Constants.MAX_SECOND_FACTOR_RE_REQUEST_DELAY_IN_MINUTES)]
        [InlineData("2147483647", Constants.MAX_SECOND_FACTOR_RE_REQUEST_DELAY_IN_MINUTES)]
        [InlineData("2147483648", Constants.DEFAULT_SECOND_FACTOR_RE_REQUEST_DELAY_IN_MINUTES)]
        public void ReRequestDelay_ShouldUseBoundedValueOrDefault(string value, int expected)
        {
            var settings = Settings();
            settings[ConfigurationKeys.SecondFactorReRequestDelayInMinutes] = value;

            var curr = TestableConfiguration.Reload(settings);

            Assert.Equal(expected, curr.ReRequestDelayInMinutes);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("0")]
        [InlineData("-1")]
        public void ApiLifeCheckInterval_ShouldReturnDefaultValue(string val)
        {
            var settings = Settings();
            settings[ConfigurationKeys.ApiLifeCheckInterval] = val;

            var curr = TestableConfiguration.Reload(settings);

            Assert.Equal(TimeSpan.FromMinutes(15), curr.ApiLifeCheckInterval);
        }

        [Theory]
        [InlineData("https://mail.company.com/owa", "https://mail.company.com/owa/")]
        [InlineData("  https://mail.company.com/owa/  ", "https://mail.company.com/owa/")]
        public void PublicUrl_AcceptableValue_ShouldBeStoredNormalized(string val, string expected)
        {
            var settings = Settings();
            settings[ConfigurationKeys.PublicUrl] = val;

            var curr = TestableConfiguration.Reload(settings);

            Assert.Equal(expected, curr.PublicUrl);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void PublicUrl_NotDefined_ShouldReturnNull(string val)
        {
            var settings = Settings();
            settings[ConfigurationKeys.PublicUrl] = val;

            var curr = TestableConfiguration.Reload(settings);

            Assert.Null(curr.PublicUrl);
        }

        [Theory]
        [InlineData("not an url")]
        [InlineData("/owa")]
        [InlineData("http://mail.company.com/owa")]
        [InlineData("https://user:pwd@mail.company.com/owa")]
        [InlineData("https://mail.company.com/owa?a=b")]
        [InlineData("https://mail.company.com/owa#fragment")]
        public void PublicUrl_UnacceptableValue_ShouldThrow(string val)
        {
            var settings = Settings();
            settings[ConfigurationKeys.PublicUrl] = val;

            var ex = Assert.Throws<Exception>(() => TestableConfiguration.Reload(settings));

            Assert.Contains(ConfigurationKeys.PublicUrl, ex.Message);
        }
    }
}
