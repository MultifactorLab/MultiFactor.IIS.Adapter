using System;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Web;
using Moq;
using MultiFactor.IIS.Adapter.Dto;
using MultiFactor.IIS.Adapter.Services;
using MultiFactor.IIS.Adapter.Services.Ldap;
using Xunit;

namespace MultiFactor.IIS.Adapter.Tests
{
    [Collection(ConfigurationCollection.Name)]
    public class MfaApiRequestExecutorTests : IDisposable
    {
        private const string CallbackUrl = "https://mail.example.com/owa/";
        private const string AccessPageUrl = "https://access.multifactor.ru/page";

        public void Dispose() => TestableConfiguration.ResetCurrent();

        private static void SetConfiguration(bool bypassWhenApiUnreachable)
        {
            TestableConfiguration.SetCurrent(new NameValueCollection
            {
                { ConfigurationKeys.ActiveDirectoryDomain, "example.test" },
                { ConfigurationKeys.ApiUrl, "https://api.multifactor.ru" },
                { ConfigurationKeys.ApiKey, "key" },
                { ConfigurationKeys.ApiSecret, "secret" },
                { ConfigurationKeys.BypassSecondFactorWhenApiUnreachable, bypassWhenApiUnreachable.ToString() }
            });
        }

        private static Mock<HttpContextBase> Context(string userName, Action<string> onWrite = null)
        {
            var context = HttpContextMockBuilder.Create(x =>
            {
                if (onWrite != null)
                {
                    x.Response.Setup(r => r.Write(It.IsAny<string>())).Callback(onWrite);
                }
            });
            context.SetupGet(x => x.User).Returns(new GenericPrincipal(new GenericIdentity(userName), new string[0]));
            return context;
        }

        private static MfaApiRequestExecutor Executor(HttpContextBase context, StubAccessUrl accessUrl)
        {
            return new MfaApiRequestExecutor(context, accessUrl, Logger.Owa);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute_AccessPageCreated_ShouldRedirectToAccessPage(bool bypass)
        {
            SetConfiguration(bypass);
            var context = Context($"CONTOSO\\executor-success-{bypass}");
            var accessUrl = new StubAccessUrl();
            accessUrl.GetResult = () => AccessPageUrl;

            Executor(context.Object, accessUrl).Execute(CallbackUrl);

            context.Verify(x => x.Response.Redirect(AccessPageUrl, true), Times.Once);
        }

        [Fact]
        public void Execute_ApiUnreachableAndBypassEnabled_ShouldSetFlagAndRedirectToCallback()
        {
            SetConfiguration(true);
            const string userName = "CONTOSO\\executor-unreachable-open";
            var context = Context(userName);
            var accessUrl = new StubAccessUrl();
            accessUrl.GetResult = () => throw new Exception($"{Constants.API_UNREACHABLE_CODE} host unreachable");

            Executor(context.Object, accessUrl).Execute(CallbackUrl);

            context.Verify(x => x.Response.Redirect(CallbackUrl, true), Times.Once);
            Assert.True(new CacheAdapter(context.Object).GetApiUnreachable(userName));
        }

        [Fact]
        public void Crm_ApiUnreachable_ShouldAcceptNextRequestForDomainUser()
        {
            SetConfiguration(true);
            const string userName = "CONTOSO\\crm-unreachable";
            const string callbackUrl = "https://crm.example.com/";
            var initialRequest = Context(userName);
            var accessUrl = new StubAccessUrl
            {
                GetResult = () => throw new Exception($"{Constants.API_UNREACHABLE_CODE} host unreachable")
            };

            new MfaApiRequestExecutor(initialRequest.Object, accessUrl, Logger.IIS).Execute(callbackUrl);

            initialRequest.Verify(x => x.Response.Redirect(callbackUrl, true), Times.Once);

            var nextRequest = HttpContextMockBuilder.Create(x =>
            {
                x.Request.SetupGet(r => r.Url).Returns(new Uri(callbackUrl));
                x.Request.SetupGet(r => r.Cookies).Returns(x.RequestCookies);
                x.Headers["x-requested-with"] = "XMLHttpRequest";
            });
            nextRequest.SetupGet(x => x.User).Returns(initialRequest.Object.User);

            new MsDynamics365.Module().OnPostAuthorizeRequest(nextRequest.Object);

            nextRequest.Verify(x => x.Response.End(), Times.Never);
            nextRequest.Verify(x => x.Response.Redirect(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public void Execute_ApiUnreachableAndBypassDisabled_ShouldThrowAndNotLetRequestThrough()
        {
            SetConfiguration(false);
            const string userName = "CONTOSO\\executor-unreachable-closed";
            var context = Context(userName);
            var accessUrl = new StubAccessUrl();
            accessUrl.GetResult = () => throw new Exception($"{Constants.API_UNREACHABLE_CODE} host unreachable");

            var ex = Assert.Throws<Exception>(() => Executor(context.Object, accessUrl).Execute(CallbackUrl));

            Assert.StartsWith(Constants.API_UNREACHABLE_CODE, ex.Message);
            context.Verify(x => x.Response.Redirect(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            Assert.False(new CacheAdapter(context.Object).GetApiUnreachable(userName));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute_UnexpectedApiError_ShouldThrowRegardlessOfBypass(bool bypass)
        {
            SetConfiguration(bypass);
            var context = Context($"CONTOSO\\executor-quota-{bypass}");
            var accessUrl = new StubAccessUrl();
            accessUrl.GetResult = () => throw new Exception($"{Constants.API_USERS_QUOTA_EXCEEDED_CODE} Users quota exceeded");

            Assert.Throws<Exception>(() => Executor(context.Object, accessUrl).Execute(CallbackUrl));

            context.Verify(x => x.Response.Redirect(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public void Execute_UserNotRegistered_ShouldSendInfoPage()
        {
            SetConfiguration(false);
            string html = null;
            var context = Context("CONTOSO\\executor-not-registered", written => html = written);
            var accessUrl = new StubAccessUrl();
            accessUrl.GetResult = () => throw new Exception($"{Constants.API_NOT_REGISTERED_CODE} UserNotRegistered");
            accessUrl.InfoResult = new ScopeSupportInfoDto { AdminName = "Support Admin" };

            Executor(context.Object, accessUrl).Execute(CallbackUrl);

            Assert.NotNull(html);
            Assert.Contains("Support Admin", html);
            context.Verify(x => x.Response.End(), Times.Once);
            context.Verify(x => x.Response.Redirect(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        private class StubAccessUrl : IAccessUrl
        {
            public Func<string> GetResult { get; set; } = () => AccessPageUrl;
            public ScopeSupportInfoDto InfoResult { get; set; } = new ScopeSupportInfoDto();

            public string Get(LdapIdentity identity, string postbackUrl) => GetResult();

            public ScopeSupportInfoDto Info() => InfoResult;
        }
    }

    [CollectionDefinition(Name, DisableParallelization = true)]
    public class ConfigurationCollection
    {
        public const string Name = "Configuration.Current";
    }
}
