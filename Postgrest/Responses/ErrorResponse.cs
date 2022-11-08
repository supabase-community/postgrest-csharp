using Newtonsoft.Json;
using System.Net.Http;

namespace Postgrest.Responses
{
    /// <summary>
    /// A representation of Postgrest's API error response.
    /// </summary>
    public class ErrorResponse : BaseResponse
    {
        public ErrorResponse(ClientOptions clientOptions, HttpResponseMessage? responseMessage, string? content) : base(clientOptions, responseMessage, content)
        { }

        [JsonProperty("hint")]
        public object? Hint { get; set; }

        [JsonProperty("details")]
        public object? Details { get; set; }

        [JsonProperty("code")]
        public string? Code { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }
    }
}
