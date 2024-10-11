using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiFactor.IIS.Adapter.Services.Ldap.Profile
{
    internal class LdapProfile : ILdapProfile
    {
        private readonly LdapIdentity _identity;
        private readonly string _twoFaIdentityAttrName;
        private readonly string[] _phoneAttrs;

        private readonly Dictionary<string, HashSet<string>> _attrs = new Dictionary<string, HashSet<string>>(new AttributeKeyComparer());

        /// <summary>
        /// Name we got from the authentication pipeline. Most often it is netbios
        /// </summary>
        public string RawUserName => _identity.RawName;

        /// <summary>
        /// Normalized name. Can be used to search in the AD and as 2fa identity by default.
        /// </summary>
        public string FriendlyUserName => _identity.Name;

        /// <summary>
        /// Use only if corresponding setting is specified in the config
        /// </summary>
        public string Custom2FAIdentity => GetAttr(_twoFaIdentityAttrName).FirstOrDefault();

        public string Phone
        {
            get
            {
                foreach (var attr in _phoneAttrs)
                {
                    var values = GetAttr(attr);
                    if (values.Length != 0)
                    {
                        return values[0];
                    }
                }
                return null;
            }
        }

        public LdapProfile(LdapIdentity identity, Configuration configuration)
        {
            if (identity == null)
            {
                throw new ArgumentException($"'{nameof(identity)}' cannot be null.", nameof(identity));
            }
            
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            _identity = identity;
            _attrs[identity.TypeName] = new HashSet<string>{ identity.Name };
            _twoFaIdentityAttrName = configuration.HasTwoFaIdentityAttribute 
                ? configuration.TwoFaIdentityAttribute 
                : identity.TypeName;

            var phoneAttrs = new List<string>
            {
                "telephoneNumber"
            };
            
            if (configuration.PhoneAttributes != null && configuration.PhoneAttributes.Length != 0)
            {
                phoneAttrs.AddRange(configuration.PhoneAttributes);
            }
            else
            {
                phoneAttrs.Add("telephoneNumber");
            }
            
            _phoneAttrs = phoneAttrs.Distinct(new AttributeKeyComparer()).ToArray();
        }

        public void AddAttribute(string attribute, IEnumerable<string> values)
        {
            if (attribute is null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            if (values is null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (_attrs.TryGetValue(attribute, out var hashset))
            {
                foreach (var value in values)
                {
                    hashset.Add(value);
                }
            }
            else
            {
                var h = new HashSet<string>();
                foreach (var value in values)
                {
                    h.Add(value);
                }
                _attrs[attribute] = h;
            }
        }

        private string[] GetAttr(string key)
        {
            return _attrs.TryGetValue(key, out var values) 
                ? values.ToArray() 
                : new string[0];
        }
    }
}