using DickinsonBros.Cosmos.Configurators;
using DickinsonBros.Cosmos.Extensions;
using DickinsonBros.Cosmos.Models;
using DickinsonBros.NoSQL.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace DickinsonBros.Cosmos.Tests.Extensions
{
    [TestClass]
    public class IServiceCollectionExtensionsTests
    {
        [TestMethod]
        public void AddCosmosService_Should_Succeed()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            // Act
            serviceCollection.AddCosmosService();

            // Assert

            Assert.IsTrue(serviceCollection.Any(serviceDefinition => serviceDefinition.ServiceType == typeof(INoSQLService) &&
                                           serviceDefinition.ImplementationType == typeof(CosmosService) &&
                                           serviceDefinition.Lifetime == ServiceLifetime.Singleton));

            Assert.IsTrue(serviceCollection.Any(serviceDefinition => serviceDefinition.ServiceType == typeof(IConfigureOptions<CosmosServiceOptions>) &&
                                           serviceDefinition.ImplementationType == typeof(CosmosServiceOptionsConfigurator) &&
                                           serviceDefinition.Lifetime == ServiceLifetime.Singleton));
        }
    }
}
