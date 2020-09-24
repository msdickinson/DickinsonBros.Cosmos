using DickinsonBros.Cosmos.Models;
using DickinsonBros.Encryption.Certificate.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DickinsonBros.Cosmos.Configurators
{
    public class CosmosServiceOptionsConfigurator : IConfigureOptions<CosmosServiceOptions>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        public CosmosServiceOptionsConfigurator(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        void IConfigureOptions<CosmosServiceOptions>.Configure(CosmosServiceOptions options)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var provider = scope.ServiceProvider;
                var configuration = provider.GetRequiredService<IConfiguration>();
                var configurationEncryptionService = provider.GetRequiredService<IConfigurationEncryptionService>();
                var telemetryServiceOptions = configuration.GetSection(nameof(CosmosServiceOptions)).Get<CosmosServiceOptions>();

                configuration.Bind($"{nameof(CosmosServiceOptions)}", options);

                options.ConnectionString = configurationEncryptionService.Decrypt(telemetryServiceOptions.ConnectionString);
                options.PrimaryKey = configurationEncryptionService.Decrypt(telemetryServiceOptions.PrimaryKey);
            }
        }
    }
}
