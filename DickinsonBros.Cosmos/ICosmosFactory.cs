using DickinsonBros.Cosmos.Models;
using Microsoft.Azure.Cosmos;

namespace DickinsonBros.Cosmos
{
    public interface ICosmosFactory
    {
        CosmosClient CreateCosmosClient(CosmosServiceOptions cosmosServiceOptions);
        Container GetContainer(CosmosClient cosmosClient, CosmosServiceOptions value);
    }
}