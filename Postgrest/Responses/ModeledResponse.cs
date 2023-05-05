using System;
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
		public List<T> Models { get; } = new();

		public ModeledResponse(BaseResponse baseResponse, JsonSerializerSettings serializerSettings, Func<Dictionary<string, string>>? getHeaders = null, bool shouldParse = true) : base(baseResponse.ClientOptions, baseResponse.ResponseMessage, baseResponse.Content)
		{
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

					foreach (var model in Models)
					{
						model.BaseUrl = baseResponse.ResponseMessage!.RequestMessage.RequestUri.GetInstanceUrl().Replace(model.TableName, "").TrimEnd('/');
						model.RequestClientOptions = ClientOptions;
						model.GetHeaders = getHeaders;
					}
				}
				else if (token is JObject)
				{
					Models.Clear();

					T? obj = JsonConvert.DeserializeObject<T>(Content!, serializerSettings);

					if (obj != null)
					{
						obj.BaseUrl = baseResponse.ResponseMessage!.RequestMessage.RequestUri.GetInstanceUrl().Replace(obj.TableName, "").TrimEnd('/');
						obj.RequestClientOptions = ClientOptions;
						obj.GetHeaders = getHeaders;

						Models.Add(obj);
					}
				}
			}
		}

	}
}
