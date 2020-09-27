using MultiFactor.IIS.Adapter.Services;
using System;
using System.Linq;
using System.Web;

namespace MultiFactor.IIS.Adapter.Owa
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

            context.PostAuthorizeRequest += Context_PostAuthorizeRequest;
        }

        private void Context_PostAuthorizeRequest(object sender, EventArgs e)
        {
            var context = HttpContext.Current;
            var path = context.Request.Url.GetComponents(UriComponents.Path, UriFormat.Unescaped);

            //static resources
            if (WebUtil.IsStaticResourceRequest(context.Request.Url))
            {
                return;
            }

            //logoff page
            if (path.Contains("logoff.owa"))
            {
                //clean mfa cookie
                context.Response.Cookies[Constants.COOKIE_NAME].Expires = DateTime.UtcNow.AddDays(-1);
                return;
            }

            //auth page
            if (path.Contains("/auth"))
            {
                return;
            }

            //language selection page
            if (path.Contains("/languageselection.aspx") || path.Contains("lang.owa"))
            {
                return;
            }

            if (!context.User.Identity.IsAuthenticated)
            {
                //not yet authenticated with login/pwd
                return;
            }

            var user = context.User.Identity.Name;

            //system mailbox
            if (Constants.SYSTEM_MAILBOX_PREFIX.Any(sm => user.StartsWith(sm)))
            {
                return;
            }

            //process request or postback to/from MultiFactor
            if (path.Contains(Constants.MULTIFACTOR_PAGE))
            {
                if (context.Request.HttpMethod == "POST")
                {
                    ProcessMultifactorRequest(context);
                }
                return;
            }

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
                    //tell owa to refresh authentication
                    context.Response.StatusCode = 440;
                    context.Response.End();
                }
                else
                {
                    //redirect to mfa
                    context.Response.Redirect(context.Request.ApplicationPath + "/" + Constants.MULTIFACTOR_PAGE);
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

            var token = context.Request.Form["AccessToken"];
            if (token != null)
            {
                //mfa response
                var cookie = new HttpCookie(Constants.COOKIE_NAME, token);

                context.Response.Cookies.Add(cookie);
                context.Response.Redirect(context.Request.ApplicationPath);

                return;
            }

            var url = context.Request.Form["url"];
            if (url != null)
            {
                //mfa request
                var user = context.User.Identity.Name;
                var multiFactorAccessUrl = _multiFactorApiClient.CreateRequest(user, url);
                context.Response.Redirect(multiFactorAccessUrl, true);
            }
        }
    }
}