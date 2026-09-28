using System;
using System.Net;
using Xunit;

namespace MultiFactor.IIS.Adapter.Tests
{
    public class MultiFactorApiClientTests
    {
        [Fact]
        public void ApplyProxy_WithConfiguredUrl_ShouldSetExplicitProxy()
        {
            using (var webClient = new WebClient { Proxy = null })
            {
                Services.MultiFactorApiClient.TryApplyProxy(webClient, "http://proxy.example.test:3128");

                var proxy = Assert.IsType<WebProxy>(webClient.Proxy);
                Assert.Equal(new Uri("http://proxy.example.test:3128"), proxy.Address);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ApplyProxy_WithoutConfiguredUrl_ShouldKeepExistingProxy(string proxyUrl)
        {
            var existingProxy = new WebProxy("http://system-proxy.example.test:8080");
            using (var webClient = new WebClient { Proxy = existingProxy })
            {
                Services.MultiFactorApiClient.TryApplyProxy(webClient, proxyUrl);

                Assert.Same(existingProxy, webClient.Proxy);
            }
        }
    }
}