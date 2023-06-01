using Moq;
using System;
using Xunit;

namespace MultiFactor.IIS.Adapter.Tests
{
    public class OwaModuleTests
    {
        [Fact]
        public void OnBeginRequest_ContainsToken_ShouldSetupCookieAndRedirect()
        {
            const string expectedRedirect = "https://exchange/owa";
            var context = HttpContextMockBuilder.Create(x =>
            {
                x.Request.SetupGet(r => r.Url).Returns(new Uri("https://exchange/owa/mfa.aspx"));
                x.Request.SetupGet(r => r.ApplicationPath).Returns(expectedRedirect);
                x.Form.Add("AccessToken", "MFA TOKEN");
            });
            var module = new Owa.Module();

            module.OnBeginRequest(context.Object);

            Assert.Single(context.Object.Response.Cookies.AllKeys, Constants.COOKIE_NAME);
            context.Verify(x => x.Response.Redirect(expectedRedirect, true), Times.Once);
        }
    }
}
