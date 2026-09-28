using MultiFactor.IIS.Adapter.Dto;
using MultiFactor.IIS.Adapter.Properties;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MultiFactor.IIS.Adapter.Services
{
    /// <summary>
    /// Service to interact with MultiFactor API
    /// </summary>
    public class MultiFactorApiClient
    {
        private readonly Logger _logger;
        private readonly Func<string> _getTraceId;

        public MultiFactorApiClient(Logger logger, Func<string> getTraceId)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _getTraceId = getTraceId ?? throw new ArgumentNullException(nameof(getTraceId));
        }

        public string CreateRequest(string identity, string rawUserName, string postbackUrl, string name, string email, string phone)
        {
            try
            {
                //make sure we can communicate securely
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                //payload
                var payload = Util.JsonSerialize(new
                {
                    Identity = identity,
                    name,
                    email,
                    phone,
                    Callback = new
                    {
                        Action = postbackUrl,
                        Target = "_self"
                    },
                    claims = new Dictionary<string, string>
                    {
                        {  Constants.RAW_USER_NAME_CLAIM, rawUserName }
                    }
                });

                var requestData = Encoding.UTF8.GetBytes(payload);
                byte[] responseData = null;

                //basic authorization 
                var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Configuration.Current.ApiKey}:{Configuration.Current.ApiSecret}"));

                _logger.Info($"Create mfa request to api for {identity}");

                using (var web = new WebClient())
                {
                    web.Headers.Add("Content-Type", "application/json");
                    web.Headers.Add("Authorization", $"Basic {auth}");
                    web.Headers.Add("mf-trace-id", _getTraceId());

                    TryApplyProxy(web, Configuration.Current.ApiProxy);

                    responseData = web.UploadData($"{Configuration.Current.ApiUrl}/access/requests", "POST", requestData);
                }

                var responseJson = Encoding.UTF8.GetString(responseData);
                var response = Util.JsonDeserialize<MultiFactorWebResponseDto<MultiFactorAccessPageDto>>(responseJson);
                if (!response.Success)
                {
                    _logger.Error($"Got unsuccessful response from API: {responseJson}");
                    throw new Exception(response.Message);
                }
                _logger.Info($"Successfully get url {response.Model.Url} for {identity}");
                return response.Model.Url;
            }
            catch (WebException wex) // webclient way to catch unsuccess http status code
            {
                var errmsg = $"Multifactor API host unreachable: {Configuration.Current.ApiUrl}. Reason: {wex.Message}";
                _logger.Error(errmsg);
                _logger.Error(wex.Message);
                throw new Exception($"{Constants.API_UNREACHABLE_CODE} {errmsg}", wex);
            }
            catch (Exception ex)
            {
                string errmsg = "Something went wrong";
                _logger.Error(ex.Message);
                if (ex.Message.Contains("UserNotRegistered"))
                {
                    throw new Exception($"{Constants.API_NOT_REGISTERED_CODE} {ex.Message}", ex);
                }
                if (ex.Message.Contains("Users quota exceeded"))
                {
                    throw new Exception($"{Constants.API_USERS_QUOTA_EXCEEDED_CODE} {ex.Message}", ex);
                }

                throw new Exception($"{errmsg}", ex);
            }
        }

        public MultiFactorAccessRequest CreateNonInteractiveAccessRequest(
            string methodPath,
            string identity,
            string email,
            string phone)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var url = $"{Configuration.Current.ApiUrl}{methodPath}";
            var payload = new
            {
                Identity = identity,
                Phone = phone,
                Email = email
            };

            var str = Util.JsonSerialize(payload);
            var requestData = Encoding.UTF8.GetBytes(str);
            var auth = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{Configuration.Current.ApiKey}:{Configuration.Current.ApiSecret}"));
            byte[] responseData = null;

            using (var web = new WebClient())
            {
                web.Headers.Add("Content-Type", "application/json");
                web.Headers.Add("Authorization", $"Basic {auth}");
                web.Headers.Add("mf-trace-id", _getTraceId());

                TryApplyProxy(web, Configuration.Current.ApiProxy);

                responseData = web.UploadData(url, "POST", requestData);
            }

            var responseJson = Encoding.UTF8.GetString(responseData);

            var response = Util.JsonDeserialize<MultiFactorWebResponseDto<MultiFactorAccessRequest>>(responseJson);

            if (!response.Success)
            {
                throw new Exception($"Got unsuccessful response from API: {responseJson}");
            }

            return response.Model;
        }

        internal static void TryApplyProxy(WebClient webClient, string proxyUrl)
        {
            if (webClient == null)
            {
                throw new ArgumentNullException(nameof(webClient));
            }

            if (!string.IsNullOrWhiteSpace(proxyUrl))
            {
                webClient.Proxy = new WebProxy(proxyUrl);
            }
        }

        public ScopeSupportInfoDto GetScopeSupportInfo()
        {
            try
            {
                var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Configuration.Current.ApiKey}:{Configuration.Current.ApiSecret}"));
                string responseData = null;

                using (var web = new WebClient())
                {
                    web.Headers.Add("Content-Type", "application/json");
                    web.Headers.Add("Authorization", $"Basic {auth}");
                    web.Headers.Add("mf-trace-id", _getTraceId());

                    TryApplyProxy(web, Configuration.Current.ApiProxy);

                    responseData = web.DownloadString($"{Configuration.Current.ApiUrl}/iis/support-info");
                }

                var responseDto = Util.JsonDeserialize<MultiFactorWebResponseDto<ScopeSupportInfoDto>>(responseData);
                return responseDto?.Model;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);

                throw new Exception($"{ex.Message}", ex);
            }
        }
    }

    public class MultiFactorAccessRequest
    {
        public string Id { get; set; }
        public string Identity { get; set; }
        public AccessRequestStatus Status { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Status of the MultiFactor access request.
    /// </summary>
    public enum AccessRequestStatus
    {
        Unknown = 0,
        Granted,
        Denied,
        Bypassed
    }
}