﻿using MultiFactor.IIS.Adapter.Services;
using System;
using System.Web;

namespace MultiFactor.IIS.Adapter.Extensions
{
    internal static class HttpContextBaseExtensions
    {
        public static CacheAdapter GetCacheAdapter(this HttpContextBase httpContext)
        {
            if (httpContext is null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            return new CacheAdapter(httpContext);
        }

        public static bool HasApiUnreachableFlag(this HttpContextBase httpContext, bool trimName = true)
        {
            var name = httpContext?.User?.Identity?.Name;
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            // for owa we dont want trim username, because owa send netbiosname
            // but for compatibility with Ms365 we use flag
            if (trimName)
                return httpContext.GetCacheAdapter().GetApiUnreachable(Util.CanonicalizeUserName(name));
            else
                return httpContext.GetCacheAdapter().GetApiUnreachable(name);
        }
    }
}