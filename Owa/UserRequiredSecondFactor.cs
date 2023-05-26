using MultiFactor.IIS.Adapter.Services;
using System;

namespace MultiFactor.IIS.Adapter.Owa
{
    public class UserRequiredSecondFactor
    {
        private readonly ActiveDirectoryService _activeDirectory;

        public UserRequiredSecondFactor(ActiveDirectoryService activeDirectory)
        {
            _activeDirectory = activeDirectory ?? throw new ArgumentNullException(nameof(activeDirectory));
        }

        public bool Execute(string samAccountName)
        {
            if (samAccountName is null)
            {
                throw new ArgumentNullException(nameof(samAccountName));
            }

            if (string.IsNullOrEmpty(Configuration.Current.ActiveDirectory2FaGroup))
            {
                return true;
            }

            return _activeDirectory.ValidateMembership(samAccountName);
        }
    }
}