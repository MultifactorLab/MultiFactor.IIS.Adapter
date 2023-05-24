using MultiFactor.IIS.Adapter.Extensions;
using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;

namespace MultiFactor.IIS.Adapter.Services.Ldap.Profile
{
    public class ProfileLoader
    {
        private readonly LdapConnectionAdapter _adapter;
        private readonly Logger _logger;

        public ProfileLoader(LdapConnectionAdapter adapter, Logger logger)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ILdapProfile Load(string samAccountName)
        {
            var profile = LdapProfile.Create(samAccountName);

            var attrs = new List<string>();
            attrs.AddRange(Configuration.Current.PhoneAttributes);
            attrs.Add("UserPrincipalName");

            try
            {
                var baseDn = _adapter.Domain.GetDn();
                var searchFilter = $"(&(sAMAccountName={samAccountName})(objectClass=user))";

                var response = _adapter.Search(baseDn, searchFilter, SearchScope.Subtree, attrs.ToArray());
                if (response.Entries.Count == 0) return profile.Build();

                SetPhone(profile, response);
                SetUpn(profile, response);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
            }

            return profile.Build();
        }

        private static void SetUpn(ILdapProfileBuilder profile, SearchResponse response)
        {
            var upn = response.Entries[0].Attributes["UserPrincipalName"]?[0]?.ToString();
            if (upn != null)
            {
                profile.SetUpn(upn);
            }
        }

        private static void SetPhone(ILdapProfileBuilder profile, SearchResponse response)
        {
            foreach (var attr in Configuration.Current.PhoneAttributes)
            {
                var existed = response.Entries[0].Attributes[attr]?[0]?.ToString();
                if (existed == null) continue;

                profile.SetPhone(attr, existed);
                break;
            }
        }
    }
}