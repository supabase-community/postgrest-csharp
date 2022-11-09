using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Postgrest.Interfaces;
using Postgrest.Models;
using Postgrest.Responses;

namespace Postgrest
{
    /// <summary>
    /// A a single, reusable connection to a Postgrest endpoint.
    /// </summary>
    public class Client : IPostgrestClient
    {
        /// <summary>
        /// API Base Url for subsequent calls.
        /// </summary>
        public string BaseUrl { get; private set; }

        /// <summary>
        /// The Options <see cref="Client"/> was initialized with.
        /// </summary>
        public ClientOptions Options { get; private set; }


        /// <summary>
        /// Should be the first call to this class to initialize a connection with a Postgrest API Server
        /// </summary>
        /// <param name="baseUrl">Api Endpoint (ex: "http://localhost:8000"), no trailing slash required.</param>
        /// <param name="options">Optional client configuration.</param>
        /// <returns></returns>
        public Client(string baseUrl, ClientOptions? options = null)
        {
            BaseUrl = baseUrl;

            options ??= new ClientOptions();

            Options = options;
        }

        /// <summary>
        /// Returns a Table Query Builder instance for a defined model - representative of `USE $TABLE`
        /// </summary>
        /// <typeparam name="T">Custom Model derived from `BaseModel`</typeparam>
        /// <returns></returns>
        public IPostgrestTable<T> Table<T>() where T : BaseModel, new() =>
            new Table<T>(BaseUrl, Options, StatelessClient.SerializerSettings(Options));

        /// <summary>
        /// Perform a stored procedure call.
        /// </summary>
        /// <param name="procedureName">The function name to call</param>
        /// <param name="parameters">The parameters to pass to the function call</param>
        /// <returns></returns>
        public Task<BaseResponse> Rpc(string procedureName, Dictionary<string, object> parameters)
        {
            // Build Uri
            var builder = new UriBuilder($"{BaseUrl}/rpc/{procedureName}");

            var canonicalUri = builder.Uri.ToString();

            var serializerSettings = StatelessClient.SerializerSettings(Options);

            // Prepare parameters
            var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                JsonConvert.SerializeObject(parameters, serializerSettings));

            // Prepare headers
            var headers = Helpers.PrepareRequestHeaders(HttpMethod.Post,
                new Dictionary<string, string>(Options.Headers), Options);

            // Send request
            var request = Helpers.MakeRequest(Options, HttpMethod.Post, canonicalUri, serializerSettings, data, headers);
            return request;
        }
    }
}