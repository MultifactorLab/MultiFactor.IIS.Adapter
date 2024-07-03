using MultiFactor.IIS.Adapter.Extensions;
using System;
using System.Web;

namespace MultiFactor.IIS.Adapter.Services
{
    internal class MfaApiRequestExecutor
    {
        private readonly HttpContextBase _context;
        private readonly AccessUrl _accessUrl;
        private readonly Logger _logger;

        public MfaApiRequestExecutor(HttpContextBase context, AccessUrl accessUrl, Logger logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _accessUrl = accessUrl ?? throw new ArgumentNullException(nameof(accessUrl));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Execute(string postbackUrl, string appRootPath)
        {
            try
            {
                var multiFactorAccessUrl = _accessUrl.Get(_context.User.Identity.Name, postbackUrl);
                _context.Response.Redirect(multiFactorAccessUrl, true);
            }
            catch (Exception ex) when (NeedToBypass(ex))
            {
                _logger.Error($"Debug top exception: {ex.Message}");

                _logger.Warn($"Bypassing the second factor for user '{_context.User.Identity.Name}' due to an API error '{ex.Message}'. Bypass session duration: {Configuration.Current.ApiLifeCheckInterval.TotalMinutes} min");
                _context.GetCacheAdapter().SetApiUnreachable(Util.CanonicalizeUserName(_context.User.Identity.Name), true);
                _context.Response.Redirect(appRootPath, true);
            }
        }

        private bool NeedToBypass(Exception ex)
        {
            _logger.Error($"Debug exception: {ex.Message}");
            return ex.Message?.StartsWith(Constants.API_UNREACHABLE_CODE) == true && Configuration.Current.BypassSecondFactorWhenApiUnreachable;
        }
    }
}
