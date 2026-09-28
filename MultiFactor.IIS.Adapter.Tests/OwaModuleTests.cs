using Moq;
using System;
using System.Web;
using Xunit;

namespace MultiFactor.IIS.Adapter.Tests
{
    public class OwaModuleTests
    {
        private const string TokenField = "AccessToken";

        private static Mock<HttpContextBase> Context(string requestUrl, string token, Action<string> onWrite = null)
        {
            return HttpContextMockBuilder.Create(x =>
            {
                x.Request.SetupGet(r => r.Url).Returns(new Uri(requestUrl));
                if (token != null)
                {
                    x.Form.Add(TokenField, token);
                }
                if (onWrite != null)
                {
                    x.Response.Setup(r => r.Write(It.IsAny<string>())).Callback(onWrite);
                }
            });
        }

        [Fact]
        public void OnBeginRequest_ContainsToken_ShouldSendPageCompletingTheSecondFactor()
        {
            const string token = "MFA TOKEN";
            string html = null;
            var context = Context("https://exchange/owa/", token, written => html = written);

            new Owa.Module().OnBeginRequest(context.Object);

            Assert.NotNull(html);
            Assert.Contains(token, html);
            Assert.DoesNotContain("%MULTIFACTOR_COOKIE%", html);
            context.Verify(x => x.Response.Clear(), Times.Once);
            context.Verify(x => x.Response.End(), Times.Once);
            Assert.Empty(context.Object.Response.Cookies);
        }

        [Fact]
        public void OnBeginRequest_NoToken_ShouldDoNothing()
        {
            var context = Context("https://exchange/owa/", null);

            new Owa.Module().OnBeginRequest(context.Object);

            context.Verify(x => x.Response.Write(It.IsAny<string>()), Times.Never);
            context.Verify(x => x.Response.End(), Times.Never);
        }

        [Fact]
        public void OnBeginRequest_LanguageSelectionRequest_ShouldDoNothing()
        {
            var context = Context("https://exchange/owa/lang.owa", "MFA TOKEN");

            new Owa.Module().OnBeginRequest(context.Object);

            context.Verify(x => x.Response.Write(It.IsAny<string>()), Times.Never);
            context.Verify(x => x.Response.End(), Times.Never);
        }
    }
}
