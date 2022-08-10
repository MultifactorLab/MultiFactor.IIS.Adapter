using MultiFactor.IIS.Adapter.Owa;
using System;
using System.Text;

namespace MultiFactor.IIS.Adapter.Services
{
    /// <summary>
    /// Service to load public key and verify token signature, issuer and expiration date
    /// </summary>
    public class TokenValidationService
    {
        /// <summary>
        /// Verify JWT
        /// </summary>
        public string VerifyToken(string jwt)
        {
            //https://multifactor.ru/docs/integration/

            if (string.IsNullOrEmpty(jwt))
            {
                throw new ArgumentNullException(nameof(jwt));
            }

            var parts = jwt.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            var head = parts[0];
            var body = parts[1];
            var sign = parts[2];

            //validate JwtHS256 token signature
            var key = Encoding.UTF8.GetBytes(Configuration.Current.ApiSecret);
            var message = Encoding.UTF8.GetBytes($"{head}.{body}");

            var computedSign = Util.Base64UrlEncode(Util.HMACSHA256(key, message));

            if (computedSign != sign)
            {
                throw new Exception("Invalid token signature");
            }

            var decodedBody = Encoding.UTF8.GetString(Util.Base64UrlDecode(body));
            var json = Util.JsonToDictionary(decodedBody);

            //validate audience
            var aud = json["aud"] as string;
            if (aud != Configuration.Current.ApiKey)
            {
                throw new Exception("Invalid token audience");
            }

            //validate expiration date
            var iat = Convert.ToInt64(json["exp"]);
            if (Util.UnixTimeStampToDateTime(iat) < DateTime.UtcNow)
            {
                throw new Exception("Expired token");
            }

            //identity
            var sub = json["sub"] as string;
            if (string.IsNullOrEmpty(sub))
            {
                throw new Exception("Name ID not found");
            }

            //as is logged user without transformation
            var rawUserName = json[Constants.RAW_USER_NAME_CLAIM] as string;

            return rawUserName ?? sub;
        }

        /// <summary>
        /// Verify JWT safe
        /// </summary>
        public bool TryVerifyToken(string jwt, out string identity)
        {
            try
            {
                identity = VerifyToken(jwt);
                return true;
            }
            catch
            {
                identity = null;
                return false;
            }
        }
    }
}