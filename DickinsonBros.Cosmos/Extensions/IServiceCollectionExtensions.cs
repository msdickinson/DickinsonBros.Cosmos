using DickinsonBros.Cosmos.Configurators;
using DickinsonBros.Cosmos.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;

namespace DickinsonBros.Cosmos.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddCosmosService(this IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddSingleton<ICosmosService, CosmosService>();
            serviceCollection.TryAddSingleton<IConfigureOptions<CosmosServiceOptions>, CosmosServiceOptionsConfigurator>();

            serviceCollection.AddSingleton((provider) =>
            {
                var cosmosServiceOptions = provider.GetService<IOptions<CosmosServiceOptions>>().Value;
                return new CosmosClient(cosmosServiceOptions.ConnectionString, new CosmosClientOptions { SerializerOptions = new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase } });
            });

            return serviceCollection;
        }
    }
}
