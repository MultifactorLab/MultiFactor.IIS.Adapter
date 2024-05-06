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

        public string CreateRequest(string identity, string rawUserName, string postbackUrl, ILdapProfile profile)
        {
            try
            {
                //make sure we can communicate securely
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                //payload
                var payload = Util.JsonSerialize(new
                {
                    Identity = identity,
                    profile.Phone,
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

                return response.Model.Url;
            }
            catch (Exception ex)
            {
                var errmsg = $"Multifactor API host unreachable: {Configuration.Current.ApiUrl}. Reason: {ex.Message}";
                _logger.Error(errmsg);
                if (ex.Message.Contains("UserNotRegistered"))
                {
                    throw new Exception($"{Constants.API_NOT_REGISTERED_CODE} {errmsg}", ex);
                }

                throw new Exception($"{Constants.API_UNREACHABLE_CODE} {errmsg}", ex);
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