using System;

namespace MultiFactor.IIS.Adapter.Services.Ldap
{
    public class LdapIdentity
    {
        /// <summary>
        /// Name we got from the authentication pipeline. Most often it is netbios
        /// </summary>
        public string RawName { get; private set; }

        /// <summary>
        /// Normalized name. Can be used to search in the AD and as 2fa identity by default
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Most often it matches the RawName
        /// </summary>
        public string NetBiosName { get; private set; } = string.Empty;

        /// <summary>
        /// Type of name in terms of AD
        /// </summary>
        public IdentityType Type { get; set; }

        /// <summary>
        /// AD attribute name
        /// </summary>
        public string TypeName
        {
            get
            {
                switch (Type)
                {
                    case IdentityType.SamAccountName:
                        return "sAMAccountName";
                    case IdentityType.UserPrincipalName:
                        return "userPrincipalName";
                    default:
                        return "name";
                }
            }
        }
        
        public static LdapIdentity Parse(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            var identity = name.ToLower();
            string netBiosName = string.Empty;
            //remove DOMAIN\\ prefix
            var type = IdentityType.SamAccountName;
            var index = identity.IndexOf("\\");
            if (index > 0)
            {
                type = IdentityType.SamAccountName;
                netBiosName = identity.Substring(0, index);
                identity = identity.Substring(index + 1);
            }

            // rare case
            if (identity.Contains("@"))
            {
                type = IdentityType.UserPrincipalName;
            }

            return new LdapIdentity
            {
                RawName = name,
                Name = identity,
                Type = type,
                NetBiosName = netBiosName,
            };
        }

        public bool HasNetbiosName() => !string.IsNullOrEmpty(NetBiosName);

        public LdapIdentity WithRawName(string rawName)
        {
            this.RawName = rawName;
            return this;
        }
    }

    // from System.DirectoryServices.AccountManagement
    public enum IdentityType
    {
        /// <summary>The identity is a Security Account Manager (SAM) name.</summary>
        SamAccountName,
        /// <summary>The identity is a name.</summary>
        Name,
        /// <summary>The identity is a User Principal Name (UPN).</summary>
        UserPrincipalName,
    }
}