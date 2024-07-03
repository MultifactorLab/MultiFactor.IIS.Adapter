using MultiFactor.IIS.Adapter.Services.Ldap.Profile;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace MultiFactor.IIS.Adapter.Services
{
    /// <summary>
    /// Service to interact with MultiFactor API
    /// </summary>
    public class MultiFactorApiClient
    {
        private readonly Logger _logger;

        public MultiFactorApiClient(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

                    if (!string.IsNullOrEmpty(Configuration.Current.ApiProxy))
                    {
                        web.Proxy = new WebProxy(Configuration.Current.ApiProxy);
                    }

                    responseData = web.UploadData($"{Configuration.Current.ApiUrl}/access/requests", "POST", requestData);
                }

                var responseJson = Encoding.UTF8.GetString(responseData);
                var response = Util.JsonDeserialize<MultiFactorWebResponse<MultiFactorAccessPage>>(responseJson);
                if (!response.Success)
                {
                    _logger.Error($"Got unsuccessful response from API: {responseJson}");
                    throw new Exception(response.Message);
                }
                _logger.Info($"Succesfully get url {response.Model.Url} for {identity}");
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
                //var errmsg = $"Multifactor API host unreachable: {Configuration.Current.ApiUrl}. Reason: {ex.Message}";
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
    }

    public class MultiFactorWebResponse<TModel>
    {
        public bool Success { get; set; }

        public string Message { get; set; }

        public TModel Model { get; set; }
    }

    public class MultiFactorAccessPage
    {
        public string Url { get; set; }
    }
}