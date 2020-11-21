using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Postgrest.Attributes;

namespace Postgrest.Responses
{
    /// <summary>
    /// A representation of a successful Postgrest response that transforms the string response into a C# Modelled response.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ModeledResponse<T> : BaseResponse
    {
        public List<T> Models { get; private set; } = new List<T>();

        public ModeledResponse(BaseResponse baseResponse, bool shouldParse = true)
        {
            Content = baseResponse.Content;
            ResponseMessage = baseResponse.ResponseMessage;

            if (shouldParse)
            {
                var token = JToken.Parse(Content);

                if (token is JArray)
                {
                    Models = JsonConvert.DeserializeObject<List<T>>(Content, Client.Instance.SerializerSettings);
                }
                else if (token is JObject)
                {
                    Models.Clear();
                    T obj = JsonConvert.DeserializeObject<T>(Content, Client.Instance.SerializerSettings);
                    Models.Add(obj);
                }
            }
        }

    }
}
