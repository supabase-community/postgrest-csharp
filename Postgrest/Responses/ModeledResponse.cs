using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Postgrest.Extensions;
using Postgrest.Models;

namespace Postgrest.Responses
{
    /// <summary>
    /// A representation of a successful Postgrest response that transforms the string response into a C# Modelled response.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ModeledResponse<T> : BaseResponse where T : BaseModel, new()
    {
        private JsonSerializerSettings SerializerSettings { get; set; }

        public List<T> Models { get; private set; } = new List<T>();

        public ModeledResponse(BaseResponse baseResponse, JsonSerializerSettings serializerSettings, bool shouldParse = true) : base(baseResponse.ClientOptions, baseResponse.ResponseMessage, baseResponse.Content)
        {
            SerializerSettings = serializerSettings;
            Content = baseResponse.Content;
            ResponseMessage = baseResponse.ResponseMessage;

            if (shouldParse && !string.IsNullOrEmpty(Content))
            {
                var token = JToken.Parse(Content!);

                if (token is JArray)
                {
                    var deserialized = JsonConvert.DeserializeObject<List<T>>(Content!, serializerSettings);

                    if (deserialized != null)
                        Models = deserialized;

                    if (Models != null)
                    {
                        foreach (var model in Models)
                        {
                            model.BaseUrl = baseResponse.ResponseMessage!.RequestMessage.RequestUri.GetBaseUrl();
                            model.RequestClientOptions = ClientOptions;
                        }
                    }
                }
                else if (token is JObject)
                {
                    Models.Clear();

                    T? obj = JsonConvert.DeserializeObject<T>(Content!, serializerSettings);

                    if (obj != null)
                    {
                        obj.BaseUrl = baseResponse.ResponseMessage!.RequestMessage.RequestUri.GetBaseUrl();
                        obj.RequestClientOptions = ClientOptions;

                        Models.Add(obj);
                    }
                }
            }
        }

    }
}
