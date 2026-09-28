using Xunit;

namespace MultiFactor.IIS.Adapter.Tests
{
    public class LdapFilterTests
    {
        [Theory]
        [InlineData("alice", "alice")]
        [InlineData("EAS (2FA)", "EAS \\282FA\\29")]
        [InlineData("a*b", "a\\2ab")]
        [InlineData("a\\b", "a\\5cb")]
        [InlineData("a\0b", "a\\00b")]
        public void Escape_ShouldEncodeLdapFilterSpecialCharacters(string value, string expected)
        {
            Assert.Equal(expected, Services.Ldap.LdapFilter.Escape(value));
        }
    }
}