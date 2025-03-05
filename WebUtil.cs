using System;
using System.Linq;
using System.Web;

namespace MultiFactor.IIS.Adapter
{
    public static class WebUtil
    {
        public static bool IsXhrRequest(HttpRequestBase request)
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
        
        /// <summary>
        /// Determines whether the request is the first provisioning request
        /// https://learn.microsoft.com/en-us/previous-versions/office/developer/exchange-server-interoperability-guidance/hh531590(v=exchg.140)
        /// </summary> 
        public static bool IsInitialProvisionRequest(HttpRequestBase request)
        {
            var cmd = request.Params["Cmd"];
            var xMsPolicyKey = request.Headers["X-MS-PolicyKey"]?.Trim();
            if (cmd?.ToLower() == "provision" && (string.IsNullOrWhiteSpace(xMsPolicyKey) || xMsPolicyKey == "0"))
            {
                return true;
            }

            return false;
        }
    }
}
