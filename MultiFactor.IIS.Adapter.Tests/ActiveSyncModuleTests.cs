using Moq;
using System;
using System.Net;
using System.Runtime.Caching;
using System.Threading.Tasks;
using Xunit;

namespace MultiFactor.IIS.Adapter.Tests
{
    public class ActiveSyncModuleTests
    {
        [Fact]
        public async Task OnProvision_UserOutsideConfiguredGroup_ShouldBypassSecondFactor()
        {
            using (var cache = CreateCache())
            {
                var module = new TestModule(cache) { IsSecondFactorRequired = false };
                var context = CreateInitialProvisionContext("CONTOSO\\alice", "device-1");

                await module.OnProvision(context.Object);

                Assert.Equal(1, module.GroupCheckCount);
                Assert.Equal(0, module.StartSecondFactorCount);
                context.VerifySet(x => x.Response.StatusCode = 440, Times.Never);
            }
        }

        [Fact]
        public async Task OnProvision_GrantedSession_ShouldBeCachedPerUserAndDevice()
        {
            using (var cache = CreateCache())
            {
                var module = new TestModule(cache) { SecondFactorResult = true };
                var context = CreateInitialProvisionContext("CONTOSO\\alice", "device-1");

                await module.OnProvision(context.Object);
                await module.OnProvision(context.Object);
                context.Object.Request.Params["DeviceId"] = "device-2";
                await module.OnProvision(context.Object);

                Assert.Equal(2, module.StartSecondFactorCount);
            }
        }

        [Fact]
        public async Task OnProvision_DeniedSession_ShouldBeCachedAndReturn440()
        {
            using (var cache = CreateCache())
            {
                var module = new TestModule(cache) { SecondFactorResult = false };
                var context = CreateInitialProvisionContext("CONTOSO\\alice", "device-1");

                await module.OnProvision(context.Object);
                await module.OnProvision(context.Object);

                Assert.Equal(1, module.StartSecondFactorCount);
                context.VerifySet(x => x.Response.StatusCode = 440, Times.Exactly(2));
            }
        }

        [Theory]
        [InlineData("Sync", "0")]
        [InlineData("Provision", "123")]
        public async Task OnProvision_NotInitialProvision_ShouldSkipRequest(string command, string policyKey)
        {
            using (var cache = CreateCache())
            {
                var module = new TestModule(cache);
                var context = CreateInitialProvisionContext("CONTOSO\\alice", "device-1");
                context.Object.Request.Params["Cmd"] = command;
                context.Object.Request.Headers["X-MS-PolicyKey"] = policyKey;

                await module.OnProvision(context.Object);

                Assert.Equal(0, module.GroupCheckCount);
                Assert.Equal(0, module.StartSecondFactorCount);
            }
        }

        [Fact]
        public void GetCanonicalizedUserName_ShouldUseTrimmedProxyIdentityWhenUserHasNoDomain()
        {
            var context = CreateInitialProvisionContext("alice", "device-1");
            context.Object.Request.Params["HTTP_X_EAS_PROXY"] = "proxy-node, CONTOSO\\alice ";

            var result = ActiveSync.Module.GetCanonicalizedUserName(context.Object);

            Assert.Equal("contoso\\alice", result);
        }

        [Fact]
        public async Task CreateAccessRequest_ApiUnreachableAndBypassEnabled_ShouldFailOpenTemporarily()
        {
            using (var cache = CreateCache())
            {
                var module = new TestModule(cache, bypassWhenApiUnreachable: true)
                {
                    AccessRequestException = new WebException("network unreachable")
                };
                var context = CreateInitialProvisionContext("CONTOSO\\network-open-user", "device-1");

                var result = module.CreateAccessRequest(
                    context.Object,
                    "network-open-user",
                    null,
                    null,
                    "CONTOSO\\network-open-user");
                await module.OnProvision(context.Object);

                Assert.Equal(Services.AccessRequestStatus.Bypassed, result.Status);
                Assert.Equal(0, module.StartSecondFactorCount);
            }
        }

        [Fact]
        public void CreateAccessRequest_ApiUnreachableAndBypassDisabled_ShouldFailClosed()
        {
            using (var cache = CreateCache())
            {
                var module = new TestModule(cache, bypassWhenApiUnreachable: false)
                {
                    AccessRequestException = new WebException("network unreachable")
                };
                var context = CreateInitialProvisionContext("CONTOSO\\network-closed-user", "device-1");

                var result = module.CreateAccessRequest(
                    context.Object,
                    "network-closed-user",
                    null,
                    null,
                    "CONTOSO\\network-closed-user");

                Assert.Equal(Services.AccessRequestStatus.Denied, result.Status);
            }
        }

        private static Mock<System.Web.HttpContextBase> CreateInitialProvisionContext(string userName, string deviceId)
        {
            return HttpContextMockBuilder.Create(x =>
            {
                x.Request.SetupGet(r => r.HttpMethod).Returns("POST");
                x.Request.SetupGet(r => r.Url).Returns(new Uri("https://exchange/Microsoft-Server-ActiveSync"));
                x.Request.SetupGet(r => r.Path).Returns("/Microsoft-Server-ActiveSync");
                x.Request.SetupGet(r => r.ApplicationPath).Returns("/Microsoft-Server-ActiveSync");
                x.Params["User"] = userName;
                x.Params["DeviceId"] = deviceId;
                x.Params["Cmd"] = "Provision";
                x.Headers["X-MS-PolicyKey"] = "0";
            });
        }

        private static MemoryCache CreateCache()
        {
            return new MemoryCache("eas-tests-" + Guid.NewGuid().ToString("N"));
        }

        private sealed class TestModule : ActiveSync.Module
        {
            public bool IsSecondFactorRequired { get; set; } = true;
            public bool SecondFactorResult { get; set; } = true;
            public int GroupCheckCount { get; private set; }
            public int StartSecondFactorCount { get; private set; }
            public Exception AccessRequestException { get; set; }

            public TestModule(ObjectCache cache, bool bypassWhenApiUnreachable = true)
                : base(
                    cache,
                    () => TimeSpan.FromMinutes(10),
                    () => TimeSpan.FromMinutes(5),
                    () => bypassWhenApiUnreachable,
                    () => "https://api.multifactor.ru",
                    () => TimeSpan.FromMinutes(15))
            {
            }

            internal override bool SecondFactorIsRequired(System.Web.HttpContextBase context, string userName)
            {
                GroupCheckCount++;
                return IsSecondFactorRequired;
            }

            internal override bool StartSecondFactorAuth(System.Web.HttpContextBase context, string userName)
            {
                StartSecondFactorCount++;
                return SecondFactorResult;
            }

            internal override Services.MultiFactorAccessRequest SendAccessRequest(
                string identity,
                string email,
                string phone)
            {
                if (AccessRequestException != null)
                {
                    throw AccessRequestException;
                }

                return new Services.MultiFactorAccessRequest { Status = Services.AccessRequestStatus.Granted };
            }
        }
    }
}