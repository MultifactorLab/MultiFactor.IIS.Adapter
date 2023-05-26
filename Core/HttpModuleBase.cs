using System.Web;

namespace MultiFactor.IIS.Adapter.Core
{
    public abstract class HttpModuleBase : IHttpModule
    {
        private readonly object _sync = new object();

        public void Init(HttpApplication context)
        {
            if (Configuration.Current == null)
            {
                //load configuration from web.config
                lock (_sync)
                {
                    Configuration.Load();
                }
            }

            context.BeginRequest += (sender, e) => OnBeginRequest(new HttpContextWrapper(((HttpApplication)sender).Context));
            context.PostAuthorizeRequest += (sender, e) => OnPostAuthorizeRequest(new HttpContextWrapper(((HttpApplication)sender).Context));
        }

        public virtual void Dispose() { }
        public virtual void OnBeginRequest(HttpContextBase context) { }
        public virtual void OnPostAuthorizeRequest(HttpContextBase context) { }
    }
}