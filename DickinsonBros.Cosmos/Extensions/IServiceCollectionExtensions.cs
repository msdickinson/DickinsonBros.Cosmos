using DickinsonBros.Cosmos.Configurators;
using DickinsonBros.Cosmos.Models;
using DickinsonBros.NoSQLService.Abstractions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace DickinsonBros.Cosmos.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddCosmosService(this IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddSingleton<INoSQLService, CosmosService>();
            serviceCollection.TryAddSingleton<IConfigureOptions<CosmosServiceOptions>, CosmosServiceOptionsConfigurator>();

            serviceCollection.AddSingleton((provider) =>
            {
                var cosmosServiceOptions = provider.GetService<IOptions<CosmosServiceOptions>>().Value;
                return new CosmosClient(cosmosServiceOptions.ConnectionString);
            });

            return serviceCollection;
        }
    }
}
