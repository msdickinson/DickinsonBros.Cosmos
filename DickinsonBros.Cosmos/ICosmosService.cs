using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DickinsonBros.Cosmos
{
    public interface ICosmosService<U>
    {
        Task<IEnumerable<T>> QueryAsync<T>(QueryDefinition queryDefinition, QueryRequestOptions queryRequestOptions);
        Task<ItemResponse<object>> DeleteAsync(string id, string key);
        Task<ItemResponse<T>> FetchAsync<T>(string id, string key);
        Task<ItemResponse<T>> InsertAsync<T>(string key, T value);
        Task<ItemResponse<T>> UpsertAsync<T>(string key, string eTag, T value);
    }
}