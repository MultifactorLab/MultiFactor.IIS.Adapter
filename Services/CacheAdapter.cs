using MultiFactor.IIS.Adapter.Services.Ldap.Profile;
using System;
using System.Web;
using System.Web.Caching;

namespace MultiFactor.IIS.Adapter.Services
{
    public class CacheAdapter
    {
        private static readonly TimeSpan GROUP_DN_CACHE_TIMEOUT = TimeSpan.FromMinutes(60);
        const string KEY_PREFIX = "multifactor";
        const string IS_2FA_KEY = "is2fa";
        const string DN_KEY = "dn";
        const string PROFILE_KEY = "profile";
        const string API_UNREACHABLE = "api-unreachable";

        private readonly HttpContextBase _context;

        public CacheAdapter(HttpContextBase context)
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

            SetItem(key, val, Configuration.Current.ActiveDirectoryCacheTimout);
        }

        public ILdapProfile GetProfile(string user)
        {
            var key = $"{KEY_PREFIX}:{PROFILE_KEY}:{user}";
            return GetItem<ILdapProfile>(key);
        }
        
        public void SetProfile(string user, ILdapProfile profile)
        {
            var key = $"{KEY_PREFIX}:{PROFILE_KEY}:{user}";
            SetItem(key, profile, Configuration.Current.ActiveDirectoryCacheTimout);
        }

        public bool GetApiUnreachable(string user)
        {
            var key = $"{KEY_PREFIX}:{API_UNREACHABLE}:{user}";
            return GetItem<bool>(key);
        }

        public void SetApiUnreachable(string user, bool bypass)
        {
            var key = $"{KEY_PREFIX}:{API_UNREACHABLE}:{user}";
            SetItem(key, bypass, Configuration.Current.ApiLifeCheckInterval);
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

        private void SetItem(string key, string value, TimeSpan ttl)
        {
            _context.Cache.Add(key, value, null, DateTime.Now.Add(ttl), Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
        }
        
        private void SetItem<T>(string key, T value, TimeSpan ttl)
        {
            _context.Cache.Add(key, value, null, DateTime.Now.Add(ttl), Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
        }
    }
}