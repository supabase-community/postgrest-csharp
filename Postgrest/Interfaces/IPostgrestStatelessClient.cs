using Postgrest.Models;
using Postgrest.Responses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Postgrest.Interfaces
{
    public interface IPostgrestStatelessClient
    {
        Task<BaseResponse> Rpc(string procedureName, Dictionary<string, object> parameters, StatelessClientOptions options);
        IPostgrestTable<T> Table<T>(StatelessClientOptions options) where T : BaseModel, new();
    }
}