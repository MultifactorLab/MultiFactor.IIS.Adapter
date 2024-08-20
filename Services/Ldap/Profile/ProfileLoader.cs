using MultiFactor.IIS.Adapter.Extensions;
using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;

namespace MultiFactor.IIS.Adapter.Services.Ldap.Profile
{
    public class ProfileLoader
    {
        private readonly LdapConnectionAdapter _adapter;
        private readonly Configuration _config;

        public ProfileLoader(LdapConnectionAdapter adapter, Configuration config)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public ILdapProfile Load(string samAccountName)
        {
            if (string.IsNullOrWhiteSpace(samAccountName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(samAccountName));
            }

            var profile = new LdapProfile(samAccountName, _config);

            var queryAttributes = new List<string>();
            queryAttributes.AddRange(_config.PhoneAttributes);
            if (_config.HasTwoFaIdentityAttribute)
            {
                queryAttributes.Add(_config.TwoFaIdentityAttribute);
            }
            
            var baseDn = _adapter.Domain.GetDn();
            var searchFilter = $"(&(sAMAccountName={samAccountName})(objectClass=user))";

            var response = _adapter.Search(baseDn, searchFilter, SearchScope.Subtree, queryAttributes.ToArray());
            if (response.Entries.Count == 0)
            {
                return profile;
            }

            var attributes = response.Entries[0].Attributes;
            foreach (var attr in queryAttributes)
            {
                var values = attributes[attr]?
                    .GetValues(typeof(string))
                    .Cast<string>().ToArray() ?? new string[0];
                
                profile.AddAttribute(attr, values);
            }
            
            return profile;
        }
    }
}