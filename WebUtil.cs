using System;
using System.Linq;
using System.Web;

namespace MultiFactor.IIS.Adapter
{
    public static class WebUtil
    {
        public static bool IsXhrRequest(HttpRequest request)
        {
            return request.Headers["x-requested-with"] == "XMLHttpRequest";
        }

        public static bool IsStaticResourceRequest(Uri uri)
        {
            var staticContent = new[]
            {
                ".js",
                ".css",
                ".png",
                ".gif",
                ".ico",
            };

            var path = uri.LocalPath;
            return staticContent.Any(c => path.EndsWith(c));
        }
    }
}