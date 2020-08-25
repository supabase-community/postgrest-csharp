using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Postgrest.Responses;

namespace Postgrest
{
    public static class Helpers
    {
        public static T GetPropertyValue<T>(object obj, string propName) => (T)obj.GetType().GetProperty(propName).GetValue(obj, null);

        public static T GetCustomAttribute<T>(object obj) where T : Attribute => (T)Attribute.GetCustomAttribute(obj.GetType(), typeof(T));

        private static readonly HttpClient client = new HttpClient();

        public static async Task<ModeledResponse<T>> MakeRequest<T>(HttpMethod method, string url, Dictionary<string, string> reqParams = null, Dictionary<string, string> headers = null)
        {
            var baseResponse = await MakeRequest(method, url, reqParams, headers);
            return new ModeledResponse<T>(baseResponse);
        }

        public static async Task<BaseResponse> MakeRequest(HttpMethod method, string url, Dictionary<string, string> reqParams = null, Dictionary<string, string> headers = null)
        {
            try
            {
                var builder = new UriBuilder(url);
                var query = HttpUtility.ParseQueryString(builder.Query);
                builder.Port = -1;

                if (reqParams != null && method == HttpMethod.Get)
                {
                    foreach (var param in reqParams)
                        query[param.Key] = param.Value;
                }

                builder.Query = query.ToString();

                var requestMessage = new HttpRequestMessage(method, builder.Uri);

                if (reqParams != null && method != HttpMethod.Get)
                {
                    requestMessage.Content = new StringContent(JsonConvert.SerializeObject(reqParams), Encoding.UTF8, "application/json");
                }

                if (headers != null)
                {
                    foreach (var kvp in headers)
                    {
                        requestMessage.Headers.Add(kvp.Key, kvp.Value);
                    }
                }

                var response = await client.SendAsync(requestMessage);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var obj = JsonConvert.DeserializeObject<ErrorResponse>(content);
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
