using DickinsonBros.Cosmos.Runner.Models;
using DickinsonBros.Encryption.Certificate.Abstractions;
using DickinsonBros.Telemetry.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace DickinsonBros.Cosmos.Runner.Services
{
    public class TelemetryServiceOptionsConfigurator : IConfigureOptions<TelemetryServiceOptions>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        public TelemetryServiceOptionsConfigurator(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }
        void IConfigureOptions<TelemetryServiceOptions>.Configure(TelemetryServiceOptions options)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var provider = scope.ServiceProvider;
            var configuration = provider.GetRequiredService<IConfiguration>();
            var certificateEncryptionService = provider.GetRequiredService<ICertificateEncryptionService<RunnerCertificateEncryptionServiceOptions>>();
            var telemetryServiceOptions = configuration.GetSection(nameof(TelemetryServiceOptions)).Get<TelemetryServiceOptions>();
            telemetryServiceOptions.ConnectionString = certificateEncryptionService.Decrypt(telemetryServiceOptions.ConnectionString);
            configuration.Bind($"{nameof(TelemetryServiceOptions)}", options);

            options.ConnectionString = telemetryServiceOptions.ConnectionString;
        }
    }
}
