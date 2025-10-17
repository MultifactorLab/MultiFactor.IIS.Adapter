using MultiFactor.IIS.Adapter.Dto;
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
        const string SUPPORT = "support";
        const string ADMIN = "admin";

        private readonly HttpContextBase _context;

        public CacheAdapter(HttpContextBase context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public string GetGroupDn(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(groupName));
            }

            var key = $"{KEY_PREFIX}:{DN_KEY}:{groupName}";
            return GetItem(key);
        }

        public void SetGroupDn(string groupName, string dn)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(groupName));
            }

            var key = $"{KEY_PREFIX}:{DN_KEY}:{groupName}";
            SetItem(key, dn, GROUP_DN_CACHE_TIMEOUT);
        }

        public bool? GetMembership(string samAccountName)
        {
            if (string.IsNullOrWhiteSpace(samAccountName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(samAccountName));
            }

            var key = $"{KEY_PREFIX}:{IS_2FA_KEY}:{samAccountName}";
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

        public void SetMembership(string samAccountName, bool isMember)
        {
            if (string.IsNullOrWhiteSpace(samAccountName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(samAccountName));
            }

            var key = $"{KEY_PREFIX}:{IS_2FA_KEY}:{samAccountName}";
            var val = isMember ? "1" : "0";

            SetItem(key, val, Configuration.Current.ActiveDirectoryCacheTimout);
        }

        public ILdapProfile GetProfile(string samAccountName)
        {
            if (string.IsNullOrWhiteSpace(samAccountName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(samAccountName));
            }

            var key = $"{KEY_PREFIX}:{PROFILE_KEY}:{samAccountName}";
            return GetItem<ILdapProfile>(key);
        }
        
        public void SetProfile(string samAccountName, ILdapProfile profile)
        {
            if (string.IsNullOrWhiteSpace(samAccountName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(samAccountName));
            }
            
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var key = $"{KEY_PREFIX}:{PROFILE_KEY}:{samAccountName}";
            SetItem(key, profile, Configuration.Current.ActiveDirectoryCacheTimout);
        }

        public bool GetApiUnreachable(string samAccountName)
        {
            if (string.IsNullOrWhiteSpace(samAccountName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(samAccountName));
            }

            var key = $"{KEY_PREFIX}:{API_UNREACHABLE}:{samAccountName}";
            return GetItem<bool>(key);
        }

        public void SetApiUnreachable(string samAccountName, bool bypass)
        {
            if (string.IsNullOrWhiteSpace(samAccountName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(samAccountName));
            }

            var key = $"{KEY_PREFIX}:{API_UNREACHABLE}:{samAccountName}";
            SetItem(key, bypass, Configuration.Current.ApiLifeCheckInterval);
        }

        public ScopeSupportInfoDto GetSupportAdmin(string samAccountName)
        {
            if (string.IsNullOrWhiteSpace(samAccountName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(samAccountName));
            }

            var key = $"{SUPPORT}:{ADMIN}:{samAccountName}";
            return GetItem<ScopeSupportInfoDto>(key);
        }

        public void SetSupportAdmin(string samAccountName, ScopeSupportInfoDto admin)
        {
            if (string.IsNullOrWhiteSpace(samAccountName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(samAccountName));
            }

            var key = $"{SUPPORT}:{ADMIN}:{samAccountName}";
            SetItem(key, admin, Configuration.Current.ApiLifeCheckInterval);
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