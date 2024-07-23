using MultiFactor.IIS.Adapter.Core;
using MultiFactor.IIS.Adapter.Extensions;
using MultiFactor.IIS.Adapter.Owa;
using MultiFactor.IIS.Adapter.Services;
using System;
using System.Web;

namespace MultiFactor.IIS.Adapter.MsDynamics365
{
    public class Module : HttpModuleBase
    {
        public override void OnBeginRequest(HttpContextBase context)
        {
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

        public override void OnPostAuthorizeRequest(HttpContextBase context)
        {
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

            var ad = new ActiveDirectoryService(context.GetCacheAdapter(), Logger.IIS);
            var secondFactorRequired = new UserRequiredSecondFactor(ad);
            if (!secondFactorRequired.Execute(canonicalUserName))
            {
                //bypass 2fa
                return;
            }

            //mfa
            var valSrv = new TokenValidationService(Logger.IIS);
            var checker = new AuthChecker(context, valSrv);
            var isAuthenticatedByMultifactor = checker.IsAuthenticated(user);
            if (isAuthenticatedByMultifactor || context.HasApiUnreachableFlag())
            {
                return;
            }

            if (WebUtil.IsXhrRequest(context.Request)) //ajax request
            {
                //tell app to refresh authentication
                context.Response.StatusCode = 440;
                context.Response.End();
                return;
            }       
            
            //redirect to mfa
            var redirectUrl = $"{GetWebAppRoot()}{Constants.MULTIFACTOR_PAGE}";
            context.Response.Redirect(redirectUrl);       
        }

        private void ProcessMultifactorRequest(HttpContextBase context)
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

            var executor = MfaApiRequestExecutorFactory.CreateCrm(context);
            executor.Execute(url, GetWebAppRoot());
        }

        private static bool NeedToBypass(Exception ex)
        {
            return ex.Message?.StartsWith(Constants.API_UNREACHABLE_CODE) == true && Configuration.Current.BypassSecondFactorWhenApiUnreachable;
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
                host = $"{host}{HttpContext.Current.Request.ApplicationPath}";
            }

            if (!host.EndsWith("/"))
            {
                host = $"{host}/";
            }

            return host;
        }
    }
}