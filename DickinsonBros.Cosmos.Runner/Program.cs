using System;
using DickinsonBros.Encryption.Certificate.Extensions;
using DickinsonBros.Encryption.Certificate.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using DickinsonBros.Cosmos.Runner.Services;
using DickinsonBros.Cosmos.Runner.Models;
using DickinsonBros.Cosmos.Extensions;
using Microsoft.Extensions.Options;
using DickinsonBros.Cosmos.Models;
using DickinsonBros.NoSQLService.Abstractions;
using DickinsonBros.Stopwatch.Extensions;
using DickinsonBros.Logger.Extensions;
using DickinsonBros.Redactor.Extensions;
using DickinsonBros.Redactor.Models;
using DickinsonBros.Telemetry.Extensions;
using DickinsonBros.Telemetry.Models;
using DickinsonBros.DateTime.Extensions;
using Microsoft.Extensions.Hosting;

namespace DickinsonBros.Cosmos.Runner
{
    class Program
    {
        IConfiguration _configuration;
        async static Task Main()
        {
            await new Program().DoMain();
        }
        async Task DoMain()
        {
            try
            {
                var services = InitializeDependencyInjection();
                ConfigureServices(services);

                using (var provider = services.BuildServiceProvider())
                {
                    var noSQLService = provider.GetRequiredService<INoSQLService>();
                    var hostApplicationLifetime = provider.GetRequiredService<IHostApplicationLifetime>();
                    var guid = Guid.NewGuid().ToString();
                    var value = Guid.NewGuid().ToString();
                    var sampleModelValue = new SampleModel
                    {
                        id = guid,
                        key = guid,
                        coasterData = value
                    };

                    await noSQLService.InsertAsync(sampleModelValue.key, sampleModelValue).ConfigureAwait(false);
                    await noSQLService.UpsertAsync(sampleModelValue.key, sampleModelValue).ConfigureAwait(false);
                    var fetchedSampleModel = await noSQLService.FetchAsync<SampleModel>(sampleModelValue.id, sampleModelValue.key).ConfigureAwait(false);
                    await noSQLService.DeleteAsync(sampleModelValue.id, sampleModelValue.key).ConfigureAwait(false);

                    Console.WriteLine(
$@"
sampleModelValue: {System.Text.Json.JsonSerializer.Serialize(sampleModelValue)}
fetchedSampleModel: {System.Text.Json.JsonSerializer.Serialize(fetchedSampleModel)}
");


                    hostApplicationLifetime.StopApplication();
                }
                await Task.CompletedTask.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Console.WriteLine("End...");
                Console.ReadKey();
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddLogging(config =>
            {
                config.AddConfiguration(_configuration.GetSection("Logging"));

                if (Environment.GetEnvironmentVariable("BUILD_CONFIGURATION") == "DEBUG")
                {
                    config.AddConsole();
                }
            });

            services.AddSingleton<IHostApplicationLifetime, HostApplicationLifetime>();
            services.AddDateTimeService();
            services.AddStopwatchService();
            services.AddLoggingService();
            services.AddRedactorService();
            services.AddConfigurationEncryptionService();
            services.AddTelemetryService();
            services.AddCosmosService();
        }

      
 
        //internal readonly CosmosClient _cosmosClient;
        //internal readonly Container _cosmosContainer;

        IServiceCollection InitializeDependencyInjection()
        {
            var aspnetCoreEnvironment = Environment.GetEnvironmentVariable("BUILD_CONFIGURATION");
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{aspnetCoreEnvironment}.json", true);
            _configuration = builder.Build();
            var services = new ServiceCollection();
            services.AddSingleton(_configuration);
            return services;
        }
    }
}

