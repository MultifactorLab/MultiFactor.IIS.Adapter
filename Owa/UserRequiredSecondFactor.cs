using MultiFactor.IIS.Adapter.Services;
using MultiFactor.IIS.Adapter.Services.Ldap;
using System;

namespace MultiFactor.IIS.Adapter.Owa
{
    public class UserRequiredSecondFactor
    {
        private readonly ActiveDirectoryService _activeDirectory;
        private readonly Logger _logger;

        public UserRequiredSecondFactor(ActiveDirectoryService activeDirectory, Logger logger)
        {
            _activeDirectory = activeDirectory ?? throw new ArgumentNullException(nameof(activeDirectory));
            _logger = logger;
        }

        public bool Execute(LdapIdentity identity)
        {
            if (identity is null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            if (string.IsNullOrEmpty(Configuration.Current.ActiveDirectory2FaGroup))
            {
                return true;
            }

            // very noisy log, only for debug
            //_logger.Info($"Start validate membership for {identity.RawName}");
            return _activeDirectory.ValidateMembership(identity);
        }
    }
}