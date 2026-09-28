using Moq;
using System;
using System.Collections.Specialized;
using System.Web;

namespace MultiFactor.IIS.Adapter.Tests
{
    internal class HttpContextMockBuilder
    {
        public Mock<HttpContextBase> HttpContext { get; }
        public Mock<HttpRequestBase> Request { get; }
        public Mock<HttpResponseBase> Response { get; }
        public HttpCookieCollection RequestCookies { get; }
        public NameValueCollection Form { get; }
        public NameValueCollection Params { get; }
        public NameValueCollection Headers { get; }

        private HttpContextMockBuilder()
        {
            HttpContext = new Mock<HttpContextBase>();
            Request = new Mock<HttpRequestBase>();
            Response = new Mock<HttpResponseBase>();
            RequestCookies = new HttpCookieCollection();
            Form = new NameValueCollection();
            Params = new NameValueCollection();
            Headers = new NameValueCollection();
        }

        public static Mock<HttpContextBase> Create(Action<HttpContextMockBuilder> build = null)
        {
            var builder = new HttpContextMockBuilder();
            build?.Invoke(builder);
            return builder.Build();
        }

        private Mock<HttpContextBase> Build()
        {
            Request.SetupGet(x => x.Form).Returns(Form);
            Request.SetupGet(x => x.Params).Returns(Params);
            Request.SetupGet(x => x.Headers).Returns(Headers);
            Response.SetupGet(x => x.Cookies).Returns(RequestCookies);

            HttpContext.SetupGet(context => context.Request).Returns(Request.Object);
            HttpContext.SetupGet(context => context.Response).Returns(Response.Object);
            HttpContext.SetupGet(context => context.Cache).Returns(HttpRuntime.Cache);
            return HttpContext;
        }
    }
}
