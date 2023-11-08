using MultiFactor.IIS.Adapter.Extensions;
using System;
using System.Web;

namespace MultiFactor.IIS.Adapter.Services
{
    internal class MfaApiRequestExecutor
    {
        private readonly HttpContextBase _context;
        private readonly AccessUrlGetter _urlGetter;
        private readonly Logger _logger;

        public MfaApiRequestExecutor(HttpContextBase context, AccessUrlGetter urlGetter, Logger logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _urlGetter = urlGetter ?? throw new ArgumentNullException(nameof(urlGetter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Execute(string postbackUrl, string appRootPath)
        {

            try
            {
                var multiFactorAccessUrl = _urlGetter.GetAccessUrl(_context.User.Identity.Name, postbackUrl);
                _context.Response.Redirect(multiFactorAccessUrl, true);
            }
            catch (Exception ex) when (NeedToBypass(ex))
            {
                // set bypass flag
                _logger.Warn($"Bypassing the second factor for user '{_context.User.Identity.Name}' due to an API error '{ex.Message}'");
                _context.GetCacheAdapter().SetApiUnreachable(Util.CanonicalizeUserName(_context.User.Identity.Name), true);
                _context.Response.Redirect(appRootPath, true);
            }
        }

        private static bool NeedToBypass(Exception ex)
        {
            return ex.Message?.StartsWith(Constants.API_UNREACHABLE_CODE) == true && Configuration.Current.BypassSecondFactorWhenApiUnreachable;
        }
    }
}