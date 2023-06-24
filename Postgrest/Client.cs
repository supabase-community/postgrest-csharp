using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Postgrest.Interfaces;
using Postgrest.Models;
using Postgrest.Responses;
using Supabase.Core.Extensions;
namespace Postgrest
{
	/// <inheritdoc />
	public class Client : IPostgrestClient
	{
		/// <summary>
		/// Custom Serializer resolvers and converters that will be used for encoding and decoding Postgrest JSON responses.
		///
		/// By default, Postgrest seems to use a date format that C# and Newtonsoft do not like, so this initial
		/// configuration handles that.
		/// </summary>
		public static JsonSerializerSettings SerializerSettings(ClientOptions? options = null)
		{
			options ??= new ClientOptions();

			return new JsonSerializerSettings
			{
				ContractResolver = new PostgrestContractResolver(),
				Converters =
				{
					// 2020-08-28T12:01:54.763231
					new IsoDateTimeConverter
					{
						DateTimeStyles = options.DateTimeStyles,
						DateTimeFormat = ClientOptions.DATE_TIME_FORMAT
					}
				}
			};
		}


		/// <inheritdoc />
		public string BaseUrl { get; }

		/// <inheritdoc />
		public ClientOptions Options { get; }
		
		/// <inheritdoc />
		public void AddDebugHandler(IPostgrestDebugger.DebugEventHandler handler) =>
			Debugger.Instance.AddDebugHandler(handler);
		
		/// <inheritdoc />
		public void RemoveDebugHandler(IPostgrestDebugger.DebugEventHandler handler) =>
			Debugger.Instance.RemoveDebugHandler(handler);
		
		/// <inheritdoc />
		public void ClearDebugHandlers() => Debugger.Instance.ClearDebugHandlers();

		/// <summary>
		/// Function that can be set to return dynamic headers.
		/// 
		/// Headers specified in the constructor options will ALWAYS take precedence over headers returned by this function.
		/// </summary>
		public Func<Dictionary<string, string>>? GetHeaders { get; set; }

		/// <summary>
		/// Should be the first call to this class to initialize a connection with a Postgrest API Server
		/// </summary>
		/// <param name="baseUrl">Api Endpoint (ex: "http://localhost:8000"), no trailing slash required.</param>
		/// <param name="options">Optional client configuration.</param>
		/// <returns></returns>
		public Client(string baseUrl, ClientOptions? options = null)
		{
			BaseUrl = baseUrl;
			Options = options ?? new ClientOptions();
		}


		/// <inheritdoc />
		public IPostgrestTable<T> Table<T>() where T : BaseModel, new() =>
			new Table<T>(BaseUrl, SerializerSettings(Options), Options)
			{
				GetHeaders = GetHeaders
			};


		/// <inheritdoc />
		public Task<BaseResponse> Rpc(string procedureName, Dictionary<string, object>? parameters = null)
		{
			// Build Uri
			var builder = new UriBuilder($"{BaseUrl}/rpc/{procedureName}");

			var canonicalUri = builder.Uri.ToString();

			var serializerSettings = SerializerSettings(Options);

			// Prepare parameters
			Dictionary<string, string>? data = null;
			if (parameters != null)
				data = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(parameters, serializerSettings));

			// Prepare headers
			var headers = Helpers.PrepareRequestHeaders(HttpMethod.Post, new Dictionary<string, string>(Options.Headers), Options);

			if (GetHeaders != null)
				headers = GetHeaders().MergeLeft(headers);

			// Send request
			var request = Helpers.MakeRequest(Options, HttpMethod.Post, canonicalUri, serializerSettings, data, headers);
			return request;
		}
	}
}
