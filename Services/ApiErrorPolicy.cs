using System.Net;

namespace MultiFactor.IIS.Adapter.Services
{
    internal static class ApiErrorPolicy
    {
        public static bool ShouldDenyRequest(WebException exception, out string reason)
        {
            if (exception?.Status == WebExceptionStatus.Timeout)
            {
                reason = "timeout";
                return true;
            }

            switch ((exception?.Response as HttpWebResponse)?.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    reason = "HTTP 401 Unauthorized, check ApiKey/ApiSecret";
                    return true;
                case HttpStatusCode.Forbidden:
                    reason = "HTTP 403 Forbidden";
                    return true;
                case (HttpStatusCode)429:
                    reason = "HTTP 429 Too Many Requests";
                    return true;
                default:
                    reason = null;
                    return false;
            }
        }
    }
}
