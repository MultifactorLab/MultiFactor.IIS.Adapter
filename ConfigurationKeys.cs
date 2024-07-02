namespace MultiFactor.IIS.Adapter
{
    public static class ConfigurationKeys
    {
        private const string _prefix = "multifactor";

        public static readonly string ApiUrl = $"{_prefix}:api-url";
        public static readonly string ApiKey = $"{_prefix}:api-key";
        public static readonly string ApiSecret = $"{_prefix}:api-secret";
        public static readonly string ApiProxy = $"{_prefix}:api-proxy";
        public static readonly string BypassSecondFactorWhenApiUnreachable = $"{_prefix}:bypass-second-factor-when-api-unreachable";

        public static readonly string ActiveDirectoryDomain = $"{_prefix}:active-directory-domain";
        public static readonly string ActiveDirectory2FAGroup = $"{_prefix}:active-directory-2fa-group";
        public static readonly string ActiveDirectory2FAGroupMembershipCacheTimeout = $"{_prefix}:active-directory-2fa-group-membership-cache-timeout";
        public static readonly string ActiveDirectoryCacheTimeout = $"{_prefix}:active-directory-cache-timeout";
        public static readonly string ApiLifeCheckInterval = $"{_prefix}:api-life-check-interval";

        public static readonly string UseUpnAsIdentity = $"{_prefix}:use-upn-as-identity";
        public static readonly string PhoneAttribute = $"{_prefix}:phone-attribute";
    }
}