using MultiFactor.IIS.Adapter.Services.Ldap;
using System;

namespace MultiFactor.IIS.Adapter.Interop
{
    public class UserSearchContext
    {
        public string Domain { get; set; }
        public LdapIdentity UserIdentity { get; set; }

        public UserSearchContext(string domain, string upn, string rawUserName)
        {
            if (string.IsNullOrWhiteSpace(domain))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(domain));
            if (string.IsNullOrWhiteSpace(upn))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(upn));
            Domain = domain;
            UserIdentity = LdapIdentity.Parse(upn).WithRawName(rawUserName);
        }

        public override string ToString() => $"User:{UserIdentity.RawName}, UPN:{UserIdentity.Name}, Domain:{Domain}";
    }
}