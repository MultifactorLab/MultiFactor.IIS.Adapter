﻿using MultiFactor.IIS.Adapter.Core;
using MultiFactor.IIS.Adapter.Extensions;
using MultiFactor.IIS.Adapter.Properties;
using MultiFactor.IIS.Adapter.Services;
using System;
using System.Linq;
using System.Security.Principal;
using System.Web;

namespace MultiFactor.IIS.Adapter.Owa
{
    public class Module : HttpModuleBase
    {
        public override void OnBeginRequest(HttpContextBase context)
        {
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

            var complete2FaHtml = Resources.complete_2fa_html.Replace("%MULTIFACTOR_COOKIE%", token);
            SendPage(context, complete2FaHtml);
        }

        public override void OnPostAuthorizeRequest(HttpContextBase context)
        {
            var path = context.Request.Url.GetComponents(UriComponents.Path, UriFormat.Unescaped);

            //static resources
            if (WebUtil.IsStaticResourceRequest(context.Request.Url))
            {
                return;
            }

            //logoff page
            if (path.Contains("logoff.owa"))
            {
                SendPage(context, Resources.complete_logout_html);
                return;
            }

            //auth page
            if (path.Contains("/auth"))
            {
                return;
            }

            //language selection page
            if (path.Contains("languageselection.aspx") || path.Contains("lang.owa")) return;

            if (!context.User.Identity.IsAuthenticated)
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
                    ProcessMultifactorRequest(context);
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
            var isAuthenticatedByMultifactor = checker.IsAuthenticated(user);
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
            var redirectUrl = $"{context.Request.ApplicationPath}/{Constants.MULTIFACTOR_PAGE}";
            context.Response.Redirect(redirectUrl);
        }

        private void SendPage(HttpContextBase context, string html)
        {
            context.Response.Clear();
            context.Response.ClearContent();
            context.Response.Write(html);
            context.Response.Flush();
            context.Response.End();
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

            //mfa request
            if (url.IndexOf("#") == -1)
            {
                url += "#path=/mail";
            }

            var executor = MfaApiRequestExecutorFactory.CreateOwa(context);
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
    }
}