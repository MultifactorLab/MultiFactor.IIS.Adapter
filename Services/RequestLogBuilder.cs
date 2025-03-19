using System.Collections.Specialized;
using System.Text;
using System.Web;

namespace MultiFactor.IIS.Adapter.Services
{
    public class RequestLoggerBuilder
    {
        public static string BuildRequestLog(HttpContextBase context)
        {
            var messageHttpMethod = context.Request.HttpMethod;
            var requestUrl = context.Request.Url?.AbsoluteUri;
            var requestParams = context.Request.Params;
            var form = context.Request.Form;
            var requestPath = context.Request.Path;
            var requestQueryString = context.Request.QueryString;
            var requestHeaders = context.Request.Headers;
            var appPath = context.Request.ApplicationPath;
            var identity = context.User?.Identity?.Name;
            var authType = context.User?.Identity?.AuthenticationType;
            var isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;
            var builder = new StringBuilder("Request: ");

            builder.AppendLine($"{nameof(context.Request.HttpMethod)} = {messageHttpMethod}");
            builder.AppendLine($"{nameof(context.Request.Url.AbsoluteUri)} = {requestUrl}");
            builder.AppendLine($"{nameof(context.Request.Path)} = {requestPath}");
            builder.AppendLine($"{nameof(context.Request.QueryString)} = {requestQueryString}");
            builder.AppendLine($"{nameof(context.Request.ApplicationPath)} = {appPath}");
            
            builder.AppendLine($"{nameof(context.User.Identity.Name)} = {identity}");
            builder.AppendLine($"{nameof(context.User.Identity.IsAuthenticated)} = {isAuthenticated}");
            builder.AppendLine($"{nameof(context.User.Identity.AuthenticationType)} = {authType}");
            
            string user = context.Request.Params["User"];
            builder.AppendLine("User = " + user);
            
            string deviceId = context.Request.Params["DeviceId"];
            builder.AppendLine("Device Id = " + deviceId);
            
            string deviceType = context.Request.Params["DeviceType"];
            string userAgent = context.Request.Params["HTTP_USER_AGENT"];
            builder.AppendLine("Device Type = " + deviceType);
            builder.AppendLine("Device UserAgent = " + userAgent);

            var cmd = context.Request.Params["Cmd"];
            if (!string.IsNullOrWhiteSpace(cmd))
            {
                builder.AppendLine("Cmd = " + cmd);
            }

            builder.AppendLine("--------------------------");
            builder.AppendLine($"Headers:");
            AddParams(builder, requestHeaders);

            builder.AppendLine("--------------------------");
            builder.AppendLine($"Form:");
            AddParams(builder, form);

            builder.AppendLine("--------------------------");
            builder.AppendLine($"Parameters:");
            AddParams(builder, requestParams);

            return builder.ToString();
        }
        
        private static void AddParams(StringBuilder builder, NameValueCollection valueCollection)
        {
            foreach (var key in valueCollection.AllKeys)
            {
                builder.AppendLine($"{key} = {valueCollection[key]}");
            }
        }
    }
}
