using DickinsonBros.Cosmos.Models;
using DickinsonBros.DateTime.Abstractions;
using DickinsonBros.Logger.Abstractions;
using DickinsonBros.Stopwatch.Abstractions;
using DickinsonBros.Telemetry.Abstractions;
using DickinsonBros.Telemetry.Abstractions.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DickinsonBros.Cosmos
{
    public class CosmosService : ICosmosService
    {
        internal readonly IServiceProvider _serviceProvider;
        internal readonly ILoggingService<CosmosService> _logger;
        internal readonly ITelemetryService _telemetryService;
        internal readonly CosmosClient _cosmosClient;
        internal readonly Container _cosmosContainer;
        internal readonly IDateTimeService _dateTimeService;

        public CosmosService
        (
            CosmosClient cosmosClient,
            IServiceProvider serviceProvider,
            IOptions<CosmosServiceOptions> options,
            ITelemetryService telemetryService,
            IDateTimeService dateTimeService,
            ILoggingService<CosmosService> logger
        )
        {
            _cosmosClient = cosmosClient;
            _cosmosContainer = _cosmosClient.GetContainer(options.Value.DatabaseId, options.Value.ContainerId);

            _serviceProvider = serviceProvider;
            _logger = logger;
            _telemetryService = telemetryService;
            _dateTimeService = dateTimeService;
        }

        public async Task<ItemResponse<T>> FetchAsync<T>(string id, string key)
        {
            var methodIdentifier = $"{nameof(CosmosService)}.{nameof(CosmosService.FetchAsync)}";
            var stopwatchService = _serviceProvider.GetRequiredService<IStopwatchService>();

            var telemetry = new TelemetryData
            {
                Name = methodIdentifier,
                DateTime = _dateTimeService.GetDateTimeUTC(),
                TelemetryType = TelemetryType.NoSQL
            };

            try
            {
                stopwatchService.Start();
                var result =  await _cosmosContainer.ReadItemAsync<T>(id, new PartitionKey(key)).ConfigureAwait(false);
                stopwatchService.Stop();

                telemetry.ElapsedMilliseconds = (int)stopwatchService.ElapsedMilliseconds;
                telemetry.TelemetryState = TelemetryState.Successful;

                _logger.LogInformationRedacted
                (
                    methodIdentifier,
                    new Dictionary<string, object>
                    {
                        { nameof(id), id },
                        { nameof(key), key },
                        { nameof(result), result },
                        { nameof(stopwatchService.ElapsedMilliseconds), telemetry.ElapsedMilliseconds }
                    }
                );

                return result;
            }
            catch(Exception exception)
            {
                stopwatchService.Stop();
                telemetry.ElapsedMilliseconds = (int)stopwatchService.ElapsedMilliseconds;
                telemetry.TelemetryState = TelemetryState.Failed;

                _logger.LogErrorRedacted
                (
                    $"Unhandled exception {methodIdentifier}",
                    exception,
                    new Dictionary<string, object>
                    {
                        { nameof(id), id },
                        { nameof(key), key },
                        { nameof(stopwatchService.ElapsedMilliseconds), telemetry.ElapsedMilliseconds }
                    }
                );

                throw;
            }
            finally
            {
                _telemetryService.Insert(telemetry);
            }
        }

        public async Task<ItemResponse<T>> InsertAsync<T>(string key, T value)
        {
            var methodIdentifier = $"{nameof(CosmosService)}.{nameof(CosmosService.InsertAsync)}";
            var stopwatchService = _serviceProvider.GetRequiredService<IStopwatchService>();

            var telemetry = new TelemetryData
            {
                Name = methodIdentifier,
                DateTime = _dateTimeService.GetDateTimeUTC(),
                TelemetryType = TelemetryType.NoSQL
            };

            try
            {
                stopwatchService.Start();
                var itemResponse = await _cosmosContainer.CreateItemAsync<T>(value, new PartitionKey(key)).ConfigureAwait(false);

                stopwatchService.Stop();
                telemetry.ElapsedMilliseconds = (int)stopwatchService.ElapsedMilliseconds;
                telemetry.TelemetryState = TelemetryState.Successful;

                _logger.LogInformationRedacted
                (
                    methodIdentifier,
                    new Dictionary<string, object>
                    {
                        { nameof(key), key },
                        { nameof(value), value },
                        { nameof(stopwatchService.ElapsedMilliseconds), telemetry.ElapsedMilliseconds }
                    }
                );

                return itemResponse;
            }
            catch (Exception exception)
            {
                stopwatchService.Stop();
                telemetry.ElapsedMilliseconds = (int)stopwatchService.ElapsedMilliseconds;
                telemetry.TelemetryState = TelemetryState.Failed;

                _logger.LogErrorRedacted
                (
                    $"Unhandled exception {methodIdentifier}",
                    exception,
                    new Dictionary<string, object>
                    {
                        { nameof(key), key },
                        { nameof(value), value },
                        { nameof(stopwatchService.ElapsedMilliseconds), telemetry.ElapsedMilliseconds }
                    }
                );

                throw;
            }
            finally
            {
                _telemetryService.Insert(telemetry);
            }
        }

        public async Task<ItemResponse<T>> UpsertAsync<T>(string key, string eTag, T value)
        {
            var methodIdentifier = $"{nameof(CosmosService)}.{nameof(CosmosService.UpsertAsync)}";
            var stopwatchService = _serviceProvider.GetRequiredService<IStopwatchService>();

            var telemetry = new TelemetryData
            {
                Name = methodIdentifier,
                DateTime = _dateTimeService.GetDateTimeUTC(),
                TelemetryType = TelemetryType.NoSQL
            };

            try
            {
                stopwatchService.Start();
                var itemResponse = await _cosmosContainer.UpsertItemAsync<T>(value, new PartitionKey(key), new ItemRequestOptions { IfMatchEtag = eTag }).ConfigureAwait(false);
                stopwatchService.Stop();

                telemetry.ElapsedMilliseconds = (int)stopwatchService.ElapsedMilliseconds;
                telemetry.TelemetryState = TelemetryState.Successful;

                _logger.LogInformationRedacted
                (
                    methodIdentifier,
                    new Dictionary<string, object>
                    {
                        { nameof(key), key },
                        { nameof(value), value },
                        { nameof(stopwatchService.ElapsedMilliseconds), telemetry.ElapsedMilliseconds }
                    }
                );

                return itemResponse;
            }
            catch(CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
            {
                stopwatchService.Stop();
                telemetry.ElapsedMilliseconds = (int)stopwatchService.ElapsedMilliseconds;
                telemetry.TelemetryState = TelemetryState.BadRequest;

                _logger.LogInformationRedacted
                (
                    $"PreconditionFailed {methodIdentifier}",
                    new Dictionary<string, object>
                    {
                        { nameof(key), key },
                        { nameof(value), value },
                        { nameof(stopwatchService.ElapsedMilliseconds), telemetry.ElapsedMilliseconds }
                    }
                );

                throw;
            }
            catch (Exception exception)
            {
                stopwatchService.Stop();
                telemetry.ElapsedMilliseconds = (int)stopwatchService.ElapsedMilliseconds;
                telemetry.TelemetryState = TelemetryState.Failed;

                _logger.LogErrorRedacted
                (
                    $"Unhandled exception {methodIdentifier}",
                    exception,
                    new Dictionary<string, object>
                    {
                        { nameof(key), key },
                        { nameof(value), value },
                        { nameof(stopwatchService.ElapsedMilliseconds), telemetry.ElapsedMilliseconds }
                    }
                );

                throw;
            }
            finally
            {
                _telemetryService.Insert(telemetry);
            }

        }

        public async Task<ItemResponse<object>> DeleteAsync(string id, string key)
        {
            var methodIdentifier = $"{nameof(CosmosService)}.{nameof(CosmosService.DeleteAsync)}";
            var stopwatchService = _serviceProvider.GetRequiredService<IStopwatchService>();

            var telemetry = new TelemetryData
            {
                Name = methodIdentifier,
                DateTime = _dateTimeService.GetDateTimeUTC(),
                TelemetryType = TelemetryType.NoSQL
            };

            try
            {
                stopwatchService.Start();
                var response = await _cosmosContainer.DeleteItemAsync<object>(id, new PartitionKey(key)).ConfigureAwait(false);
                stopwatchService.Stop();
                telemetry.ElapsedMilliseconds = (int)stopwatchService.ElapsedMilliseconds;
                telemetry.TelemetryState = TelemetryState.Successful;

                _logger.LogInformationRedacted
                (
                    methodIdentifier,
                    new Dictionary<string, object>
                    {
                        { nameof(id), id },
                        { nameof(key), key },
                        { nameof(stopwatchService.ElapsedMilliseconds), telemetry.ElapsedMilliseconds }
                    }
                );

                return response;
            }
            catch (Exception exception)
            {
                stopwatchService.Stop();
                telemetry.ElapsedMilliseconds = (int)stopwatchService.ElapsedMilliseconds;
                telemetry.TelemetryState = TelemetryState.Failed;

                _logger.LogErrorRedacted
                (
                    $"Unhandled exception {methodIdentifier}",
                    exception,
                    new Dictionary<string, object>
                    {
                        { nameof(id), id },
                        { nameof(key), key },
                        { nameof(stopwatchService.ElapsedMilliseconds), telemetry.ElapsedMilliseconds }
                    }
                );

                throw;
            }
            finally
            {
                _telemetryService.Insert(telemetry);
            }
        }

    }
}
