using System;
using System.Web;
using System.Web.Caching;

namespace MultiFactor.IIS.Adapter.Services
{
    public class CacheService
    {
        const int GROUP_DN_CACHE_TIMEOUT = 60;
        const int USER_MEMBERSHIP_CACHE_TIMEOUT = 15;
        const string KEY_PREFIX = "multifactor";

        private HttpContext _context;

        public CacheService(HttpContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public string GetGroupDn(string name)
        {
            var key = $"{KEY_PREFIX}:dn:{name}";
            return GetItem(key);
        }
        public void SetGroupDn(string name, string dn)
        {
            var key = $"{KEY_PREFIX}:dn:{name}";
            SetItem(key, dn, GROUP_DN_CACHE_TIMEOUT);
        }

        public bool? GetMembership(string user)
        {
            var key = $"{KEY_PREFIX}:is2fa:{user}";
            var val = GetItem(key);

            switch (val)
            {
                case "1":
                    return true;
                case "0":
                    return false;
                default:
                    return null;
            }
        }
        public void SetMembership(string user, bool isMember)
        {
            var key = $"{KEY_PREFIX}:is2fa:{user}";
            var val = isMember ? "1" : "0";

            var ttl = Configuration.Current.ActiveDirectory2FaGroupMembershipCacheTimout >= 0 ?
                Configuration.Current.ActiveDirectory2FaGroupMembershipCacheTimout.Value :
                USER_MEMBERSHIP_CACHE_TIMEOUT;

            SetItem(key, val, ttl);
        }

        private string GetItem(string key)
        {
            return _context.Cache.Get(key) as string;
        }
        private void SetItem(string key, string value, int ttl)
        {
            _context.Cache.Add(key, value, null, DateTime.Now.AddMinutes(ttl), Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
        }
    }
}