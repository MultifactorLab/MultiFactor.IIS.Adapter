using MultiFactor.IIS.Adapter.Core;
using MultiFactor.IIS.Adapter.Extensions;
using MultiFactor.IIS.Adapter.Owa;
using MultiFactor.IIS.Adapter.Services;
using MultiFactor.IIS.Adapter.Services.Ldap;
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
                context.Response.Redirect(PublicUrlResolver.ResolveCrm(context), true);

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
            var user = LdapIdentity.Parse(context.User.Identity.Name);


            var ad = new ActiveDirectoryService(context.GetCacheAdapter(), Logger.IIS);
            var secondFactorRequired = new UserRequiredSecondFactor(ad, Logger.IIS);
            if (!secondFactorRequired.Execute(user))
            {
                //bypass 2fa
                return;
            }

            //mfa
            var valSrv = new TokenValidationService(Logger.IIS);
            var checker = new AuthChecker(context, valSrv, Logger.IIS);
            var isAuthenticatedByMultifactor = checker.IsAuthenticated(user.RawName);
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
            
            //multifactor posts the access token back to this url and the user returns here after the second factor
            var callbackUrl = PublicUrlResolver.ResolveCrm(context);
            var executor = MfaApiRequestExecutorFactory.CreateCrm(context);
            executor.Execute(callbackUrl);
        }

        private static bool NeedToBypass(Exception ex)
        {
            return ex.Message?.StartsWith(Constants.API_UNREACHABLE_CODE) == true && Configuration.Current.BypassSecondFactorWhenApiUnreachable;
        }
    }
}