using System.Web;

namespace MultiFactor.IIS.Adapter.Core
{
    public abstract class HttpModuleBase : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.BeginRequest += (sender, e) => OnBeginRequest(new HttpContextWrapper(((HttpApplication)sender).Context));
            context.PostAuthorizeRequest += (sender, e) => OnPostAuthorizeRequest(new HttpContextWrapper(((HttpApplication)sender).Context));
        }

        public virtual void Dispose() { }
        public virtual void OnBeginRequest(HttpContextBase context) { }
        public virtual void OnPostAuthorizeRequest(HttpContextBase context) { }
    }
}