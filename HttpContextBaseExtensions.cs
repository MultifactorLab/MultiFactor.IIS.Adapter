using System;
using System.Web;

namespace MultiFactor.IIS.Adapter
{
    public static class HttpContextBaseExtensions
    {
        public static void RemoveCookie(
            this HttpContextBase context,
            string cookieName)
        {
            HttpCookie cookie = context.Response.Cookies[cookieName];
            if (cookie == null)
            {
                return;
            }

            cookie.Expires = DateTime.UtcNow.AddDays(-1);
        }

        public static void AddCookie(this HttpContextBase context, HttpCookie cookie)
        {
            context.Response.Cookies.Set(cookie);
        }

        public static void RemoveAllCookie(this HttpContextBase context)
        {
            foreach (string allKey in context.Request.Cookies.AllKeys)
            {
                context.RemoveCookie(allKey);
            }
        }
    }
}
