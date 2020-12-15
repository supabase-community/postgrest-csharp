using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Postgrest.Attributes;
using Postgrest.Responses;

namespace Postgrest
{
    public static class Helpers
    {
        public static T GetPropertyValue<T>(object obj, string propName) => (T)obj.GetType().GetProperty(propName).GetValue(obj, null);
        public static T GetCustomAttribute<T>(object obj) where T : Attribute => (T)Attribute.GetCustomAttribute(obj.GetType(), typeof(T));
        public static T GetCustomAttribute<T>(Type type) where T : Attribute => (T)Attribute.GetCustomAttribute(type, typeof(T));

        private static readonly HttpClient client = new HttpClient();

        /// <summary>
        /// Helper to make a request using the defined parameters to an API Endpoint and coerce into a model. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="method"></param>
        /// <param name="url"></param>
        /// <param name="reqParams"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public static async Task<ModeledResponse<T>> MakeRequest<T>(HttpMethod method, string url, object data = null, Dictionary<string, string> headers = null)
        {
            var baseResponse = await MakeRequest(method, url, data, headers);
            return new ModeledResponse<T>(baseResponse);
        }

        /// <summary>
        /// Helper to make a request using the defined parameters to an API Endpoint.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="url"></param>
        /// <param name="reqParams"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public static async Task<BaseResponse> MakeRequest(HttpMethod method, string url, object data = null, Dictionary<string, string> headers = null)
        {
            try
            {
                var builder = new UriBuilder(url);
                var query = HttpUtility.ParseQueryString(builder.Query);

                if (data != null && method == HttpMethod.Get)
                {
                    // Case if it's a Get request the data object is a dictionary<string,string>
                    if(data is Dictionary<string,string> reqParams)
                    {
                        foreach (var param in reqParams)
                            query[param.Key] = param.Value;
                    }
                    
                }

                builder.Query = query.ToString();

                var requestMessage = new HttpRequestMessage(method, builder.Uri);

                if (data != null && method != HttpMethod.Get)
                {
                    requestMessage.Content = new StringContent(JsonConvert.SerializeObject(data, Client.Instance.SerializerSettings), Encoding.UTF8, "application/json");
                }

                if (headers != null)
                {
                    foreach (var kvp in headers)
                    {
                        requestMessage.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                    }
                }

                var response = await client.SendAsync(requestMessage);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    ErrorResponse obj = null;

                    try
                    {
                        obj = JsonConvert.DeserializeObject<ErrorResponse>(content);
                    }
                    catch (JsonSerializationException)
                    {
                        obj = new ErrorResponse { Message = "Invalid or Empty response received. Are you trying to update or delete a record that does not exist?" };
                    }

                    obj.Content = content;
                    throw new RequestException(response, obj);
                }
                else
                {
                    return new BaseResponse { Content = content, ResponseMessage = response };
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                throw e;
            }
        }

        /// <summary>
        /// Prepares the request with appropriate HTTP headers expected by Postgrest.
        /// </summary>
        /// <param name="headers"></param>
        /// <returns></returns>
        public static Dictionary<string, string> PrepareRequestHeaders(HttpMethod method, Dictionary<string, string> headers = null, ClientAuthorization authorization = null, ClientOptions options = null, int rangeFrom = int.MinValue, int rangeTo = int.MinValue)
        {
            if (headers == null)
                headers = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(options?.Schema))
            {
                if (method == HttpMethod.Get)
                    headers.Add("Accept-Profile", options.Schema);
                else
                    headers.Add("Content-Profile", options.Schema);
            }

            if (authorization != null)
            {
                switch (authorization.Type)
                {
                    case ClientAuthorization.AuthorizationType.ApiKey:
                        headers.Add("apikey", authorization.ApiKey);
                        break;
                    case ClientAuthorization.AuthorizationType.Token:
                        headers.Add("Authorization", $"Bearer {authorization.Token}");
                        break;
                    case ClientAuthorization.AuthorizationType.Basic:
                        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{authorization.Username}:{authorization.Password}"));
                        headers.Add("Authorization", $"Basic {header}");
                        break;
                }
            }

            if (rangeFrom != int.MinValue)
            {
                headers.Add("Range-Unit", "items");
                headers.Add("Range", $"{rangeFrom}-{(rangeTo != int.MinValue ? rangeTo.ToString() : null)}");
            }

            return headers;
        }
    }

    public class RequestException : Exception
    {
        public HttpResponseMessage Response { get; private set; }
        public ErrorResponse Error { get; private set; }

        public RequestException(HttpResponseMessage response, ErrorResponse error) : base(error.Message)
        {
            Response = response;
            Error = error;
        }
    }
}
