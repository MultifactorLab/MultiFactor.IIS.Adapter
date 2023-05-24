using System;
using System.Collections.Generic;

namespace MultiFactor.IIS.Adapter.Services.Ldap.Profile
{
    public class LdapProfile : ILdapProfile, ILdapProfileBuilder
    {
        private const string _samAccountNameKey = "samAccountName";
        private const string _upnKey = "UserPrincipalName";

        private readonly Dictionary<string, string> _attrs = new Dictionary<string, string>();
        private readonly Func<string, string> _getter;
        private Func<string> _phoneGetter;

        public string SamAccountName => _getter(_samAccountNameKey);
        public string UserPrincipalName => _getter(_upnKey);
        public string Phone => _phoneGetter();

        public string this[string key] => _getter(key);


        private LdapProfile(string samAccountName)
        {
            if (string.IsNullOrWhiteSpace(samAccountName))
            {
                throw new ArgumentException($"'{nameof(samAccountName)}' cannot be null or whitespace.", nameof(samAccountName));
            }

            _attrs[_samAccountNameKey] = samAccountName;
            _getter = key => _attrs.ContainsKey(key) ? _attrs[key] : null;
            _phoneGetter = () => _getter("telephoneNumber");
        }

        public static ILdapProfileBuilder Create(string samAccountName) => new LdapProfile(samAccountName);

        public ILdapProfileBuilder SetPhone(string value, string attrName)
        {
            _attrs[attrName] = value;
            _phoneGetter = () => _attrs.ContainsKey(attrName) ? _attrs[attrName] : null;
            return this;
        }

        public ILdapProfile Build() => this;

        public ILdapProfileBuilder SetUpn(string value)
        {
            _attrs[_upnKey] = value;
            return this;
        }
    }
}