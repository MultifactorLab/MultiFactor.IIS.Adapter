using System;
using System.Web;

namespace MultiFactor.IIS.Adapter.Services
{
    public static class PublicUrlResolver
    {
        //set by the exchange front end site when it proxies the request to the back end,
        //holds the address the user opened in the browser
        private const string EXCHANGE_PROXY_URI_HEADER = "msExchProxyUri";

        public static string ResolveOwa(HttpContextBase context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var configured = Configuration.Current.PublicUrl;
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured;
            }

            var proxied = GetProxiedUrl(context);
            if (proxied != null)
            {
                return proxied;
            }

            var message =
                $"Configuration error: '{ConfigurationKeys.PublicUrl}' element not found or empty " +
                $"and the public address cannot be taken from the '{EXCHANGE_PROXY_URI_HEADER}' header of the request";
            Logger.Owa.Error(message);
            throw new Exception(message);
        }

        public static string ResolveCrm(HttpContextBase context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var configured = Configuration.Current.PublicUrl;
            return !string.IsNullOrWhiteSpace(configured) ? configured : BuildFromRequest(context);
        }

        private static string GetProxiedUrl(HttpContextBase context)
        {
            var header = context.Request.Headers[EXCHANGE_PROXY_URI_HEADER];
            if (string.IsNullOrWhiteSpace(header) ||
                !Uri.TryCreate(header.Trim(), UriKind.Absolute, out var proxyUri))
            {
                return null;
            }

            //only the origin is taken from the header, the path of the requested page is dropped
            var applicationPath = context.Request.ApplicationPath;
            if (string.IsNullOrEmpty(applicationPath) || applicationPath == "/")
            {
                applicationPath = string.Empty;
            }

            return Normalize($"{proxyUri.Scheme}://{proxyUri.Authority}{applicationPath}");
        }

        private static string BuildFromRequest(HttpContextBase context)
        {
            var url = context.Request.Url;
            var host = url.IsDefaultPort ? url.Host : url.Authority;
            var root = $"{url.Scheme}://{host}";

            var applicationPath = context.Request.ApplicationPath;
            if (!string.IsNullOrEmpty(applicationPath) && applicationPath != "/")
            {
                root = $"{root}{applicationPath}";
            }

            return root.EndsWith("/") ? root : $"{root}/";
        }

        internal static string Normalize(string value)
        {
            if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrEmpty(uri.UserInfo) ||
                !string.IsNullOrEmpty(uri.Query) ||
                !string.IsNullOrEmpty(uri.Fragment))
            {
                return null;
            }

            var builder = new UriBuilder(uri);
            if (!builder.Path.EndsWith("/", StringComparison.Ordinal))
            {
                builder.Path += "/";
            }
            
            return builder.Uri.AbsoluteUri;
        }
    }
}
