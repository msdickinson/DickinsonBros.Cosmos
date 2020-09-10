using DickinsonBros.CosmosService.Models;
using DickinsonBros.CosmosService.Runner.Models;
using DickinsonBros.Encryption.Certificate.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DickinsonBros.CosmosService.Runner.Services
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
            using var scope = _serviceScopeFactory.CreateScope();
            var provider = scope.ServiceProvider;
            var configuration = provider.GetRequiredService<IConfiguration>();
            var certificateEncryptionService = provider.GetRequiredService<ICertificateEncryptionService<RunnerCertificateEncryptionServiceOptions>>();
            var cosmosServiceOptions = configuration.GetSection(nameof(CosmosServiceOptions)).Get<CosmosServiceOptions>();
         
            configuration.Bind($"{nameof(CosmosServiceOptions)}", options);

        options.ConnectionString = certificateEncryptionService.Decrypt(cosmosServiceOptions.ConnectionString);
            options.ContainerId = cosmosServiceOptions.ContainerId;
            options.DatabaseId = cosmosServiceOptions.DatabaseId;
            options.EndpointUri = cosmosServiceOptions.EndpointUri;
            options.PrimaryKey = certificateEncryptionService.Decrypt(cosmosServiceOptions.PrimaryKey);
        }
    }
}
