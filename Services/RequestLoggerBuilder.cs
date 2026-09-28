using System.Text;
using System.Web;

namespace MultiFactor.IIS.Adapter.Services
{
    public class RequestLoggerBuilder
    {
        public static string BuildRequestLog(HttpContextBase context)
        {
            var messageHttpMethod = context.Request.HttpMethod;
            var requestPath = context.Request.Path;
            var appPath = context.Request.ApplicationPath;
            var builder = new StringBuilder("Request: ");
            string deviceId = context.Request.Params["DeviceId"];
            string user = context.Request.Params["User"];
            var cmd = context.Request.Params["Cmd"];
            builder.AppendLine($"User: {user}, DeviceId: {deviceId}, Cmd: {cmd}");
            builder.AppendLine($"{nameof(context.Request.HttpMethod)} = {messageHttpMethod}");
            builder.AppendLine($"{nameof(context.Request.Path)} = {requestPath}");
            builder.AppendLine($"{nameof(context.Request.ApplicationPath)} = {appPath}");

            string deviceType = context.Request.Params["DeviceType"];
            string userAgent = context.Request.Params["HTTP_USER_AGENT"];
            builder.AppendLine("Device Type = " + deviceType);
            builder.AppendLine("Device UserAgent = " + userAgent);

            return builder.ToString();
        }
    }
}