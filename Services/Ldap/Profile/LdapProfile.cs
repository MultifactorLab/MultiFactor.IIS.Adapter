using System;
using System.Collections.Generic;

namespace MultiFactor.IIS.Adapter.Services.Ldap.Profile
{
    public class LdapProfile : ILdapProfile, ILdapProfileBuilder
    {
        private const string _samAccountNameKey = "samAccountName";
        private readonly string _2FAIdentityAttrName;

        private readonly Dictionary<string, string> _attrs = new Dictionary<string, string>();
        private Func<string> _phoneGetter;

        public string SamAccountName => GetAttr(_samAccountNameKey);
        public string TwoFAIdentity => GetAttr(_2FAIdentityAttrName);
        public string Phone => _phoneGetter();

        public string this[string key] => GetAttr(key);


        private LdapProfile(string samAccountName, string twoFAIdentityAttrName)
        {
            if (string.IsNullOrWhiteSpace(samAccountName))
            {
                throw new ArgumentException($"'{nameof(samAccountName)}' cannot be null or whitespace.", nameof(samAccountName));
            }

            if (string.IsNullOrWhiteSpace(twoFAIdentityAttrName))
            {
                throw new ArgumentException($"'{nameof(twoFAIdentityAttrName)}' cannot be null or whitespace.", nameof(twoFAIdentityAttrName));
            }

            _attrs[_samAccountNameKey] = samAccountName;
            _2FAIdentityAttrName = twoFAIdentityAttrName;
            _phoneGetter = () => GetAttr("telephoneNumber");
        }

        public static ILdapProfileBuilder Create(string samAccountName, string twoFAIdentityAttrName) => new LdapProfile(samAccountName, twoFAIdentityAttrName);

        public ILdapProfileBuilder SetPhone(string value, string attrName)
        {
            _attrs[attrName] = value;
            _phoneGetter = () => _attrs.ContainsKey(attrName) ? _attrs[attrName] : null;
            return this;
        }

        public ILdapProfile Build() => this;

        public ILdapProfileBuilder Set2FAIdentityAttribute(string value)
        {
            _attrs[_2FAIdentityAttrName] = value;
            return this;
        }

        private string GetAttr(string key) => _attrs.ContainsKey(key) ? _attrs[key] : null;
    }
}