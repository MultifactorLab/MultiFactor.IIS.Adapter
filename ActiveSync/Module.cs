using System;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Threading.Tasks;
using System.Web;
using MultiFactor.IIS.Adapter.ActiveSync.AsyncLocker;
using MultiFactor.IIS.Adapter.Extensions;
using MultiFactor.IIS.Adapter.Owa;
using MultiFactor.IIS.Adapter.Services;
using MultiFactor.IIS.Adapter.Services.Ldap;

namespace MultiFactor.IIS.Adapter.ActiveSync
{
    public class Module : IHttpModule
    {
        private const int Denied = 0;
        private const int Granted = 1;

        private static readonly AsyncLocker<string> _locker = new AsyncLocker<string>();

        private readonly ObjectCache _memoryCache;
        private readonly Func<TimeSpan> _grantedSessionLifetime;
        private readonly Func<TimeSpan> _deniedSessionLifetime;
        private readonly Func<bool> _bypassWhenApiUnreachable;
        private readonly Func<string> _getApiUrl;
        private readonly Func<TimeSpan> _apiLifeCheckInterval;

        public Module()
            : this(
                MemoryCache.Default,
                () => TimeSpan.FromHours(Configuration.Current.SessionLifeTimeInHours),
                () => TimeSpan.FromMinutes(Configuration.Current.ReRequestDelayInMinutes),
                () => Configuration.Current.BypassSecondFactorWhenApiUnreachable,
                () => Configuration.Current.ApiUrl,
                () => Configuration.Current.ApiLifeCheckInterval)
        {
        }

        internal Module(
            ObjectCache memoryCache,
            Func<TimeSpan> grantedSessionLifetime,
            Func<TimeSpan> deniedSessionLifetime,
            Func<bool> bypassWhenApiUnreachable,
            Func<string> getApiUrl,
            Func<TimeSpan> apiLifeCheckInterval)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _grantedSessionLifetime = grantedSessionLifetime ?? throw new ArgumentNullException(nameof(grantedSessionLifetime));
            _deniedSessionLifetime = deniedSessionLifetime ?? throw new ArgumentNullException(nameof(deniedSessionLifetime));
            _bypassWhenApiUnreachable = bypassWhenApiUnreachable ?? throw new ArgumentNullException(nameof(bypassWhenApiUnreachable));
            _getApiUrl = getApiUrl ?? throw new ArgumentNullException(nameof(getApiUrl));
            _apiLifeCheckInterval = apiLifeCheckInterval ?? throw new ArgumentNullException(nameof(apiLifeCheckInterval));
        }

        public void Init(HttpApplication context)
        {
            EventHandlerTaskAsyncHelper handler = new EventHandlerTaskAsyncHelper(HandleRequest);
            context.AddOnBeginRequestAsync(handler.BeginEventHandler, handler.EndEventHandler);
        }

        private async Task HandleRequest(object sender, EventArgs e)
        {
            await OnProvision(new HttpContextWrapper(((HttpApplication)sender).Context));
        }

        internal async Task OnProvision(HttpContextBase context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (ShouldSkipRequest(context))
            {
                return;
            }

            Logger.ActiveSync.Info(RequestLoggerBuilder.BuildRequestLog(context));

            var canonicalUserName = GetCanonicalizedUserName(context);
            if (string.IsNullOrWhiteSpace(canonicalUserName))
            {
                Logger.ActiveSync.Warn("Unable to determine the ActiveSync user name.");
                AccessDenied(context);
                return;
            }

            var cacheKey = BuildCacheKey(canonicalUserName, context);
            using (await _locker.LockAsync(cacheKey))
            {
                if (!SecondFactorIsRequired(context, canonicalUserName))
                {
                    Logger.ActiveSync.Info($"Bypass for user '{canonicalUserName}'.");
                    return;
                }

                var val = GetCacheValue(cacheKey);
                if (val == Denied)
                {
                    AccessDenied(context);
                    return;
                }

                if (val == Granted || context.HasApiUnreachableFlag(canonicalUserName))
                {
                    return;
                }

                Logger.ActiveSync.Info("2fa begin for user " + canonicalUserName);
                var isSecondFactorSuccessful = StartSecondFactorAuth(context, canonicalUserName);
                if (isSecondFactorSuccessful)
                {
                    _memoryCache.Set(cacheKey, Granted, DateTimeOffset.Now.Add(_grantedSessionLifetime()));
                    Logger.ActiveSync.Info("2fa is successful for user " + canonicalUserName);
                    return;
                }

                if (context.HasApiUnreachableFlag(canonicalUserName))
                {
                    return;
                }

                _memoryCache.Set(cacheKey, Denied, DateTimeOffset.Now.Add(_deniedSessionLifetime()));
                AccessDenied(context);
            }
        }

        internal static bool ShouldSkipRequest(HttpContextBase context)
        {
            var method = context.Request.HttpMethod.ToLowerInvariant();
            var rawUserName = context.Request.Params["User"] ?? string.Empty;
            var userName = rawUserName.Length == 0 ? string.Empty : Util.CanonicalizeUserName(rawUserName);
            var isPost = method == "post";
            var isSystemMailbox = Constants.EXCHANGE_SYSTEM_MAILBOX_PREFIX.Any(sm => userName.StartsWith(sm, StringComparison.Ordinal));
            var isProvision = WebUtil.IsInitialProvisionRequest(context.Request);
            return !isPost || !isProvision || isSystemMailbox;
        }

        internal static string GetCanonicalizedUserName(HttpContextBase context)
        {
            string rawUserName = context.Request.Params["User"];
            string userName = string.IsNullOrWhiteSpace(rawUserName) ? null : Util.CanonicalizeUserName(rawUserName);
            string userDomain = string.IsNullOrWhiteSpace(rawUserName) ? null : Util.GetUserDomain(rawUserName);
            string httpXEasProxyParam = context.Request.Params["HTTP_X_EAS_PROXY"];
            string[] chunks;
            if (!string.IsNullOrWhiteSpace(httpXEasProxyParam))
            {
                chunks = httpXEasProxyParam.Split(',');
            }
            else
            {
                chunks = null;
            }

            string lastChunk = chunks == null || chunks.Length <= 1 ? null : chunks.LastOrDefault()?.Trim();
            string userNameFromProxy = string.IsNullOrWhiteSpace(lastChunk) ? null : Util.CanonicalizeUserName(lastChunk);
            string userDomainFromProxy = string.IsNullOrWhiteSpace(lastChunk) ? null : Util.GetUserDomain(lastChunk);
            if (string.IsNullOrWhiteSpace(userDomain))
            {
                if (string.IsNullOrWhiteSpace(userDomainFromProxy))
                {
                    return userName;
                }

                return userDomainFromProxy + "\\" + userNameFromProxy;
            }

            return userDomain + "\\" + userName;
        }

        internal virtual bool SecondFactorIsRequired(HttpContextBase context, string userName)
        {
            var ad = new ActiveDirectoryService(context.GetCacheAdapter(), Logger.ActiveSync);
            var secondFactorRequired = new UserRequiredSecondFactor(ad, Logger.ActiveSync);
            return secondFactorRequired.Execute(LdapIdentity.Parse(userName));
        }

        private int? GetCacheValue(string key)
        {
            var cachedValue = _memoryCache.Get(key);
            return cachedValue as int?;
        }

        internal virtual bool StartSecondFactorAuth(HttpContextBase context, string userName)
        {
            var ad = new ActiveDirectoryService(context.GetCacheAdapter(), Logger.ActiveSync);

            var ldapIdentity = LdapIdentity.Parse(userName);
            var identity = ldapIdentity.Name;
            Logger.ActiveSync.Info($"Applying identity canonicalization: {userName}->{identity}");

            var profile = ad.GetProfile(ldapIdentity);

            if (profile == null)
            {
                Logger.ActiveSync.Error($"No profile found for identity: {identity}");
                // redirect to (custom?) error page
                throw new Exception($"Profile {identity} not found");
            }

            if (Configuration.Current.HasTwoFaIdentityAttribute && !string.IsNullOrEmpty(profile.Custom2FAIdentity))
            {
                Logger.ActiveSync.Info($"Applying 2fa identity attribute: {identity}->{profile.Custom2FAIdentity}");
                identity = profile.Custom2FAIdentity;
            }

            var personalData = new PersonalData(profile.Name, profile.Email, profile.Phone, Configuration.Current.PrivacyMode);
            var response = CreateAccessRequest(context, identity, personalData.Phone, personalData.Email, userName);

            return response.Status == AccessRequestStatus.Granted;
        }

        internal MultiFactorAccessRequest CreateAccessRequest(
            HttpContextBase context,
            string identity,
            string phone,
            string email,
            string cacheIdentity)
        {
            try
            {
                return SendAccessRequest(identity, email, phone);
            }
            catch (WebException wex) when (_bypassWhenApiUnreachable())
            {
                var errmsg = $"Multifactor API host unreachable: {_getApiUrl()}. Reason: {wex}";
                Logger.ActiveSync.Error(errmsg);

                if (wex.Response != null)
                {
                    var httpStatusCode = ((HttpWebResponse)wex.Response).StatusCode;
                    if ((int)httpStatusCode == 429)
                    {
                        Logger.ActiveSync.Error($"Too many requests. Please try again later.");
                    }

                    return new MultiFactorAccessRequest { Status = AccessRequestStatus.Denied };
                }

                Logger.ActiveSync.Warn($"Bypassing the second factor for user '{identity}'.");
                context
                    .GetCacheAdapter()
                    .SetApiUnreachable(Util.CanonicalizeUserName(cacheIdentity), true, _apiLifeCheckInterval());
                return new MultiFactorAccessRequest { Status = AccessRequestStatus.Bypassed };
            }
            catch (Exception ex)
            {
                Logger.ActiveSync.Error(ex.ToString());
            }

            return new MultiFactorAccessRequest { Status = AccessRequestStatus.Denied };
        }

        internal virtual MultiFactorAccessRequest SendAccessRequest(string identity, string email, string phone)
        {
            var api = new MultiFactorApiClient(Logger.ActiveSync, MfTraceIdFactory.CreateTraceActiveSync);
            return api.CreateNonInteractiveAccessRequest("/access/requests/ex", identity, email, phone);
        }

        private static void AccessDenied(HttpContextBase context)
        {
            context.Response.StatusCode = 440;
            context.Response.End();
        }

        internal static string BuildCacheKey(string userName, HttpContextBase context)
        {
            var deviceId = context.Request.Params["DeviceId"]?.Trim();
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = "unknown-device";
            }

            return $"multifactor:eas:{userName.ToLowerInvariant()}:{deviceId.ToLowerInvariant()}";
        }

        public void Dispose()
        {
        }
    }
}