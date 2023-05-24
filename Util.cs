using System;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

namespace MultiFactor.IIS.Adapter
{
    public static class Util
    {
        public static string Base64UrlEncode(byte[] arg)
        {
            if (arg == null)
            {
                throw new ArgumentNullException(nameof(arg));
            }

            string s = Convert.ToBase64String(arg); // Regular base64 encoder
            s = s.Split('=')[0]; // Remove any trailing '='s
            s = s.Replace('+', '-'); // 62nd char of encoding
            s = s.Replace('/', '_'); // 63rd char of encoding
            return s;
        }
        
        public static byte[] Base64UrlDecode(string arg)
        {
            if (string.IsNullOrEmpty(arg))
            {
                throw new ArgumentNullException(nameof(arg));
            }
            
            string s = arg;
            s = s.Replace('-', '+'); // 62nd char of encoding
            s = s.Replace('_', '/'); // 63rd char of encoding
            switch (s.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 2: s += "=="; break; // Two pad chars
                case 3: s += "="; break; // One pad char
                default:
                    throw new System.Exception("Illegal base64url string!");
            }
            return Convert.FromBase64String(s); // Standard base64 decoder
        }

        public static byte[] HMACSHA256(byte[] key, byte[] message)
        {
            using (var hmac = new HMACSHA256(key))
            {
                return hmac.ComputeHash(message);
            }
        }

        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(unixTimeStamp);
        }

        public static string JsonSerialize(object content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            
            var seriailizer = new JavaScriptSerializer();
            return seriailizer.Serialize(content);
        }

        public static T JsonDeserialize<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentNullException(nameof(json));
            }

            var seriailizer = new JavaScriptSerializer();
            return seriailizer.Deserialize<T>(json);
        }

        /// <summary>
        /// User name without domain
        /// </summary>
        public static string CanonicalizeUserName(string userName)
        {
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName));
            }

            var identity = userName.ToLower();

            var index = identity.IndexOf("\\");
            if (index > 0)
            {
                identity = identity.Substring(index + 1);
            }

            index = identity.IndexOf("@");
            if (index > 0)
            {
                identity = identity.Substring(0, index);
            }

            return identity;
        }
    }
}