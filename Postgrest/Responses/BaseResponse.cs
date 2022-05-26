using System.Net.Http;
using Newtonsoft.Json;

namespace Postgrest.Responses
{
    /// <summary>
    /// A wrapper class from which all Responses derive.
    /// </summary>
    public class BaseResponse
    {
        [JsonIgnore]
        public HttpResponseMessage ResponseMessage { get; set; }

        [JsonIgnore]
        public string Content { get; set; }
    }
}
