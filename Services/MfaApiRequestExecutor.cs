﻿using MultiFactor.IIS.Adapter.Extensions;
using MultiFactor.IIS.Adapter.Services.Ldap;
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
            var identity = LdapIdentity.Parse(_context.User.Identity.Name);
            try
            {
                _logger.Info($"Execute 2fa for {identity.RawName}");
                var multiFactorAccessUrl = _accessUrl.Get(identity, postbackUrl);
                _context.Response.Redirect(multiFactorAccessUrl, true);
            }
            catch (Exception ex) when (NeedToBypass(ex))
            {
                _logger.Warn(
                    $"Bypassing the second factor for user '{identity.RawName}' due to an API error '{ex}'. {Environment.NewLine}" +
                    $"Bypass session duration: {Configuration.Current.ApiLifeCheckInterval.TotalMinutes} min");
                _context.GetCacheAdapter()
                    .SetApiUnreachable(identity.RawName, true);
                _context.Response.Redirect(appRootPath, true);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
                throw;
            }
        }

        private static bool NeedToBypass(Exception ex)
        {
            return ex.Message?.StartsWith(Constants.API_UNREACHABLE_CODE) == true && Configuration.Current.BypassSecondFactorWhenApiUnreachable;
        }
    }
}
