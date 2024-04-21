using System.Threading.Tasks;
using Supabase.Core.Interfaces;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Responses;

namespace Supabase.Postgrest.Interfaces
{
    /// <summary>
    /// Client interface for Postgrest
    /// </summary>
    public interface IPostgrestClient : IGettableHeaders
    {
        /// <summary>
        /// API Base Url for subsequent calls.
        /// </summary>
        string BaseUrl { get; }

        /// <summary>
        /// The Options <see cref="Client"/> was initialized with.
        /// </summary>
        ClientOptions Options { get; }

        /// <summary>
        /// Adds a handler that is called prior to a request being sent.
        /// </summary>
        /// <param name="handler"></param>
        void AddRequestPreparedHandler(OnRequestPreparedEventHandler handler);

        /// <summary>
        /// Removes an <see cref="OnRequestPreparedEventHandler"/> handler.
        /// </summary>
        /// <param name="handler"></param>
        void RemoveRequestPreparedHandler(OnRequestPreparedEventHandler handler);

        /// <summary>
        /// Clears all <see cref="OnRequestPreparedEventHandler"/> handlers.
        /// </summary>
        void ClearRequestPreparedHandlers();

        /// <summary>
        /// Adds a debug handler
        /// </summary>
        /// <param name="handler"></param>
        void AddDebugHandler(IPostgrestDebugger.DebugEventHandler handler);

        /// <summary>
        /// Removes a debug handler
        /// </summary>
        /// /// <param name="handler"></param>
        void RemoveDebugHandler(IPostgrestDebugger.DebugEventHandler handler);

        /// <summary>
        /// Clears debug handlers
        /// </summary>
        void ClearDebugHandlers();

        /// <summary>
        /// Perform a stored procedure call.
        /// </summary>
        /// <param name="procedureName">The function name to call</param>
        /// <param name="parameters">The parameters to pass to the function call</param>
        /// <returns></returns>
        Task<BaseResponse> Rpc(string procedureName, object? parameters);

        /// <summary>
        /// Perform a stored procedure call.
        /// </summary>
        /// <param name="procedureName">The function name to call</param>
        /// <param name="parameters">The parameters to pass to the function call</param>
        /// <typeparam name="TModeledResponse">A type used for hydrating the HTTP response content (hydration through JSON.NET)</typeparam>
        /// <returns>A hydrated model</returns>
        Task<TModeledResponse?> Rpc<TModeledResponse>(string procedureName, object? parameters = null);

        /// <summary>
        /// Returns a Table Query Builder instance for a defined model - representative of `USE $TABLE`
        /// </summary>
        /// <typeparam name="T">Custom Model derived from `BaseModel`</typeparam>
        /// <returns></returns>
        IPostgrestTable<T> Table<T>() where T : BaseModel, new();
        
        /// <summary>
        /// Returns a Table Query Builder instance with a Cache Provider for a defined model - representative of `USE #$TABLE`
        /// </summary>
        /// <param name="cacheProvider"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IPostgrestTableWithCache<T> Table<T>(IPostgrestCacheProvider cacheProvider) where T : BaseModel, new();
    }
}