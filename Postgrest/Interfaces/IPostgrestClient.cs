using Postgrest.Models;
using Postgrest.Responses;
using Supabase.Core.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Postgrest.Interfaces;

public interface IPostgrestClient : IGettableHeaders
{
    string BaseUrl { get; }
    ClientOptions Options { get; }
    
    void AddDebugHandler(IPostgrestDebugger.DebugEventHandler handler);
    void RemoveDebugHandler(IPostgrestDebugger.DebugEventHandler handler);
    void ClearDebugHandlers();

    Task<BaseResponse> Rpc(string procedureName, Dictionary<string, object> parameters);
    IPostgrestTable<T> Table<T>() where T : BaseModel, new();
}