using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Supabase.Postgrest.Extensions;
using Supabase.Postgrest.Models;

namespace Supabase.Postgrest.Responses
{
	/// <summary>
	/// A representation of a successful Postgrest response that transforms the string response into a C# Modelled response.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ModeledResponse<T> : BaseResponse where T : BaseModel, new()
	{
		/// <summary>
		/// The first model in the response.
		/// </summary>
		public T? Model => Models.FirstOrDefault();

		/// <summary>
		/// A list of models in the response.
		/// </summary>
		public List<T> Models { get; } = new();

		/// <inheritdoc />
		public ModeledResponse(BaseResponse baseResponse, JsonSerializerOptions serializerOptions, Func<Dictionary<string, string>>? getHeaders = null, bool shouldParse = true) : base(baseResponse.ClientOptions, baseResponse.ResponseMessage, baseResponse.Content)
		{
			Content = baseResponse.Content;
			ResponseMessage = baseResponse.ResponseMessage;

			if (!shouldParse || string.IsNullOrEmpty(Content)) return;

			var jsonDocument = JsonDocument.Parse(Content!);

			switch (jsonDocument.RootElement.ValueKind)
			{
				// A List of models has been returned
				case JsonValueKind.Array:
					{
						// TODO: This deserialization is not working as expected
						// datetime fields end up as null
						var deserialized = JsonSerializer.Deserialize<List<T>>(Content!, serializerOptions);

						if (deserialized != null)
							Models = deserialized;

						foreach (var model in Models)
						{
							model.BaseUrl = baseResponse.ResponseMessage!.RequestMessage.RequestUri.GetInstanceUrl().Replace(model.TableName, "").TrimEnd('/');
							model.RequestClientOptions = ClientOptions;
							model.GetHeaders = getHeaders;
						}

						break;
					}
				// A single model has been returned
				case JsonValueKind.Object:
					{
						Models.Clear();

						var obj = JsonSerializer.Deserialize<T>(Content!, serializerOptions);

						if (obj != null)
						{
							obj.BaseUrl = baseResponse.ResponseMessage!.RequestMessage.RequestUri.GetInstanceUrl().Replace(obj.TableName, "").TrimEnd('/');
							obj.RequestClientOptions = ClientOptions;
							obj.GetHeaders = getHeaders;

							Models.Add(obj);
						}

						break;
					}
			}

			Debugger.Instance.Log(this, $"Response: [{baseResponse.ResponseMessage?.StatusCode}]\n" + $"Parsed Models <{typeof(T).Name}>:\n\t{JsonSerializer.Serialize(Models, serializerOptions)}\n");
		}
	}
}
