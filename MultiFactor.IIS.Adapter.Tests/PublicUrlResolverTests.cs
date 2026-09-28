using System;
using System.Web;
using Moq;
using MultiFactor.IIS.Adapter.Services;
using Xunit;

namespace MultiFactor.IIS.Adapter.Tests
{
    public class PublicUrlResolverTests : IDisposable
    {
        private const string ProxyHeader = "msExchProxyUri";

        public void Dispose() => TestableConfiguration.ResetCurrent();

        private static Mock<HttpContextBase> Context(string requestUrl, string applicationPath, string proxyHeader = null)
        {
            return HttpContextMockBuilder.Create(x =>
            {
                x.Request.SetupGet(r => r.Url).Returns(new Uri(requestUrl));
                x.Request.SetupGet(r => r.ApplicationPath).Returns(applicationPath);
                if (proxyHeader != null)
                {
                    x.Headers.Add(ProxyHeader, proxyHeader);
                }
            });
        }

        #region ResolveOwa

        [Fact]
        public void ResolveOwa_NullContext_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() => PublicUrlResolver.ResolveOwa(null));
        }

        [Fact]
        public void ResolveOwa_PublicUrlConfigured_ShouldReturnConfiguredValue()
        {
            TestableConfiguration.SetCurrentPublicUrl("https://configured.company.com/owa/");
            var context = Context("https://internal-exchange/owa/auth.owa", "/owa", "https://proxied.company.com/owa");

            var url = PublicUrlResolver.ResolveOwa(context.Object);

            Assert.Equal("https://configured.company.com/owa/", url);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ResolveOwa_PublicUrlNotConfigured_ShouldFallBackToProxyHeader(string configured)
        {
            TestableConfiguration.SetCurrentPublicUrl(configured);
            var context = Context("https://internal-exchange/owa/auth.owa", "/owa", "https://mail.company.com/owa");

            var url = PublicUrlResolver.ResolveOwa(context.Object);

            Assert.Equal("https://mail.company.com/owa/", url);
        }

        [Theory]
        //only the origin is taken from the header, the path of the requested page is dropped
        [InlineData("https://mail.company.com/owa/auth.owa", "/owa", "https://mail.company.com/owa/")]
        [InlineData("https://mail.company.com/owa?a=b#c", "/owa", "https://mail.company.com/owa/")]
        //credentials never leak into the callback url
        [InlineData("https://user:pwd@mail.company.com/owa", "/owa", "https://mail.company.com/owa/")]
        //a non default port belongs to the origin and is kept, the default one is dropped
        [InlineData("https://mail.company.com:8443/owa", "/owa", "https://mail.company.com:8443/owa/")]
        [InlineData("https://mail.company.com:443/owa", "/owa", "https://mail.company.com/owa/")]
        //the site is hosted in the root of the iis site
        [InlineData("https://mail.company.com/owa", "/", "https://mail.company.com/")]
        [InlineData("https://mail.company.com/owa", "", "https://mail.company.com/")]
        [InlineData("https://mail.company.com/owa", null, "https://mail.company.com/")]
        //extra whitespace of the proxied header
        [InlineData("  https://mail.company.com/owa  ", "/owa", "https://mail.company.com/owa/")]
        public void ResolveOwa_ProxyHeader_ShouldReturnOriginWithApplicationPath(string header, string applicationPath, string expected)
        {
            TestableConfiguration.SetCurrentPublicUrl(null);
            var context = Context("https://internal-exchange/owa/auth.owa", applicationPath, header);

            var url = PublicUrlResolver.ResolveOwa(context.Object);

            Assert.Equal(expected, url);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not an url")]
        [InlineData("/owa")]
        //the callback receives the access token, so plain http is not acceptable
        [InlineData("http://mail.company.com/owa")]
        [InlineData("ftp://mail.company.com/owa")]
        public void ResolveOwa_NoPublicUrlAndUnusableProxyHeader_ShouldThrow(string header)
        {
            TestableConfiguration.SetCurrentPublicUrl(null);
            var context = Context("https://internal-exchange/owa/auth.owa", "/owa", header);

            var ex = Assert.Throws<Exception>(() => PublicUrlResolver.ResolveOwa(context.Object));

            Assert.Contains(ConfigurationKeys.PublicUrl, ex.Message);
            Assert.Contains(ProxyHeader, ex.Message);
        }

        #endregion

        #region ResolveCrm

        [Fact]
        public void ResolveCrm_NullContext_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() => PublicUrlResolver.ResolveCrm(null));
        }

        [Fact]
        public void ResolveCrm_PublicUrlConfigured_ShouldReturnConfiguredValue()
        {
            TestableConfiguration.SetCurrentPublicUrl("https://configured.company.com/CRM/");
            var context = Context("https://crm.company.com/main.aspx", "/");

            var url = PublicUrlResolver.ResolveCrm(context.Object);

            Assert.Equal("https://configured.company.com/CRM/", url);
        }

        [Theory]
        //the path and the query of the current request are dropped
        [InlineData("https://crm.company.com/main.aspx?pagetype=entitylist", "/", "https://crm.company.com/")]
        [InlineData("https://crm.company.com/main.aspx", "", "https://crm.company.com/")]
        [InlineData("https://crm.company.com/main.aspx", null, "https://crm.company.com/")]
        //the application path of a site hosted in a virtual directory is kept
        [InlineData("https://crm.company.com/CRM/main.aspx", "/CRM", "https://crm.company.com/CRM/")]
        [InlineData("https://crm.company.com/CRM/main.aspx", "/CRM/", "https://crm.company.com/CRM/")]
        //a non default port is kept, the default one is dropped
        [InlineData("https://crm.company.com:444/main.aspx", "/", "https://crm.company.com:444/")]
        [InlineData("https://crm.company.com:443/main.aspx", "/", "https://crm.company.com/")]
        //unlike the proxied header, the scheme of the current request is taken as is
        [InlineData("http://crm.company.local/main.aspx", "/", "http://crm.company.local/")]
        public void ResolveCrm_PublicUrlNotConfigured_ShouldBuildFromRequest(string requestUrl, string applicationPath, string expected)
        {
            TestableConfiguration.SetCurrentPublicUrl(null);
            var context = Context(requestUrl, applicationPath);

            var url = PublicUrlResolver.ResolveCrm(context.Object);

            Assert.Equal(expected, url);
        }

        [Fact]
        public void ResolveCrm_PublicUrlIsWhitespace_ShouldBuildFromRequest()
        {
            TestableConfiguration.SetCurrentPublicUrl("   ");
            var context = Context("https://crm.company.com/main.aspx", "/");

            var url = PublicUrlResolver.ResolveCrm(context.Object);

            Assert.Equal("https://crm.company.com/", url);
        }

        #endregion

        #region Normalize

        [Theory]
        [InlineData("https://mail.company.com/owa", "https://mail.company.com/owa/")]
        [InlineData("https://mail.company.com/owa/", "https://mail.company.com/owa/")]
        [InlineData("https://mail.company.com", "https://mail.company.com/")]
        [InlineData("  https://mail.company.com/owa  ", "https://mail.company.com/owa/")]
        [InlineData("HTTPS://MAIL.company.com/owa", "https://mail.company.com/owa/")]
        [InlineData("https://mail.company.com:443/owa", "https://mail.company.com/owa/")]
        [InlineData("https://mail.company.com:8443/owa", "https://mail.company.com:8443/owa/")]
        public void Normalize_AcceptableValue_ShouldReturnNormalizedUrl(string value, string expected)
        {
            Assert.Equal(expected, PublicUrlResolver.Normalize(value));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("mail.company.com/owa")]
        [InlineData("/owa")]
        [InlineData("http://mail.company.com/owa")]
        [InlineData("ftp://mail.company.com/owa")]
        [InlineData("https://user:pwd@mail.company.com/owa")]
        [InlineData("https://mail.company.com/owa?a=b")]
        [InlineData("https://mail.company.com/owa#fragment")]
        public void Normalize_UnacceptableValue_ShouldReturnNull(string value)
        {
            Assert.Null(PublicUrlResolver.Normalize(value));
        }

        #endregion
    }
}
