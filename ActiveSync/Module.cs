using System;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Threading.Tasks;
using System.Web;
using MultiFactor.IIS.Adapter.ActiveSync.AsyncLocker;
using MultiFactor.IIS.Adapter.Core;
using MultiFactor.IIS.Adapter.Extensions;
using MultiFactor.IIS.Adapter.Owa;
using MultiFactor.IIS.Adapter.Services;

namespace MultiFactor.IIS.Adapter.ActiveSync
{
    public class Module : IHttpModule
    {
        private MemoryCache _memoryCache;
        private static readonly AsyncLocker<string> _locker = new AsyncLocker<string>();
        
        public void Init(HttpApplication context)
        {
            EventHandlerTaskAsyncHelper handler = new EventHandlerTaskAsyncHelper(HandleRequest);
            context.AddOnBeginRequestAsync(handler.BeginEventHandler, handler.EndEventHandler);
            _memoryCache = MemoryCache.Default;
        }
        
        private async Task HandleRequest(object sender, EventArgs e)
        {
            await OnProvision(new HttpContextWrapper(((HttpApplication)sender).Context));
        }

        private async Task OnProvision(HttpContextBase context)
        {
            if (ShouldSkipRequest(context))
            {
                return;
            }
            
            var canonicalUserName = GetCanonicalizedUserName(context);
            using (await _locker.LockAsync(canonicalUserName))
            {
                if (!SecondFactorIsRequired(context, canonicalUserName))
                {
                    //bypass 2fa
                    Logger.ActiveSync.Info($"Bypass for user '{canonicalUserName}'.");
                    return;
                }
                var cacheKey = BuildCacheKey(canonicalUserName,context);
                
                var val = GetCacheValue(cacheKey);
                if (val == 0)
                {
                    AccessDenied(context);
                    return;
                }
                
                if (val == 1 || context.HasApiUnreachableFlag())
                {
                    return;
                }
                
                Logger.ActiveSync.Info("2fa begin for user " + canonicalUserName);
                var isSecondFactorSuccessful = StartSecondFactorAuth(context, canonicalUserName);
                if (isSecondFactorSuccessful)
                {
                    _memoryCache.Set(cacheKey, 1, DateTimeOffset.Now.AddSeconds(45));
                    Logger.ActiveSync.Info("2fa is successful for user" + canonicalUserName);
                    return;
                }

                _memoryCache.Set(cacheKey, 0, DateTimeOffset.Now.AddSeconds(60));
                AccessDenied(context);
            }
        }
        
        private bool ShouldSkipRequest(HttpContextBase context)
        {
            var method = context.Request.HttpMethod.ToLowerInvariant();
            var userName = context.Request.Params["User"] ?? string.Empty;
            var isPost = method == "post";
            var isSystemMailbox = Constants.EXCHANGE_SYSTEM_MAILBOX_PREFIX.Any(sm => userName.ToLowerInvariant().Contains(sm));
            var isProvision = WebUtil.IsInitialProvisionRequest(context.Request);
            return !isPost || !isProvision || isSystemMailbox;
        }
        
        private string GetCanonicalizedUserName(HttpContextBase context)
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

            string lastChunk = chunks == null || chunks.Length <= 1 ? null : chunks.LastOrDefault();
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
        
        private bool SecondFactorIsRequired(HttpContextBase context, string userName)
        {
            var ad = new ActiveDirectoryService(context.GetCacheAdapter(), Logger.ActiveSync);
            var secondFactorRequired = new UserRequiredSecondFactor(ad);
            var secondFactorUsername = Util.CanonicalizeUserName(userName);
            return secondFactorRequired.Execute(secondFactorUsername);
        }

        private int? GetCacheValue(string key)
        {
            var cachedValue = _memoryCache.Get(key);
            return cachedValue as int?;
        }

        private bool StartSecondFactorAuth(HttpContextBase context, string userName)
        {
            var ad = new ActiveDirectoryService(context.GetCacheAdapter(), Logger.ActiveSync);

            var identity = Util.CanonicalizeUserName(userName);
            Logger.ActiveSync.Info($"Applying identity canonicalization: {userName}->{identity}");

            var profile = ad.GetProfile(identity);

            if (profile == null)
            {
                Logger.ActiveSync.Error($"No profile found for identity: {identity}");
                // redirect to (custom?) error page
                throw new Exception($"Profile {identity} not found");
            }

            if (Configuration.Current.HasTwoFaIdentityAttribute && !string.IsNullOrEmpty(profile.TwoFAIdentity))
            {
                Logger.ActiveSync.Info($"Applying 2fa identity attribute: {identity}->{profile.TwoFAIdentity}");
                identity = profile.TwoFAIdentity;
            }

            var response = CreateAccessRequest(context, identity, profile?.Phone, profile?.Email);

            return response.Granted;
        }

        private MultiFactorAccessRequest CreateAccessRequest(HttpContextBase context, string identity, string phone, string email)
        {
            var api = new MultiFactorApiClient(Logger.ActiveSync, MfTraceIdFactory.CreateTraceActiveSync);

            try
            {
                var response = api.CreateNonInteractiveAccessRequest("/access/requests/ex", new PersonalData(identity, email, phone));
                return response;
            }
            catch (WebException wex) when (Configuration.Current.BypassSecondFactorWhenApiUnreachable)
            {
                var errmsg = $"Multifactor API host unreachable: {Configuration.Current.ApiUrl}. Reason: {wex}";
                Logger.ActiveSync.Error(errmsg);

                if (wex.Response != null)
                {
                    var httpStatusCode = ((HttpWebResponse)wex.Response).StatusCode;
                    if ((int)httpStatusCode == 429)
                    {
                        Logger.ActiveSync.Error($"Too many requests. Please try again later.");
                    }

                    return new MultiFactorAccessRequest { Status = "Denied" };
                }

                Logger.ActiveSync.Warn($"Bypassing the second factor for user '{identity}'.");
                context
                    .GetCacheAdapter()
                    .SetApiUnreachable(Util.CanonicalizeUserName(identity), true);
                return new MultiFactorAccessRequest { Status = "Granted" };
            }
            catch (Exception ex)
            {
                Logger.ActiveSync.Error(ex.ToString());
            }

            return new MultiFactorAccessRequest { Status = "Denied" };
        }

        private void AccessDenied(HttpContextBase context)
        {
            context.Response.StatusCode = 440;
            context.Response.End();
        }

        private string BuildCacheKey(string userName, HttpContextBase context)
        {
            var deviceId = context.Request.Params["DeviceId"];
            return $"{userName}-{deviceId}";
        }

        public void Dispose()
        {
        }
    }
}
