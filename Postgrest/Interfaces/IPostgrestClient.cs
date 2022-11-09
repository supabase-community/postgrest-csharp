using Postgrest.Models;
using Postgrest.Responses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Postgrest.Interfaces
{
    public interface IPostgrestClient
    {
        string BaseUrl { get; }
        ClientOptions Options { get; }

        Task<BaseResponse> Rpc(string procedureName, Dictionary<string, object> parameters);
        IPostgrestTable<T> Table<T>() where T : BaseModel, new();
    }
}