using Moq;
using MultiFactor.IIS.Adapter.ActiveSync.AsyncLocker;
using MultiFactor.IIS.Adapter.Extensions;
using MultiFactor.IIS.Adapter.Services;
using MultiFactor.IIS.Adapter.Services.Ldap;
using MultiFactor.IIS.Adapter.Services.Ldap.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Xunit;

namespace MultiFactor.IIS.Adapter.Tests
{
    public class NewFunctionalityTests
    {
        [Theory]
        [InlineData("GET", "Provision", "0")]
        [InlineData("POST", "Sync", "0")]
        [InlineData("POST", "Provision", "123")]
        public void ShouldSkipRequest_ShouldRejectNonInitialProvision(string method, string command, string policyKey)
        {
            var context = CreateProvisionContext("CONTOSO\\alice", "device-1", method, command, policyKey);

            Assert.True(ActiveSync.Module.ShouldSkipRequest(context.Object));
        }

        [Theory]
        [InlineData("HealthMailbox123")]
        [InlineData("CONTOSO\\SystemMailbox{guid}")]
        [InlineData("migration.8f3e7716")]
        [InlineData("FederatedEmail.4c1f4d8b")]
        [InlineData("extest_123")]
        [InlineData("CONTOSO\\HealthMailbox9a1")]
        [InlineData("contoso.com\\Migration.7c2")]
        [InlineData("HealthMailbox123@contoso.com")]
        public void ShouldSkipRequest_ShouldIgnoreExchangeSystemMailboxes(string userName)
        {
            var context = CreateProvisionContext(userName, "device-1");

            Assert.True(ActiveSync.Module.ShouldSkipRequest(context.Object));
        }

        [Theory]
        [InlineData("john.migration")]
        [InlineData("CONTOSO\\svc-extest-runner")]
        [InlineData("mail.healthmailbox")]
        public void ShouldSkipRequest_ShouldNotIgnoreUsersContainingSystemMailboxPrefix(string userName)
        {
            var context = CreateProvisionContext(userName, "device-1");

            Assert.False(ActiveSync.Module.ShouldSkipRequest(context.Object));
        }

        [Theory]
        [InlineData("CONTOSO\\Alice", "contoso\\alice")]
        [InlineData("Alice@Contoso.COM", "contoso.com\\alice")]
        [InlineData("alice", "alice")]
        public void GetCanonicalizedUserName_ShouldNormalizeSupportedIdentityForms(string input, string expected)
        {
            var context = CreateProvisionContext(input, "device-1");

            Assert.Equal(expected, ActiveSync.Module.GetCanonicalizedUserName(context.Object));
        }

        [Fact]
        public void GetCanonicalizedUserName_ShouldUseLastProxyHopForUnqualifiedUser()
        {
            var context = CreateProvisionContext("alice", "device-1");
            context.Object.Request.Params["HTTP_X_EAS_PROXY"] = "frontend, proxy, CONTOSO\\Alice ";

            Assert.Equal("contoso\\alice", ActiveSync.Module.GetCanonicalizedUserName(context.Object));
        }

        [Fact]
        public void BuildCacheKey_ShouldNormalizeCaseWhitespaceAndMissingDevice()
        {
            var withDevice = CreateProvisionContext("CONTOSO\\Alice", " Device-01 ");
            var withoutDevice = CreateProvisionContext("CONTOSO\\Alice", null);

            Assert.Equal(
                "multifactor:eas:contoso\\alice:device-01",
                ActiveSync.Module.BuildCacheKey("CONTOSO\\Alice", withDevice.Object));
            Assert.Equal(
                "multifactor:eas:contoso\\alice:unknown-device",
                ActiveSync.Module.BuildCacheKey("CONTOSO\\Alice", withoutDevice.Object));
        }

        [Fact]
        public async Task OnProvision_MissingUser_ShouldDenyWithoutStartingSecondFactor()
        {
            using (var cache = CreateCache())
            {
                var module = new ConcurrentTestModule(cache);
                var context = CreateProvisionContext(null, "device-1");

                await module.OnProvision(context.Object);

                Assert.Equal(0, module.StartSecondFactorCount);
                context.VerifySet(x => x.Response.StatusCode = 440, Times.Once);
                context.Verify(x => x.Response.End(), Times.Once);
            }
        }

        [Fact]
        public async Task OnProvision_ConcurrentRequestsForSameDevice_ShouldSendOnlyOnePush()
        {
            using (var cache = CreateCache())
            {
                var module = new ConcurrentTestModule(cache) { SecondFactorDelayMilliseconds = 150 };
                var tasks = Enumerable.Range(0, 12)
                    .Select(_ => Task.Run(() => module.OnProvision(CreateProvisionContext("CONTOSO\\parallel", "same-device").Object)))
                    .ToArray();

                await Task.WhenAll(tasks);

                Assert.Equal(1, module.StartSecondFactorCount);
                Assert.Equal(1, module.MaximumConcurrentSecondFactors);
            }
        }

        [Fact]
        public async Task OnProvision_ConcurrentDifferentDevices_ShouldNotBlockEachOther()
        {
            using (var cache = CreateCache())
            using (var rendezvous = new CountdownEvent(4))
            {
                var module = new ConcurrentTestModule(cache) { SecondFactorRendezvous = rendezvous };
                var tasks = Enumerable.Range(0, 4)
                    .Select(i => Task.Factory.StartNew(
                        () => module.OnProvision(CreateProvisionContext("CONTOSO\\parallel-devices", "device-" + i).Object),
                        CancellationToken.None,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Default).Unwrap())
                    .ToArray();

                await Task.WhenAll(tasks);

                Assert.Equal(4, module.StartSecondFactorCount);
                Assert.Equal(4, module.MaximumConcurrentSecondFactors);
            }
        }

        [Fact]
        public async Task OnProvision_GrantedCacheExpiry_ShouldAllowNewPush()
        {
            using (var cache = CreateCache())
            {
                var module = new ConcurrentTestModule(cache, TimeSpan.FromMilliseconds(80), TimeSpan.FromMinutes(1));
                var context = CreateProvisionContext("CONTOSO\\cache-expiry", "device-1");

                await module.OnProvision(context.Object);
                await Task.Delay(180);
                await module.OnProvision(context.Object);

                Assert.Equal(2, module.StartSecondFactorCount);
            }
        }

        [Fact]
        public async Task OnProvision_DeniedCacheExpiry_ShouldAllowNewPushAndDenyAgain()
        {
            using (var cache = CreateCache())
            {
                var module = new ConcurrentTestModule(cache, TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(80))
                {
                    SecondFactorResult = false
                };
                var context = CreateProvisionContext("CONTOSO\\denied-expiry", "device-1");

                await module.OnProvision(context.Object);
                await Task.Delay(180);
                await module.OnProvision(context.Object);

                Assert.Equal(2, module.StartSecondFactorCount);
                context.VerifySet(x => x.Response.StatusCode = 440, Times.Exactly(2));
            }
        }

        [Fact]
        public async Task AsyncLocker_ShouldSerializeSameKeyAndReleaseAfterDispose()
        {
            var locker = new AsyncLocker<string>();
            var concurrent = 0;
            var maximumConcurrent = 0;

            var tasks = Enumerable.Range(0, 200).Select(async _ =>
            {
                using (await locker.LockAsync("same-key"))
                {
                    var current = Interlocked.Increment(ref concurrent);
                    UpdateMaximum(ref maximumConcurrent, current);
                    await Task.Delay(2);
                    Interlocked.Decrement(ref concurrent);
                }
            });

            await Task.WhenAll(tasks);

            Assert.Equal(1, maximumConcurrent);
            Assert.Equal(0, locker.TrackedKeyCount);
        }

        [Fact]
        public async Task AsyncLocker_ShouldRemoveEveryUniqueKeyAfterRelease()
        {
            var locker = new AsyncLocker<string>();

            for (var index = 0; index < 10000; index++)
            {
                using (await locker.LockAsync("device-" + index))
                {
                }
            }

            Assert.Equal(0, locker.TrackedKeyCount);
        }

        [Fact]
        public async Task AsyncLocker_DisposableAction_ShouldBeIdempotent()
        {
            var locker = new AsyncLocker<string>();
            var handle = await locker.LockAsync("idempotent");

            handle.Dispose();
            handle.Dispose();

            Assert.Equal(0, locker.TrackedKeyCount);
        }

        [Fact]
        public void LdapProfile_ShouldExposeConfiguredPhoneAndMail()
        {
            var settings = CreateSettings();
            settings[ConfigurationKeys.PhoneAttribute] = "mobile;otherTelephone";
            var configuration = TestableConfiguration.Reload(settings);
            var profile = new LdapProfile(LdapIdentity.Parse("CONTOSO\\Alice"), configuration);
            profile.AddAttribute("mobile", new[] { "+79990000000" });
            profile.AddAttribute("mail", new[] { "alice@example.test" });

            Assert.Equal("+79990000000", profile.Phone);
            Assert.Equal("alice@example.test", profile.Email);
        }

        [Fact]
        public void LdapProfile_ShouldUseEmailFallbackCaseInsensitively()
        {
            var profile = new LdapProfile(LdapIdentity.Parse("alice"), TestableConfiguration.Reload(CreateSettings()));
            profile.AddAttribute("EMAIL", new[] { "fallback@example.test" });

            Assert.Equal("fallback@example.test", profile.Email);
        }

        [Fact]
        public void RequestLogger_ShouldLogCoreFieldsOnce()
        {
            var context = CreateProvisionContext("CONTOSO\\alice", "device-1");
            context.Object.Request.Params["DeviceType"] = "iPhone";
            context.Object.Request.Params["HTTP_USER_AGENT"] = "Test-Agent";

            var log = RequestLoggerBuilder.BuildRequestLog(context.Object);

            Assert.Equal(1, CountOccurrences(log, "User:"));
            Assert.Equal(1, CountOccurrences(log, "DeviceId:"));
            Assert.Equal(1, CountOccurrences(log, "Cmd:"));
            Assert.Contains("Device Type = iPhone", log);
            Assert.Contains("Device UserAgent = Test-Agent", log);
        }

        [Fact]
        public async Task ApiUnreachableCache_ShouldExpireAtInjectedTtl()
        {
            var context = HttpContextMockBuilder.Create();
            var cache = new CacheAdapter(context.Object);
            var identity = "ttl-user-" + Guid.NewGuid().ToString("N");

            cache.SetApiUnreachable(identity, true, TimeSpan.FromMilliseconds(80));
            Assert.True(cache.GetApiUnreachable(identity));

            await Task.Delay(250);

            Assert.False(cache.GetApiUnreachable(identity));
        }

        [Fact]
        public void CreateAccessRequest_Http429WithBypassEnabled_ShouldDenyAndNotSetFailOpenCache()
        {
            var exception = CreateHttpError((HttpStatusCode)429);
            using (var memoryCache = CreateCache())
            {
                var module = new ConcurrentTestModule(memoryCache) { AccessRequestException = exception };
                var context = CreateProvisionContext("CONTOSO\\rate-limited", "device-1");

                var result = module.CreateAccessRequest(
                    context.Object,
                    "rate-limited",
                    null,
                    null,
                    "CONTOSO\\rate-limited");

                Assert.Equal(AccessRequestStatus.Denied, result.Status);
                Assert.False(context.Object.GetCacheAdapter().GetApiUnreachable("rate-limited"));
            }
        }

        [Fact]
        public void CreateAccessRequest_UnexpectedException_ShouldFailClosed()
        {
            using (var memoryCache = CreateCache())
            {
                var module = new ConcurrentTestModule(memoryCache)
                {
                    AccessRequestException = new InvalidOperationException("bad response")
                };
                var context = CreateProvisionContext("CONTOSO\\unexpected", "device-1");

                var result = module.CreateAccessRequest(
                    context.Object,
                    "unexpected",
                    null,
                    null,
                    "CONTOSO\\unexpected");

                Assert.Equal(AccessRequestStatus.Denied, result.Status);
            }
        }

        [Theory]
        [InlineData("CONTOSO\\Alice", "contoso")]
        [InlineData("Alice@Contoso.COM", "contoso.com")]
        [InlineData("alice", null)]
        [InlineData(null, null)]
        public void GetUserDomain_ShouldHandleSupportedFormats(string identity, string expected)
        {
            Assert.Equal(expected, Util.GetUserDomain(identity));
        }

        private static Mock<HttpContextBase> CreateProvisionContext(
            string userName,
            string deviceId,
            string method = "POST",
            string command = "Provision",
            string policyKey = "0")
        {
            return HttpContextMockBuilder.Create(x =>
            {
                x.Request.SetupGet(r => r.HttpMethod).Returns(method);
                x.Request.SetupGet(r => r.Url).Returns(new Uri("https://exchange/Microsoft-Server-ActiveSync"));
                x.Request.SetupGet(r => r.Path).Returns("/Microsoft-Server-ActiveSync");
                x.Request.SetupGet(r => r.ApplicationPath).Returns("/Microsoft-Server-ActiveSync");
                x.Params["User"] = userName;
                x.Params["DeviceId"] = deviceId;
                x.Params["Cmd"] = command;
                x.Headers["X-MS-PolicyKey"] = policyKey;
            });
        }

        private static MemoryCache CreateCache()
        {
            return new MemoryCache("eas-extended-tests-" + Guid.NewGuid().ToString("N"));
        }

        private static System.Collections.Specialized.NameValueCollection CreateSettings()
        {
            return new System.Collections.Specialized.NameValueCollection
            {
                [ConfigurationKeys.ApiUrl] = "https://api.multifactor.ru",
                [ConfigurationKeys.ApiKey] = "key",
                [ConfigurationKeys.ApiSecret] = "secret",
                [ConfigurationKeys.ActiveDirectoryDomain] = "example.test"
            };
        }

        private static int CountOccurrences(string value, string fragment)
        {
            var count = 0;
            var offset = 0;
            while ((offset = value.IndexOf(fragment, offset, StringComparison.Ordinal)) >= 0)
            {
                count++;
                offset += fragment.Length;
            }

            return count;
        }

        private static WebException CreateHttpError(HttpStatusCode statusCode)
        {
            var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var server = Task.Run(() =>
            {
                using (var client = listener.AcceptTcpClient())
                using (var stream = client.GetStream())
                {
                    var buffer = new byte[4096];
                    stream.Read(buffer, 0, buffer.Length);
                    var response = Encoding.ASCII.GetBytes(
                        "HTTP/1.1 " + (int)statusCode + " Error\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
                    stream.Write(response, 0, response.Length);
                }
            });

            try
            {
                using (var webClient = new WebClient())
                {
                    webClient.DownloadString("http://127.0.0.1:" + port + "/error");
                }
            }
            catch (WebException exception)
            {
                Assert.True(server.Wait(TimeSpan.FromSeconds(5)));
                return exception;
            }
            finally
            {
                listener.Stop();
            }

            throw new Exception("Expected WebException was not thrown");
        }

        private static void UpdateMaximum(ref int maximum, int current)
        {
            int observed;
            do
            {
                observed = maximum;
                if (current <= observed)
                {
                    return;
                }
            }
            while (Interlocked.CompareExchange(ref maximum, current, observed) != observed);
        }

        private sealed class ConcurrentTestModule : ActiveSync.Module
        {
            private int _startSecondFactorCount;
            private int _currentSecondFactors;
            private int _maximumConcurrentSecondFactors;

            public bool SecondFactorResult { get; set; } = true;
            public int SecondFactorDelayMilliseconds { get; set; }
            //when set, every second factor waits for all the others instead of sleeping
            public CountdownEvent SecondFactorRendezvous { get; set; }
            public int StartSecondFactorCount => _startSecondFactorCount;
            public int MaximumConcurrentSecondFactors => _maximumConcurrentSecondFactors;
            public Exception AccessRequestException { get; set; }

            public ConcurrentTestModule(
                ObjectCache cache,
                TimeSpan? grantedLifetime = null,
                TimeSpan? deniedLifetime = null)
                : base(
                    cache,
                    () => grantedLifetime ?? TimeSpan.FromMinutes(10),
                    () => deniedLifetime ?? TimeSpan.FromMinutes(5),
                    () => true,
                    () => "https://api.multifactor.ru",
                    () => TimeSpan.FromMinutes(15))
            {
            }

            internal override bool SecondFactorIsRequired(HttpContextBase context, string userName)
            {
                return true;
            }

            internal override bool StartSecondFactorAuth(HttpContextBase context, string userName)
            {
                Interlocked.Increment(ref _startSecondFactorCount);
                var current = Interlocked.Increment(ref _currentSecondFactors);
                UpdateMaximum(ref _maximumConcurrentSecondFactors, current);
                try
                {
                    var rendezvous = SecondFactorRendezvous;
                    if (rendezvous != null)
                    {
                        rendezvous.Signal();
                        if (!rendezvous.Wait(TimeSpan.FromSeconds(10)))
                        {
                            throw new TimeoutException(
                                "Second factors of different devices did not overlap, they were serialized");
                        }
                    }
                    else if (SecondFactorDelayMilliseconds > 0)
                    {
                        Thread.Sleep(SecondFactorDelayMilliseconds);
                    }

                    return SecondFactorResult;
                }
                finally
                {
                    Interlocked.Decrement(ref _currentSecondFactors);
                }
            }

            internal override MultiFactorAccessRequest SendAccessRequest(
                string identity,
                string email,
                string phone)
            {
                if (AccessRequestException != null)
                {
                    throw AccessRequestException;
                }

                var status = SecondFactorResult ? AccessRequestStatus.Granted : AccessRequestStatus.Denied;
                return new MultiFactorAccessRequest { Status = status };
            }
        }
    }
}
