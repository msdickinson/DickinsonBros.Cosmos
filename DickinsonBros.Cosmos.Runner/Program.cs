using DickinsonBros.Cosmos.Extensions;
using DickinsonBros.Cosmos.Runner.Models;
using DickinsonBros.Cosmos.Runner.Services;
using DickinsonBros.DateTime.Extensions;
using DickinsonBros.Encryption.Certificate.Extensions;
using DickinsonBros.Logger.Extensions;
using DickinsonBros.Redactor.Extensions;
using DickinsonBros.Stopwatch.Extensions;
using DickinsonBros.Telemetry.Extensions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

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
                    var noSQLService = provider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var hostApplicationLifetime = provider.GetRequiredService<IHostApplicationLifetime>();
                    var guid = Guid.NewGuid().ToString();
                    var value = Guid.NewGuid().ToString();
                    var key = "SampleCosmosRunner";
                    var sampleModelValue = new SampleModel
                    {
                        Id = guid,
                        Key = key,
                        CoasterData = value
                    };

                    var result = await noSQLService.InsertAsync(sampleModelValue.Key, sampleModelValue).ConfigureAwait(false);
                    var resultTwo = await noSQLService.UpsertAsync(sampleModelValue.Key, result.Resource._etag, result.Resource).ConfigureAwait(false);
                    var resultThree = await noSQLService.UpsertAsync(sampleModelValue.Key, resultTwo.Resource._etag, resultTwo.Resource).ConfigureAwait(false);

                    var fetchedQuerySampleModel = await noSQLService.QueryAsync<SampleModel>
                    (
                        new QueryDefinition("SELECT * FROM coaster"),
                        new Microsoft.Azure.Cosmos.QueryRequestOptions {
                            PartitionKey = new PartitionKey(sampleModelValue.Key),
                            MaxItemCount = 100
                        }
                    ).ConfigureAwait(false);

                    var fetchedSampleModel = await noSQLService.FetchAsync<SampleModel>(sampleModelValue.Id, sampleModelValue.Key).ConfigureAwait(false);
                    await noSQLService.DeleteAsync(sampleModelValue.Id, sampleModelValue.Key).ConfigureAwait(false);

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
            services.AddCosmosService<SampleCosmosServiceOptions>();
        }

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

