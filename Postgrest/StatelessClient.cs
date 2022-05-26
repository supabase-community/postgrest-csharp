using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Postgrest.Attributes;
using Postgrest.Models;
using Postgrest.Responses;

namespace Postgrest
{
    /// <summary>
    /// A StatelessClient that allows one-off API interactions.
    /// </summary>
    public static class StatelessClient
    {
        /// <summary>
        /// Custom Serializer resolvers and converters that will be used for encoding and decoding Postgrest JSON responses.
        ///
        /// By default, Postgrest seems to use a date format that C# and Newtonsoft do not like, so this initial
        /// configuration handles that.
        /// </summary>
        internal static JsonSerializerSettings SerializerSettings(ClientOptions options = null)
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
                        DateTimeFormat = ClientOptions.DateTimeFormat
                    }
                }
            };
        }

        /// <summary>
        /// Returns a Table Query Builder instance for a defined model - representative of `USE $TABLE`
        /// </summary>
        /// <typeparam name="T">Custom Model derived from `BaseModel`</typeparam>
        /// <returns></returns>
        public static Table<T> Table<T>(StatelessClientOptions options) where T : BaseModel, new() =>
            new Table<T>(options.BaseUrl, options, SerializerSettings(options));

        /// <summary>
        /// Perform a stored procedure call.
        /// </summary>
        /// <param name="procedureName">The function name to call</param>
        /// <param name="parameters">The parameters to pass to the function call</param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static Task<BaseResponse> Rpc(
            string procedureName,
            Dictionary<string, object> parameters,
            StatelessClientOptions options)
        {
            // Build Uri
            var builder = new UriBuilder($"{options.BaseUrl}/rpc/{procedureName}");

            var canonicalUri = builder.Uri.ToString();

            var serializerSettings = SerializerSettings(options);

            // Prepare parameters
            var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                JsonConvert.SerializeObject(parameters, serializerSettings));

            // Prepare headers
            var headers = Helpers.PrepareRequestHeaders(HttpMethod.Post,
                new Dictionary<string, string>(options.Headers), options);

            // Send request
            var request = Helpers.MakeRequest(
                HttpMethod.Post,
                canonicalUri,
                serializerSettings,
                data,
                headers);

            return request;
        }
    }
}