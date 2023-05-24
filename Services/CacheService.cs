using MultiFactor.IIS.Adapter.Services.Ldap.Profile;
using System;
using System.Web;
using System.Web.Caching;

namespace MultiFactor.IIS.Adapter.Services
{
    public class CacheService
    {
        const int GROUP_DN_CACHE_TIMEOUT = 60;
        const int AD_CACHE_TIMEOUT = 15;
        const string KEY_PREFIX = "multifactor";
        const string IS_2FA_KEY = "is2fa";
        const string DN_KEY = "dn";
        const string PROFILE_KEY = "profile";

        private readonly HttpContext _context;

        public CacheService(HttpContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public string GetGroupDn(string name)
        {
            var key = $"{KEY_PREFIX}:{DN_KEY}:{name}";
            return GetItem(key);
        }

        public void SetGroupDn(string name, string dn)
        {
            var key = $"{KEY_PREFIX}:{DN_KEY}:{name}";
            SetItem(key, dn, GROUP_DN_CACHE_TIMEOUT);
        }

        public bool? GetMembership(string user)
        {
            var key = $"{KEY_PREFIX}:{IS_2FA_KEY}:{user}";
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
            var key = $"{KEY_PREFIX}:{IS_2FA_KEY}:{user}";
            var val = isMember ? "1" : "0";

            SetItem(key, val, GetTtl());
        }

        public ILdapProfile GetProfile(string user)
        {
            var key = $"{KEY_PREFIX}:{PROFILE_KEY}:{user}";
            return GetItem<ILdapProfile>(key);
        }
        
        public void SetProfile(string user, ILdapProfile profile)
        {
            var key = $"{KEY_PREFIX}:{PROFILE_KEY}:{user}";
            SetItem(key, profile, GetTtl());
        }

        private string GetItem(string key)
        {
            return _context.Cache.Get(key) as string;
        }
        
        private T GetItem<T>(string key)
        {
            var val = _context.Cache.Get(key);
            if (val is T typed) return typed;
            return default;
        }

        private void SetItem(string key, string value, int ttl)
        {
            _context.Cache.Add(key, value, null, DateTime.Now.AddMinutes(ttl), Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
        }
        
        private void SetItem<T>(string key, T value, int ttl)
        {
            _context.Cache.Add(key, value, null, DateTime.Now.AddMinutes(ttl), Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
        }

        private int GetTtl()
        {
            return Configuration.Current.ActiveDirectoryCacheTimout >= 0
                ? Configuration.Current.ActiveDirectoryCacheTimout
                : AD_CACHE_TIMEOUT;
        }
    }
}