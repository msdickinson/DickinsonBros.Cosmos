using DickinsonBros.Cosmos.Configurators;
using DickinsonBros.Cosmos.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;

namespace DickinsonBros.Cosmos.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddCosmosService<T>(this IServiceCollection serviceCollection)
        where T : CosmosServiceOptions, new()
        {
            serviceCollection.TryAddSingleton<ICosmosService<T>, CosmosService<T>>();
            serviceCollection.TryAddSingleton<ICosmosFactory, CosmosFactory>();
            serviceCollection.TryAddSingleton<IConfigureOptions<T>, CosmosServiceOptionsConfigurator<T>>();

            return serviceCollection;
        }
    }
}
