using MultiFactor.IIS.Adapter.Services;
using System;
using System.Web;

namespace MultiFactor.IIS.Adapter.Extensions
{
    internal static class GetCacheAdapterExtension
    {
        public static CacheAdapter GetCacheAdapter(this HttpContextBase httpContext)
        {
            if (httpContext is null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            return new CacheAdapter(httpContext);
        }
    }
}