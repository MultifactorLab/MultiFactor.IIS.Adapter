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
        private readonly Logger _logger;

        public ProfileLoader(LdapConnectionAdapter adapter, Configuration config, Logger logger)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger;
        }

        public ILdapProfile Load(LdapIdentity user)
        {
            if (string.IsNullOrWhiteSpace(user.Name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(user));
            }

            var profile = new LdapProfile(user, _config);

            var queryAttributes = new List<string>();
            queryAttributes.AddRange(_config.PhoneAttributes);
            if (_config.HasTwoFaIdentityAttribute)
            {
                queryAttributes.Add(_config.TwoFaIdentityAttribute);
            }
            
            var baseDn = _adapter.Domain.GetDn(); 

            var searchFilter = $"(&(objectClass=user)({user.TypeName}={user.Name}))";

            //only this domain
            var response = _adapter.Search(baseDn, searchFilter, SearchScope.Subtree,false, queryAttributes.ToArray());
            
            if (response.Entries.Count != 0)
            {
                // very noisy log,only for debug
                // _logger.Info($"Success search for {user.Name} in {baseDn} with filter {searchFilter}, {response.Entries.Count} entries");
                FillProfile(response, queryAttributes, profile);
                return profile;
            }

            //with ReferralChasing 
            response = _adapter.Search(baseDn, searchFilter, SearchScope.Subtree, true, queryAttributes.ToArray());

            if (response.Entries.Count != 0)
            {
                // very noisy log,only for debug
                // _logger.Info($"Success referral search for {user} in {baseDn} with filter {searchFilter}, {response.Entries.Count} entries");
                FillProfile(response, queryAttributes, profile);
                return profile;
            }
            
            _logger.Info($"User {user} was not found in {baseDn}");
            return profile;
        }

        private static void FillProfile(SearchResponse response, List<string> queryAttributes, LdapProfile profile)
        {
            var attributes = response.Entries[0].Attributes;
            foreach (var attr in queryAttributes)
            {
                var values = attributes[attr]?
                    .GetValues(typeof(string))
                    .Cast<string>().ToArray() ?? new string[0];
                
                profile.AddAttribute(attr, values);
            }
        }
    }
}