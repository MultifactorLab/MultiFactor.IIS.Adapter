using MultiFactor.IIS.Adapter.Dto;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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

        public string CreateRequest(string identity, string rawUserName, string postbackUrl, string userPhone)
        {
            try
            {
                //make sure we can communicate securely
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                //payload
                var payload = Util.JsonSerialize(new
                {
                    Identity = identity,
                    userPhone,
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

                    if (!string.IsNullOrEmpty(Configuration.Current.ApiProxy))
                    {
                        web.Proxy = new WebProxy(Configuration.Current.ApiProxy);
                    }

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

        public async Task<ScopeSupportInfoDto> GetScopeSupportInfoAsync()
        {
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Configuration.Current.ApiKey}:{Configuration.Current.ApiSecret}"));
            WebProxy proxy = !string.IsNullOrEmpty(Configuration.Current.ApiProxy) ? new WebProxy(Configuration.Current.ApiProxy) : null;
            var httpClientHandler = new HttpClientHandler()
            {
                Proxy = proxy,
                UseProxy = proxy != null
            };
            using (var client = new HttpClient(httpClientHandler))
            {
                client.BaseAddress = new Uri(Configuration.Current.ApiUrl);
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {auth}");
                client.DefaultRequestHeaders.Add("mf-trace-id", _getTraceId());

                var response = await client.GetAsync("self-service/support-info");
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                var responseDto = Util.JsonDeserialize<ScopeSupportInfoDto> (responseBody);
                return responseDto;
            }
        }
    }
}