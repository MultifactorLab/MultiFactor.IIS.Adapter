using MultiFactor.IIS.Adapter.Extensions;
using System;
using System.Web;

namespace MultiFactor.IIS.Adapter.Services
{
    internal static class MfaApiRequestExecutorFactory
    {
        public static MfaApiRequestExecutor CreateOwa(HttpContextBase context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var ad = new ActiveDirectoryService(context.GetCacheAdapter(), Logger.Owa);
            var api = new MultiFactorApiClient(Logger.API);
            var getter = new AccessUrlGetter(ad, api);

            return new MfaApiRequestExecutor(context, getter, Logger.Owa);
        }

        public static MfaApiRequestExecutor CreateIIS(HttpContextBase context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var ad = new ActiveDirectoryService(context.GetCacheAdapter(), Logger.IIS);
            var api = new MultiFactorApiClient(Logger.API);
            var getter = new AccessUrlGetter(ad, api);

            return new MfaApiRequestExecutor(context, getter, Logger.IIS);
        }
    }
}