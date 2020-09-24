using DickinsonBros.Cosmos.Configurators;
using DickinsonBros.Cosmos.Models;
using DickinsonBros.Encryption.Certificate.Abstractions;
using DickinsonBros.Test;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;

namespace DickinsonBros.Cosmos.Tests.Configurators
{
    [TestClass]
    public class CosmosServiceOptionsConfiguratorTests : BaseTest
    {

        [TestMethod]
        public async Task Configure_Runs_ConfigReturns()
        {
            var cosmosServiceOptions = new CosmosServiceOptions
            {
                ConnectionString = "SampleConnectionString",
                ContainerId = "SampleContainerId",
                DatabaseId = "SampleDatabaseId",
                EndpointUri = "SampleEndpointUri",
                PrimaryKey = "SamplePrimaryKey"
            };

            var cosmosServiceOptionsDecrypted = new CosmosServiceOptions
            {
                ConnectionString = "SampleDecryptedConnectionString",
                PrimaryKey = "SampleDecryptedPrimaryKey"
            };

            var configurationRoot = BuildConfigurationRoot(cosmosServiceOptions);

            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var configurationEncryptionServiceMock = serviceProvider.GetMock<IConfigurationEncryptionService>();

                    configurationEncryptionServiceMock
                    .Setup
                    (
                        configurationEncryptionService => configurationEncryptionService.Decrypt
                        (
                            cosmosServiceOptions.ConnectionString
                        )
                    )
                    .Returns
                    (
                            cosmosServiceOptionsDecrypted.ConnectionString
                    );

                    configurationEncryptionServiceMock
                    .Setup
                    (
                        configurationEncryptionService => configurationEncryptionService.Decrypt
                        (
                            cosmosServiceOptions.PrimaryKey
                        )
                    )
                    .Returns
                    (
                            cosmosServiceOptionsDecrypted.PrimaryKey
                    );

                    //Act
                    var options = serviceProvider.GetRequiredService<IOptions<CosmosServiceOptions>>().Value;

                    //Assert
                    Assert.IsNotNull(options);

                    Assert.AreEqual(cosmosServiceOptionsDecrypted.ConnectionString, options.ConnectionString);
                    Assert.AreEqual(cosmosServiceOptions.ContainerId, options.ContainerId);
                    Assert.AreEqual(cosmosServiceOptions.DatabaseId, options.DatabaseId);
                    Assert.AreEqual(cosmosServiceOptions.EndpointUri, options.EndpointUri);
                    Assert.AreEqual(cosmosServiceOptionsDecrypted.PrimaryKey, options.PrimaryKey);

                    await Task.CompletedTask.ConfigureAwait(false);

                },
                serviceCollection => ConfigureServices(serviceCollection, configurationRoot)
            );
        }

        #region Helpers

        private IServiceCollection ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddOptions();
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            serviceCollection.AddSingleton<IConfigureOptions<CosmosServiceOptions>, CosmosServiceOptionsConfigurator>();
            serviceCollection.AddSingleton(Mock.Of<IConfigurationEncryptionService>());

            return serviceCollection;
        }

        #endregion
    }
}
