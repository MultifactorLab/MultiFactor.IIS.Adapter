using System;
using System.Collections.Generic;

namespace MultiFactor.IIS.Adapter.Services.Ldap.Profile
{
    internal class AttributeKeyComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            if (x == null || y == null)
            {
                return false;
            }

            return x.Equals(y, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(string obj)
        {
            return obj.ToLower().GetHashCode();
        }
    }
}