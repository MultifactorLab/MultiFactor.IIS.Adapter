using System;
using System.Text;

namespace MultiFactor.IIS.Adapter.Services.Ldap
{
    internal static class LdapFilter
    {
        public static string Escape(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var escaped = new StringBuilder(value.Length);
            foreach (var character in value)
            {
                switch (character)
                {
                    case '\\':
                        escaped.Append("\\5c");
                        break;
                    case '*':
                        escaped.Append("\\2a");
                        break;
                    case '(':
                        escaped.Append("\\28");
                        break;
                    case ')':
                        escaped.Append("\\29");
                        break;
                    case '\0':
                        escaped.Append("\\00");
                        break;
                    default:
                        escaped.Append(character);
                        break;
                }
            }

            return escaped.ToString();
        }
    }
}