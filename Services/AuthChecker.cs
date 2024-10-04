using MultiFactor.IIS.Adapter.Services.Ldap;
using System;
using System.Web;

namespace MultiFactor.IIS.Adapter.Services
{
    public class AuthChecker
    {
        private readonly HttpContextBase _context;
        private readonly TokenValidationService _validationService;
        private readonly Logger _logger;

        public AuthChecker(HttpContextBase context, TokenValidationService validationService, Logger logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _logger = logger;
        }

        public bool IsAuthenticated(string rawUsername)
        {
            var multifactorCookie = _context.Request.Cookies[Constants.COOKIE_NAME];
            if (multifactorCookie == null)
            {
                return false;
            }

            var isValidToken = _validationService.TryVerifyToken(multifactorCookie.Value, out string userName);
            if (!isValidToken)
            {
                return false;
            }

            _logger.Info($"Сomparison netbios name of local user {rawUsername} and mf user {userName}");
            return rawUsername == userName;
        }
    }
}