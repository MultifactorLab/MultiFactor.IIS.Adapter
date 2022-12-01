using MultiFactor.IIS.Adapter.Services;
using System;
using System.Web;

namespace MultiFactor.IIS.Adapter.MsDynamics365
{
    public class Module : IHttpModule
    {
        private readonly object _sync = new object();

        private MultiFactorApiClient _multiFactorApiClient = new MultiFactorApiClient();
        private TokenValidationService _tokenValidationService = new TokenValidationService();

        public void Dispose()
        {
        }

        public void Init(HttpApplication context)
        {
            if (Configuration.Current == null)
            {
                //load configuration from web.config
                lock (_sync)
                {
                    Configuration.Load();
                }
            }

            context.BeginRequest += Context_BeginRequest;
            context.PostAuthorizeRequest += Context_PostAuthorizeRequest;
        }

        private void Context_BeginRequest(object sender, EventArgs e)
        {
            var context = HttpContext.Current;
            var token = context.Request.Form["AccessToken"];

            if (token != null)
            {
                //mfa response
                var cookie = new HttpCookie(Constants.COOKIE_NAME, token)
                {
                    HttpOnly = true,
                    Secure = true
                };

                context.Response.Cookies.Add(cookie);
                context.Response.Redirect(GetWebAppRoot(), true);

                return;
            }
        }

        private void Context_PostAuthorizeRequest(object sender, EventArgs e)
        {
            var context = HttpContext.Current;
            var path = context.Request.Url.GetComponents(UriComponents.Path, UriFormat.Unescaped).ToLower();

            //static resources
            if (WebUtil.IsStaticResourceRequest(context.Request.Url))
            {
                return;
            }

            if (path.Contains("errorhandler.aspx"))
            {
                return; //show any error
            }

            if (!context.User.Identity.IsAuthenticated)
            {
                //not yet authenticated with login/pwd
                return;
            }
            var user = context.User.Identity.Name;

            var canonicalUserName = Util.CanonicalizeUserName(user);

            //process request or postback to/from MultiFactor
            if (path.Contains(Constants.MULTIFACTOR_PAGE))
            {
                if (context.Request.HttpMethod == "POST")
                {
                    ProcessMultifactorRequest(context);
                }

                return;
            }

            if (!string.IsNullOrEmpty(Configuration.Current.ActiveDirectory2FaGroup))
            {
                //check 2fa group membership
                var activeDirectory = new ActiveDirectoryService(new CacheService(context));
                var isMember = activeDirectory.ValidateMembership(canonicalUserName);

                if (!isMember)
                {
                    //bypass 2fa
                    return;
                }
            }

            //mfa
            var isAuthenticatedByMultifactor = false;

            //check MultiFactor cookie
            var multifactorCookie = context.Request.Cookies[Constants.COOKIE_NAME];
            if (multifactorCookie != null)
            {
                var isValidToken = _tokenValidationService.TryVerifyToken(multifactorCookie.Value, out string userName);
                if (isValidToken)
                {
                    var isValidUser = Util.CanonicalizeUserName(userName) == Util.CanonicalizeUserName(user);
                    isAuthenticatedByMultifactor = isValidUser;
                }
            }

            if (!isAuthenticatedByMultifactor)
            {
                if (WebUtil.IsXhrRequest(context.Request)) //ajax request
                {
                    //tell app to refresh authentication
                    context.Response.StatusCode = 440;
                    context.Response.End();
                }
                else
                {
                    //redirect to mfa
                    var redirectUrl = GetWebAppRoot() +Constants.MULTIFACTOR_PAGE;
                    context.Response.Redirect(redirectUrl);
                }
            }
        }

        private void ProcessMultifactorRequest(HttpContext context)
        {
            //check if user session timed-out
            if (!context.User.Identity.IsAuthenticated)
            {
                context.Response.Redirect(context.Request.ApplicationPath);
                return;
            }

            var url = context.Request.Form["url"];
            if (url != null)
            {
                //mfa request

                var user = context.User.Identity.Name;
                var identity = user;

                if (Configuration.Current.UseUpnAsIdentity)     //must find upn
                {
                    if (!identity.Contains("@"))    //already upn
                    {
                        var activeDirectory = new ActiveDirectoryService(new CacheService(context));
                        identity = activeDirectory.SearchUserPrincipalName(Util.CanonicalizeUserName(identity));
                    }
                }

                var multiFactorAccessUrl = _multiFactorApiClient.CreateRequest(identity, user, url);
                context.Response.Redirect(multiFactorAccessUrl, true);
            }
        }

        private string GetWebAppRoot()
        {
            var context = HttpContext.Current;

            var host = (context.Request.Url.IsDefaultPort) ?
                context.Request.Url.Host :
                context.Request.Url.Authority;

            host = string.Format("{0}://{1}", context.Request.Url.Scheme, host);

            if (context.Request.ApplicationPath != "/")
            {
                host = host + HttpContext.Current.Request.ApplicationPath;
            }

            if (!host.EndsWith("/"))
            {
                host = host + "/";
            }

            return host;
        }
    }
}