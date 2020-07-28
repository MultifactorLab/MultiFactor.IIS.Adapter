using System;
using System.Net;
using System.Text;

namespace MultiFactor.IIS.Adapter.Services
{
    /// <summary>
    /// Service to interact with MultiFactor API
    /// </summary>
    public class MultiFactorApiClient
    {
        public string CreateRequest(string login, string postbackUrl)
        {
            try
            {
                //make sure we can communicate securely
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                //payload
                var json = Util.JsonSerialize(new
                {
                    Identity = login,
                    Callback = new
                    {
                        Action = postbackUrl,
                        Target = "_self"
                    },
                });

                var requestData = Encoding.UTF8.GetBytes(json);
                byte[] responseData = null;

                //basic authorization
                var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(Configuration.Current.ApiKey + ":" + Configuration.Current.ApiSecret));


                using (var web = new WebClient())
                {
                    web.Headers.Add("Content-Type", "application/json");
                    web.Headers.Add("Authorization", "Basic " + auth);
                    responseData = web.UploadData(Configuration.Current.ApiUrl + "/access/requests", "POST", requestData);
                }

                json = Encoding.UTF8.GetString(responseData);


                var response = Util.JsonDeserialize<MultiFactorWebResponse<MultiFactorAccessPage>>(json);

                if (!response.Success)
                {
                    throw new Exception(response.Message);
                }

                return response.Model.Url;
            }
            catch (Exception ex)
            {

            }

            return null;
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