using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Supabase.Postgrest.Interfaces;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Requests;
using Supabase.Postgrest.Responses;

namespace Supabase.Postgrest
{
    /// <summary>
    /// Represents a table constructed with a <see cref="IPostgrestCacheProvider"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TableWithCache<T> : Table<T>, IPostgrestTableWithCache<T> where T : BaseModel, new()
    {
        /// <summary>
        /// Represents a caching provider to be used with Get Requests.
        /// </summary>
        protected IPostgrestCacheProvider CacheProvider { get; }

        /// <inheritdoc />
        public TableWithCache(string baseUrl, IPostgrestCacheProvider cacheProvider,
            JsonSerializerOptions serializerOptions, ClientOptions? options = null)
            : base(baseUrl, serializerOptions, options)
        {
            CacheProvider = cacheProvider;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public new async Task<CacheBackedRequest<T>> Get(CancellationToken cancellationToken = default)
        {
            var action = new Func<Task<ModeledResponse<T>>>(() => base.Get(cancellationToken));

            var cacheModel = new CacheBackedRequest<T>(this, CacheProvider, action);
            await cacheModel.TryLoadFromCache();

            cacheModel.Invoke();

            return cacheModel;
        }
    }
}
