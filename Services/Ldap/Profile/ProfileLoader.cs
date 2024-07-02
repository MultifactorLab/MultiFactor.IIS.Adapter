using MultiFactor.IIS.Adapter.Extensions;
using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;

namespace MultiFactor.IIS.Adapter.Services.Ldap.Profile
{
    public class ProfileLoader
    {
        private readonly LdapConnectionAdapter _adapter;
        private readonly Configuration _config;
        private readonly Logger _logger;

        public ProfileLoader(LdapConnectionAdapter adapter, Configuration config, Logger logger)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _config = config;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ILdapProfile Load(string samAccountName)
        {
            var profile = LdapProfile.Create(samAccountName, _config.TwoFAIdentityAttribyte);

            var attrs = new List<string>();
            attrs.AddRange(_config.PhoneAttributes);
            if (_config.UseIdentityAttribute)
            {
                attrs.Add(_config.TwoFAIdentityAttribyte);
            }

            try
            {
                var baseDn = _adapter.Domain.GetDn();
                var searchFilter = $"(&(sAMAccountName={samAccountName})(objectClass=user))";

                var response = _adapter.Search(baseDn, searchFilter, SearchScope.Subtree, attrs.ToArray());
                if (response.Entries.Count == 0) return profile.Build();

                SetPhone(profile, response);
                Set2FAIdentityAttribute(profile, response);
            }
            catch (LdapException ex)
            {
                _logger.Error($"{ex}\r\nLDAPErrorCode={ex.ErrorCode}, ServerErrorMessage={ex.ServerErrorMessage}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
            }

            return profile.Build();
        }

        private void Set2FAIdentityAttribute(ILdapProfileBuilder profile, SearchResponse response)
        {
            var identity = response.Entries[0].Attributes[_config.TwoFAIdentityAttribyte]?[0]?.ToString();
            if (identity != null)
            {
                profile.Set2FAIdentityAttribute(identity);
            }
        }

        private void SetPhone(ILdapProfileBuilder profile, SearchResponse response)
        {
            foreach (var attr in _config.PhoneAttributes)
            {
                var value = response.Entries[0].Attributes[attr]?[0]?.ToString();
                if (value == null) continue;

                profile.SetPhone(value, attr);
                break;
            }
        }
    }
}