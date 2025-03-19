using MultiFactor.IIS.Adapter.Extensions;
using MultiFactor.IIS.Adapter.Services;
using System;
using System.Linq;
using System.Runtime.Caching;
using System.Security.Principal;
using System.Web;

namespace MultiFactor.IIS.Adapter.Owa
{
    public class Module : IHttpModule
    {
        private MemoryCache _memoryCache;
        private HttpApplication _httpApplication;

        public void Init(HttpApplication context)
        {
            _httpApplication = context;
            _memoryCache = MemoryCache.Default;
            context.BeginRequest += OnBeginRequest;
            context.PostAuthorizeRequest += OnPostAuthorizeRequest;
        }

        public void OnBeginRequest(object sender, EventArgs e)
        {
            HttpContextBase context = new HttpContextWrapper(((HttpApplication)sender).Context);
            var path = context.Request.Url.GetComponents(UriComponents.Path, UriFormat.Unescaped);
            if (path.Contains("lang.owa"))
            {
                return;
            }

            var token = context.Request.Form["AccessToken"];
            if (token == null)
            {
                return;
            }

            var cookie = new HttpCookie(BuildCookieName(context), token) { Path = context.Request.ApplicationPath };
            context.AddCookie(cookie);
            context.Response.Redirect(context.Request.ApplicationPath ?? "/");
        }

        public void OnPostAuthorizeRequest(object sender, EventArgs e)
        {
            HttpContextBase context = new HttpContextWrapper(((HttpApplication)sender).Context);
            var path = context.Request.Url.GetComponents(UriComponents.Path, UriFormat.Unescaped);

            //static resources
            if (WebUtil.IsStaticResourceRequest(context.Request.Url))
            {
                return;
            }

            //logoff page
            if (path.ToLower().Contains("logoff"))
            {
                var cookieName = BuildCookieName(context);
                var cookie = context.Request.Cookies[cookieName];
                if (cookie != null)
                {
                    context.RemoveCookie(cookieName);
                    _memoryCache.Remove(cookie?.Value);
                }

                context.Response.Redirect(context.Request.ApplicationPath);
                return;
            }

            //auth page
            if (path.Contains("/auth"))
            {
                return;
            }

            //language selection page
            if (path.Contains("languageselection.aspx") || path.Contains("lang.owa"))
            {
                return;
            }

            if (context.User?.Identity?.IsAuthenticated != true)
            {
                //not yet authenticated with login/pwd
                return;
            }

            var user = context.User.Identity.Name;
            if (user.StartsWith("S-1-5-21")) //SID
            {
                user = TryGetUpnFromSid(context.User.Identity);
            }

            var canonicalUserName = Util.CanonicalizeUserName(user);
            if (Constants.EXCHANGE_SYSTEM_MAILBOX_PREFIX.Any(sm => canonicalUserName.StartsWith(sm)))
            {
                //system mailbox
                return;
            }

            //process request or postback to/from MultiFactor
            if (path.Contains(Constants.MULTIFACTOR_PAGE))
            {
                if (context.Request.HttpMethod == "POST")
                {
                    string identityKey = context.Request.Cookies["identity"]?.Value;
                    
                    if (string.IsNullOrWhiteSpace(identityKey))
                    {
                        AccessDenied(context);
                    }
                    
                    var identity = _memoryCache.Get(identityKey)?.ToString();
                    
                    if (string.IsNullOrWhiteSpace(identity))
                    {
                        AccessDenied(context);
                    }

                    context.RemoveCookie("identity");
                    _memoryCache.Remove(identityKey);

                    ProcessMultifactorRequest(context, identity);
                }

                return;
            }

            var ad = new ActiveDirectoryService(context.GetCacheAdapter(), Logger.Owa);
            var secondFactorRequired = new UserRequiredSecondFactor(ad);
            if (!secondFactorRequired.Execute(canonicalUserName))
            {
                //bypass 2fa
                return;
            }

            //mfa
            var valSrv = new TokenValidationService(Logger.Owa);
            var checker = new AuthChecker(context, valSrv);
            var isAuthenticatedByMultifactor = checker.IsAuthenticated(user, BuildCookieName(context));
            if (isAuthenticatedByMultifactor || context.HasApiUnreachableFlag())
            {
                return;
            }

            if (WebUtil.IsXhrRequest(context.Request))
            {
                //ajax request
                //tell owa to refresh authentication
                context.Response.StatusCode = 440;
                context.Response.End();
                return;
            }

            //redirect to mfa
            if (canonicalUserName != "system")
            {
                var redirectUrl = $"{context.Request.ApplicationPath}/{Constants.MULTIFACTOR_PAGE}";
                var cookieName = "identity";
                var cacheKey = Guid.NewGuid().ToString();
                _memoryCache.Set(cacheKey, canonicalUserName, DateTime.Now.AddMinutes(6));
                context.AddCookie(new HttpCookie(cookieName, cacheKey) { Expires = DateTime.Now.AddMinutes(5) });
                context.Response.Redirect(redirectUrl);
            }
        }

        private void ProcessMultifactorRequest(HttpContextBase context, string forceIdentity = null)
        {
            //check if user session timed-out
            if (!context.User.Identity.IsAuthenticated)
            {
                context.Response.Redirect(context.Request.ApplicationPath);
                return;
            }

            var url = context.Request.Form["url"];
            if (url == null)
            {
                return;
            }

            var executor = MfaApiRequestExecutorFactory.CreateOwa(context, forceIdentity);
            executor.Execute(url, context.Request.ApplicationPath);
        }

        public string TryGetUpnFromSid(IIdentity identity)
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
        
        private string BuildCookieName(HttpContextBase context)
        {
            return Constants.COOKIE_NAME + context.Request.ApplicationPath;
        }

        public void Dispose()
        {
            _httpApplication.BeginRequest -= OnBeginRequest;
            _httpApplication.PostAuthorizeRequest -= OnPostAuthorizeRequest;
        }
    }
}
