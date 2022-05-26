using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Postgrest.Responses
{
    /// <summary>
    /// A representation of a successful Postgrest response that transforms the string response into a C# Modelled response.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ModeledResponse<T> : BaseResponse
    {
        private JsonSerializerSettings SerializerSettings { get; set; }

        public List<T> Models { get; private set; } = new List<T>();

        public ModeledResponse(BaseResponse baseResponse, JsonSerializerSettings serializerSettings, bool shouldParse = true)
        {
            SerializerSettings = serializerSettings;
            Content = baseResponse.Content;
            ResponseMessage = baseResponse.ResponseMessage;

            if (shouldParse && !string.IsNullOrEmpty(Content))
            {
                var token = JToken.Parse(Content);

                if (token is JArray)
                {
                    Models = JsonConvert.DeserializeObject<List<T>>(Content, serializerSettings);
                }
                else if (token is JObject)
                {
                    Models.Clear();
                    T obj = JsonConvert.DeserializeObject<T>(Content, serializerSettings);
                    Models.Add(obj);
                }
            }
        }

    }
}
