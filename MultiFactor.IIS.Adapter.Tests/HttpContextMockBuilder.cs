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

        private HttpContextMockBuilder()
        {
            HttpContext = new Mock<HttpContextBase>();
            Request = new Mock<HttpRequestBase>();
            Response = new Mock<HttpResponseBase>();
            RequestCookies = new HttpCookieCollection();
            Form = new NameValueCollection();
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
            Response.SetupGet(x => x.Cookies).Returns(RequestCookies);

            HttpContext.SetupGet(context => context.Request).Returns(Request.Object);
            HttpContext.SetupGet(context => context.Response).Returns(Response.Object);
            return HttpContext;
        }
    }
}
