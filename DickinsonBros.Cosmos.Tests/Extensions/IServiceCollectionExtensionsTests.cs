using DickinsonBros.Cosmos.Extensions;
using DickinsonBros.NoSQLService.Abstractions;
using Microsoft.Extensions.DependencyInjection;
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


        }
    }
}
