using System.Web;

namespace MultiFactor.IIS.Adapter.Extensions
{
    internal static class HasApiUnreachableExtension
    {
        public static bool HasApiUnreachableFlag(this HttpContextBase httpContext)
        {
            var name = httpContext?.User?.Identity?.Name;
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            return httpContext.GetCacheAdapter().GetApiUnreachable(Util.CanonicalizeUserName(name));
        }
    }
}