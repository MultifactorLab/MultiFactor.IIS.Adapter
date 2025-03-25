using System;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Web;
using MultiFactor.IIS.Adapter.Extensions;
using MultiFactor.IIS.Adapter.Owa;
using MultiFactor.IIS.Adapter.Services;
using System.Runtime.Caching;
using MultiFactor.IIS.Adapter.Core;

namespace MultiFactor.IIS.Adapter.ActiveSync
{
    public class Module : IHttpModule
    {
        private MemoryCache _memoryCache;
        private readonly string _applicationName = "/microsoft-server-activesync";
        private HttpApplication _httpApplication;
        public void Init(HttpApplication context)
        {
            context.PostAuthorizeRequest += HandlePostAuthorizeRequest;
                
            _memoryCache = MemoryCache.Default;
            _httpApplication = context;
        }

        private void HandlePostAuthorizeRequest(object sender, EventArgs e)
        {
            OnPostAuthorizeRequest(new HttpContextWrapper(((HttpApplication)sender).Context));
        }

        private void OnPostAuthorizeRequest(HttpContextBase context)
        {
            var path = context.Request.Url?.GetComponents(UriComponents.Path, UriFormat.Unescaped);

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            //static resources
            if (WebUtil.IsStaticResourceRequest(context.Request.Url))
            {
                return;
            }

            if (!context.User.Identity.IsAuthenticated)
                //not yet authenticated with login/pwd
            {
                return;
            }

            var user = context.User.Identity.Name;
            if (user.StartsWith("S-1-5-21")) //SID
            {
                user = TryGetUpnFromSid(context.User.Identity);
            }

            var canonicalUserName = Util.CanonicalizeUserName(user);
            if (Constants.EXCHANGE_SYSTEM_MAILBOX_PREFIX.Any(sm => canonicalUserName.StartsWith(sm)))
                //system mailbox
            {
                return;
            }

            var ad = new ActiveDirectoryService(context.GetCacheAdapter(), Logger.ActiveSync);
            var secondFactorRequired = new UserRequiredSecondFactor(ad);
            if (!secondFactorRequired.Execute(canonicalUserName))
                //bypass 2fa
            {
                return;
            }

            if (WebUtil.IsXhrRequest(context.Request))
            {
                AccessDenied(context);
                return;
            }

            if (WebUtil.IsInitialProvisionRequest(context.Request))
            {
                var cacheKey = BuildCacheKey(context);
                var cachedValue = _memoryCache.Get(cacheKey);
                var val = cachedValue as int?;

                if (val == 0)
                {
                    AccessDenied(context);
                    return;
                }

                if (val == 1 || context.HasApiUnreachableFlag())
                {
                    return;
                }

                //call to mfa
                var secondFactorIsSuccessed = StartSecondFactorAuth(context);
                if (secondFactorIsSuccessed)
                {
                    _memoryCache.Set(cacheKey, 1, DateTimeOffset.Now.AddSeconds(45));
                    return;
                }

                _memoryCache.Set(cacheKey, 0, DateTimeOffset.Now.AddSeconds(45));
                AccessDenied(context);
            }
        }

        private bool StartSecondFactorAuth(HttpContextBase context)
        {
            var ad = new ActiveDirectoryService(context.GetCacheAdapter(), Logger.ActiveSync);

            var userName = context.User.Identity.Name;

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

        public static string TryGetUpnFromSid(IIdentity identity)
        {
            //for download domains exchange uses OAuthIdentity with SID name
            //lets try find UPN with reflection
            var actAsUser = GetPropValue(identity, "ActAsUser");
            var upn = GetPropValue(actAsUser, "UserPrincipalName");
            return upn as string;
        }

        public static object GetPropValue(object src, string propName)
        {
            try
            {
                if (src == null)
                {
                    return null;
                }

                return src.GetType().GetProperty(propName).GetValue(src, null);
            }
            catch
            {
                return null;
            }
        }

        private void AccessDenied(HttpContextBase context)
        {
            context.Response.StatusCode = 440;
            context.Response.End();
        }

        private string BuildCacheKey(HttpContextBase context)
        {
            var userName = context.User.Identity.Name;
            var deviceId = context.Request.Params["DeviceId"];
            return $"{userName}-{deviceId}";
        }

        public void Dispose()
        {
            _httpApplication.PostAuthorizeRequest -= HandlePostAuthorizeRequest;
        }
    }
}
