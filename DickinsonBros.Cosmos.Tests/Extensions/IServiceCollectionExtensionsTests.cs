using DickinsonBros.Cosmos.Configurators;
using DickinsonBros.Cosmos.Extensions;
using DickinsonBros.Cosmos.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace DickinsonBros.Cosmos.Tests.Extensions
{
    [TestClass]
    public class IServiceCollectionExtensionsTests
    {
        class SampleCosmosServiceOptions : CosmosServiceOptions
        { }
        [TestMethod]
        public void AddCosmosService_Should_Succeed()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            // Act
            serviceCollection.AddCosmosService<SampleCosmosServiceOptions>();

            // Assert

            Assert.IsTrue(serviceCollection.Any(serviceDefinition => serviceDefinition.ServiceType == typeof(ICosmosService<SampleCosmosServiceOptions>) &&
                                           serviceDefinition.ImplementationType == typeof(CosmosService<SampleCosmosServiceOptions>) &&
                                           serviceDefinition.Lifetime == ServiceLifetime.Singleton));

            Assert.IsTrue(serviceCollection.Any(serviceDefinition => serviceDefinition.ServiceType == typeof(IConfigureOptions<SampleCosmosServiceOptions>) &&
                                           serviceDefinition.ImplementationType == typeof(CosmosServiceOptionsConfigurator<SampleCosmosServiceOptions>) &&
                                           serviceDefinition.Lifetime == ServiceLifetime.Singleton));
        }
    }
}
