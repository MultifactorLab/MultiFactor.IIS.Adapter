using System;
using System.Web;

namespace MultiFactor.IIS.Adapter.Services
{
    public class AuthChecker
    {
        private readonly HttpContextBase _context;
        private readonly TokenValidationService _validationService;

        public AuthChecker(HttpContextBase context, TokenValidationService validationService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        }

        public bool IsAuthenticated(string user)
        {
            var multifactorCookie = _context.Request.Cookies[Constants.COOKIE_NAME];
            if (multifactorCookie == null) return false;

            var isValidToken = _validationService.TryVerifyToken(multifactorCookie.Value, out string userName);
            if (!isValidToken) return false;

            return Util.CanonicalizeUserName(userName) == Util.CanonicalizeUserName(user);
        }
    }
}