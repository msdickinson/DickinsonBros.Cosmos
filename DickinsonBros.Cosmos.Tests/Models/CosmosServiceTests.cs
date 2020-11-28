using DickinsonBros.Cosmos.Models;
using DickinsonBros.DateTime.Abstractions;
using DickinsonBros.Logger.Abstractions;
using DickinsonBros.Stopwatch.Abstractions;
using DickinsonBros.Telemetry.Abstractions;
using DickinsonBros.Telemetry.Abstractions.Models;
using DickinsonBros.Test;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DickinsonBros.Cosmos.Tests.Models
{
    public class SampleCosmosServiceOptions : CosmosServiceOptions
    { }


    public class SampleModel
    {
        public string key { get; set; }
        public string id { get; set; }
        public string coasterData { get; set; }
    }

    [TestClass]
    public class CosmosServiceTests : BaseTest
    {
        #region QueryAsync

        [TestMethod]
        public async Task QueryAsync_Runs_GetDateTimeUTCCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.QueryAsync)}";

                    //--ILoggingService<CosmosService<SampleCosmosServiceOptions
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //--IDateTimeService
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //--CosmosClient
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();

                    //--FeedResponse<SampleModel>                    
                    var sampleModels = new List<SampleModel>
                    {
                        new SampleModel
                        {
                            id = "1",
                            coasterData = "SampleCoaterData1",
                            key = "SampleKey1"
                        },
                        new SampleModel
                        {
                            id = "2",
                            coasterData = "SampleCoaterData2",
                            key = "SampleKey2"
                        }
                    };
                    var feedResponseMock = new Mock<FeedResponse<SampleModel>>();
                    feedResponseMock.Setup
                    (
                        feedResponse => feedResponse.GetEnumerator()
                    )
                    .Returns
                    (
                        sampleModels.GetEnumerator()
                    );

                    //--FeedIterator<SampleModel>
                    var feedIteratorMock = new Mock<FeedIterator<SampleModel>>();
                    var feedIteratorCalls = 0;

                    feedIteratorMock.Setup
                    (
                        feedIterator => feedIterator.HasMoreResults
                    )
                    .Returns(() =>
                    {
                        feedIteratorCalls++;
                        return feedIteratorCalls <= 1;
                    });
                    feedIteratorMock.Setup
                    (
                        feedIterator => feedIterator.ReadNextAsync
                        (
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(feedResponseMock.Object);

                    //--Container
                    var containerMock = serviceProvider.GetMock<Container>();
                    var itemResponseMock = new Mock<ItemResponse<SampleModel>>();
                    itemResponseMock
                    .Setup(itemResponse => itemResponse.Resource)
                    .Returns(sampleModel);

                    containerMock
                    .Setup
                    (
                        container => container.GetItemQueryIterator<SampleModel>
                        (
                            It.IsAny<QueryDefinition>(),
                            It.IsAny<string>(),
                            It.IsAny<QueryRequestOptions>()
                        )
                    )
                    .Returns(feedIteratorMock.Object);

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ITelemetryService
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //--IStopwatchService
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);


                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    var sampleQuery = "SampleQuery";
                    var queryDef = new QueryDefinition(sampleQuery);
                    var queryRequestOptions = new QueryRequestOptions();

                    //Act
                    var observed = await uut.QueryAsync<SampleModel>(queryDef, queryRequestOptions);

                    //Assert
                    dateTimeServiceMock
                    .Verify
                    (
                        dateTimeService => dateTimeService.GetDateTimeUTC(),
                        Times.Once
                    );
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task QueryAsync_Runs_StopWatchStartCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.QueryAsync)}";

                    //--ILoggingService<CosmosService<SampleCosmosServiceOptions
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //--IDateTimeService
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //--CosmosClient
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();

                    //--FeedResponse<SampleModel>                    
                    var sampleModels = new List<SampleModel>
                    {
                        new SampleModel
                        {
                            id = "1",
                            coasterData = "SampleCoaterData1",
                            key = "SampleKey1"
                        },
                        new SampleModel
                        {
                            id = "2",
                            coasterData = "SampleCoaterData2",
                            key = "SampleKey2"
                        }
                    };
                    var feedResponseMock = new Mock<FeedResponse<SampleModel>>();
                    feedResponseMock.Setup
                    (
                        feedResponse => feedResponse.GetEnumerator()
                    )
                    .Returns
                    (
                        sampleModels.GetEnumerator()
                    );

                    //--FeedIterator<SampleModel>
                    var feedIteratorMock = new Mock<FeedIterator<SampleModel>>();
                    var feedIteratorCalls = 0;

                    feedIteratorMock.Setup
                    (
                        feedIterator => feedIterator.HasMoreResults
                    )
                    .Returns(() =>
                    {
                        feedIteratorCalls++;
                        return feedIteratorCalls <= 1;
                    });
                    feedIteratorMock.Setup
                    (
                        feedIterator => feedIterator.ReadNextAsync
                        (
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(feedResponseMock.Object);

                    //--Container
                    var containerMock = serviceProvider.GetMock<Container>();
                    var itemResponseMock = new Mock<ItemResponse<SampleModel>>();
                    itemResponseMock
                    .Setup(itemResponse => itemResponse.Resource)
                    .Returns(sampleModel);

                    containerMock
                    .Setup
                    (
                        container => container.GetItemQueryIterator<SampleModel>
                        (
                            It.IsAny<QueryDefinition>(),
                            It.IsAny<string>(),
                            It.IsAny<QueryRequestOptions>()
                        )
                    )
                    .Returns(feedIteratorMock.Object);

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ITelemetryService
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //--IStopwatchService
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);


                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    var sampleQuery = "SampleQuery";
                    var queryDef = new QueryDefinition(sampleQuery);
                    var queryRequestOptions = new QueryRequestOptions();

                    //Act
                    var observed = await uut.QueryAsync<SampleModel>(queryDef, queryRequestOptions);

                    //Assert
                    stopwatchServiceMock
                    .Verify
                    (
                        stopwatchService => stopwatchService.Start(),
                        Times.Once
                    );
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task QueryAsync_Runs_CosmosGetItemQueryIteratorCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.QueryAsync)}";

                    //--ILoggingService<CosmosService<SampleCosmosServiceOptions
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //--IDateTimeService
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //--CosmosClient
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();

                    //--FeedResponse<SampleModel>                    
                    var sampleModels = new List<SampleModel>
                    {
                        new SampleModel
                        {
                            id = "1",
                            coasterData = "SampleCoaterData1",
                            key = "SampleKey1"
                        },
                        new SampleModel
                        {
                            id = "2",
                            coasterData = "SampleCoaterData2",
                            key = "SampleKey2"
                        }
                    };
                    var feedResponseMock = new Mock<FeedResponse<SampleModel>>();
                    feedResponseMock.Setup
                    (
                        feedResponse => feedResponse.GetEnumerator()
                    )
                    .Returns
                    (
                        sampleModels.GetEnumerator()
                    );

                    //--FeedIterator<SampleModel>
                    var feedIteratorMock = new Mock<FeedIterator<SampleModel>>();
                    var feedIteratorCalls = 0;

                    feedIteratorMock.Setup
                    (
                        feedIterator => feedIterator.HasMoreResults
                    )
                    .Returns(() =>
                    {
                        feedIteratorCalls++;
                        return feedIteratorCalls <= 1;
                    });
                    feedIteratorMock.Setup
                    (
                        feedIterator => feedIterator.ReadNextAsync
                        (
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(feedResponseMock.Object);

                    //--Container
                    var containerMock = serviceProvider.GetMock<Container>();
                    var itemResponseMock = new Mock<ItemResponse<SampleModel>>();
                    itemResponseMock
                    .Setup(itemResponse => itemResponse.Resource)
                    .Returns(sampleModel);

                    containerMock
                    .Setup
                    (
                        container => container.GetItemQueryIterator<SampleModel>
                        (
                            It.IsAny<QueryDefinition>(),
                            It.IsAny<string>(),
                            It.IsAny<QueryRequestOptions>()
                        )
                    )
                    .Returns(feedIteratorMock.Object);

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ITelemetryService
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //--IStopwatchService
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);


                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    var sampleQuery = "SampleQuery";
                    var queryDef = new QueryDefinition(sampleQuery);
                    var queryRequestOptions = new QueryRequestOptions();

                    //Act
                    var observed = await uut.QueryAsync<SampleModel>(queryDef, queryRequestOptions);

                    //Assert

                    containerMock
                    .Verify
                    (
                        container => container.GetItemQueryIterator<SampleModel>
                        (
                            queryDef,
                            null,
                            queryRequestOptions
                        ),
                        Times.Once
                    );
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task QueryAsync_Runs_StopWatchStopCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.QueryAsync)}";

                    //--ILoggingService<CosmosService<SampleCosmosServiceOptions
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //--IDateTimeService
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //--CosmosClient
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();

                    //--FeedResponse<SampleModel>                    
                    var sampleModels = new List<SampleModel>
                    {
                        new SampleModel
                        {
                            id = "1",
                            coasterData = "SampleCoaterData1",
                            key = "SampleKey1"
                        },
                        new SampleModel
                        {
                            id = "2",
                            coasterData = "SampleCoaterData2",
                            key = "SampleKey2"
                        }
                    };
                    var feedResponseMock = new Mock<FeedResponse<SampleModel>>();
                    feedResponseMock.Setup
                    (
                        feedResponse => feedResponse.GetEnumerator()
                    )
                    .Returns
                    (
                        sampleModels.GetEnumerator()
                    );

                    //--FeedIterator<SampleModel>
                    var feedIteratorMock = new Mock<FeedIterator<SampleModel>>();
                    var feedIteratorCalls = 0;

                    feedIteratorMock.Setup
                    (
                        feedIterator => feedIterator.HasMoreResults
                    )
                    .Returns(() =>
                    {
                        feedIteratorCalls++;
                        return feedIteratorCalls <= 1;
                    });
                    feedIteratorMock.Setup
                    (
                        feedIterator => feedIterator.ReadNextAsync
                        (
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(feedResponseMock.Object);

                    //--Container
                    var containerMock = serviceProvider.GetMock<Container>();
                    var itemResponseMock = new Mock<ItemResponse<SampleModel>>();
                    itemResponseMock
                    .Setup(itemResponse => itemResponse.Resource)
                    .Returns(sampleModel);

                    containerMock
                    .Setup
                    (
                        container => container.GetItemQueryIterator<SampleModel>
                        (
                            It.IsAny<QueryDefinition>(),
                            It.IsAny<string>(),
                            It.IsAny<QueryRequestOptions>()
                        )
                    )
                    .Returns(feedIteratorMock.Object);

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ITelemetryService
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //--IStopwatchService
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);


                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    var sampleQuery = "SampleQuery";
                    var queryDef = new QueryDefinition(sampleQuery);
                    var queryRequestOptions = new QueryRequestOptions();

                    //Act
                    var observed = await uut.QueryAsync<SampleModel>(queryDef, queryRequestOptions);

                    //Assert
                    stopwatchServiceMock
                    .Verify
                    (
                        stopwatchService => stopwatchService.Stop(),
                        Times.Once
                    );
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task QueryAsync_Runs_LogInformationRedactedCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.QueryAsync)}";

                    //--ILoggingService<CosmosService<SampleCosmosServiceOptions
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //--IDateTimeService
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //--CosmosClient
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();

                    //--FeedResponse<SampleModel>                    
                    var sampleModels = new List<SampleModel>
                    {
                        new SampleModel
                        {
                            id = "1",
                            coasterData = "SampleCoaterData1",
                            key = "SampleKey1"
                        },
                        new SampleModel
                        {
                            id = "2",
                            coasterData = "SampleCoaterData2",
                            key = "SampleKey2"
                        }
                    };
                    var feedResponseMock = new Mock<FeedResponse<SampleModel>>();
                    feedResponseMock.Setup
                    (
                        feedResponse => feedResponse.GetEnumerator()
                    )
                    .Returns
                    (
                        sampleModels.GetEnumerator()
                    );

                    //--FeedIterator<SampleModel>
                    var feedIteratorMock = new Mock<FeedIterator<SampleModel>>();
                    var feedIteratorCalls = 0;

                    feedIteratorMock.Setup
                    (
                        feedIterator => feedIterator.HasMoreResults
                    )
                    .Returns(() =>
                    {
                        feedIteratorCalls++;
                        return feedIteratorCalls <= 1;
                    });
                    feedIteratorMock.Setup
                    (
                        feedIterator => feedIterator.ReadNextAsync
                        (
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(feedResponseMock.Object);

                    //--Container
                    var containerMock = serviceProvider.GetMock<Container>();
                    var itemResponseMock = new Mock<ItemResponse<SampleModel>>();
                    itemResponseMock
                    .Setup(itemResponse => itemResponse.Resource)
                    .Returns(sampleModel);

                    containerMock
                    .Setup
                    (
                        container => container.GetItemQueryIterator<SampleModel>
                        (
                            It.IsAny<QueryDefinition>(),
                            It.IsAny<string>(),
                            It.IsAny<QueryRequestOptions>()
                        )
                    )
                    .Returns(feedIteratorMock.Object);

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ITelemetryService
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //--IStopwatchService
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);


                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    var sampleQuery = "SampleQuery";
                    var queryDef = new QueryDefinition(sampleQuery);
                    var queryRequestOptions = new QueryRequestOptions();

                    //Act
                    var observed = await uut.QueryAsync<SampleModel>(queryDef, queryRequestOptions);

                    //Assert
                    loggingServiceMock
                    .Verify
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        ),
                        Times.Once
                    );

                    Assert.AreEqual(methodIdentifier                    , messageObserved);
                    Assert.AreEqual(4                                   , propertiesObserved.Count);
                    Assert.AreEqual(2                                   , ((List<SampleModel>)propertiesObserved["items"]).Count);
                    Assert.AreEqual(sampleModels.First()                , ((List<SampleModel>)propertiesObserved["items"]).First());
                    Assert.AreEqual(sampleModels.Last()                 , ((List<SampleModel>)propertiesObserved["items"]).Last());
                    Assert.AreEqual(queryDef                            , propertiesObserved["queryDefinition"]);
                    Assert.AreEqual(queryRequestOptions                 , propertiesObserved["queryRequestOptions"]);
                    Assert.AreEqual((int)expectedElapsedMilliseconds    , propertiesObserved["ElapsedMilliseconds"]);
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task QueryAsync_Runs_ReturnsItems()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.QueryAsync)}";

                    //--ILoggingService<CosmosService<SampleCosmosServiceOptions
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //--IDateTimeService
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //--CosmosClient
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();

                    //--FeedResponse<SampleModel>                    
                    var sampleModels = new List<SampleModel>
                    {
                        new SampleModel
                        {
                            id = "1",
                            coasterData = "SampleCoaterData1",
                            key = "SampleKey1"
                        },
                        new SampleModel
                        {
                            id = "2",
                            coasterData = "SampleCoaterData2",
                            key = "SampleKey2"
                        }
                    };
                    var feedResponseMock = new Mock<FeedResponse<SampleModel>>();
                    feedResponseMock.Setup
                    (
                        feedResponse => feedResponse.GetEnumerator()
                    )
                    .Returns
                    (
                        sampleModels.GetEnumerator()
                    );

                    //--FeedIterator<SampleModel>
                    var feedIteratorMock = new Mock<FeedIterator<SampleModel>>();
                    var feedIteratorCalls = 0;

                    feedIteratorMock.Setup
                    (
                        feedIterator => feedIterator.HasMoreResults
                    )
                    .Returns(() =>
                    {
                        feedIteratorCalls++;
                        return feedIteratorCalls <= 1;
                    });
                    feedIteratorMock.Setup
                    (
                        feedIterator => feedIterator.ReadNextAsync
                        (
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(feedResponseMock.Object);

                    //--Container
                    var containerMock = serviceProvider.GetMock<Container>();
                    var itemResponseMock = new Mock<ItemResponse<SampleModel>>();
                    itemResponseMock
                    .Setup(itemResponse => itemResponse.Resource)
                    .Returns(sampleModel);

                    containerMock
                    .Setup
                    (
                        container => container.GetItemQueryIterator<SampleModel>
                        (
                            It.IsAny<QueryDefinition>(),
                            It.IsAny<string>(),
                            It.IsAny<QueryRequestOptions>()
                        )
                    )
                    .Returns(feedIteratorMock.Object);

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ITelemetryService
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //--IStopwatchService
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);


                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    var sampleQuery = "SampleQuery";
                    var queryDef = new QueryDefinition(sampleQuery);
                    var queryRequestOptions = new QueryRequestOptions();

                    //Act
                    var observed = await uut.QueryAsync<SampleModel>(queryDef, queryRequestOptions);

                    //Assert
                    Assert.IsNotNull(observed);
                    Assert.AreEqual(2                       , observed.Count());
                    Assert.AreEqual(sampleModels.First()    , observed.First());
                    Assert.AreEqual(sampleModels.Last()     , observed.Last());
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task QueryAsync_OnExceptionThrown_LogErrorRedactedCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.QueryAsync)}";

                    //--ILoggingService<CosmosService<SampleCosmosServiceOptions
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //--IDateTimeService
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //--CosmosClient
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();

                    //--FeedResponse<SampleModel>                    
                    var sampleModels = new List<SampleModel>
                    {
                        new SampleModel
                        {
                            id = "1",
                            coasterData = "SampleCoaterData1",
                            key = "SampleKey1"
                        },
                        new SampleModel
                        {
                            id = "2",
                            coasterData = "SampleCoaterData2",
                            key = "SampleKey2"
                        }
                    };
                    var feedResponseMock = new Mock<FeedResponse<SampleModel>>();
                    feedResponseMock.Setup
                    (
                        feedResponse => feedResponse.GetEnumerator()
                    )
                    .Returns
                    (
                        sampleModels.GetEnumerator()
                    );

                    //--FeedIterator<SampleModel>
                    var feedIteratorMock = new Mock<FeedIterator<SampleModel>>();
                    var feedIteratorCalls = 0;

                    feedIteratorMock.Setup
                    (
                        feedIterator => feedIterator.HasMoreResults
                    )
                    .Returns(() =>
                    {
                        feedIteratorCalls++;
                        return feedIteratorCalls <= 1;
                    });
                    feedIteratorMock.Setup
                    (
                        feedIterator => feedIterator.ReadNextAsync
                        (
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(feedResponseMock.Object);

                    //--Container
                    var containerMock = serviceProvider.GetMock<Container>();
                    var itemResponseMock = new Mock<ItemResponse<SampleModel>>();
                    itemResponseMock
                    .Setup(itemResponse => itemResponse.Resource)
                    .Returns(sampleModel);

                    containerMock
                    .Setup
                    (
                        container => container.GetItemQueryIterator<SampleModel>
                        (
                            It.IsAny<QueryDefinition>(),
                            It.IsAny<string>(),
                            It.IsAny<QueryRequestOptions>()
                        )
                    )
                    .Throws(new Exception());

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ITelemetryService
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //--IStopwatchService
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    var sampleQuery = "SampleQuery";
                    var queryDef = new QueryDefinition(sampleQuery);
                    var queryRequestOptions = new QueryRequestOptions();

                    //Act
                    var observed = await uut.QueryAsync<SampleModel>(queryDef, queryRequestOptions);

                    //Assert
                    loggingServiceMock
                    .Verify
                    (
                        loggingService => loggingService.LogErrorRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<Exception>(),
                            It.IsAny<IDictionary<string, object>>()
                        ),
                        Times.Once
                    );

                    Assert.AreEqual($"Unhandled exception {methodIdentifier}", messageObserved);
                    Assert.AreEqual(3, propertiesObserved.Count);
                    Assert.AreEqual(queryDef, propertiesObserved["queryDefinition"]);
                    Assert.AreEqual(queryRequestOptions, propertiesObserved["queryRequestOptions"]);
                    Assert.AreEqual((int)expectedElapsedMilliseconds, propertiesObserved["ElapsedMilliseconds"]);
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task QueryAsync_OnExceptionThrown_ThrowsException()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.QueryAsync)}";

                    //--ILoggingService<CosmosService<SampleCosmosServiceOptions
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //--IDateTimeService
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //--CosmosClient
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();

                    //--FeedResponse<SampleModel>                    
                    var sampleModels = new List<SampleModel>
                    {
                        new SampleModel
                        {
                            id = "1",
                            coasterData = "SampleCoaterData1",
                            key = "SampleKey1"
                        },
                        new SampleModel
                        {
                            id = "2",
                            coasterData = "SampleCoaterData2",
                            key = "SampleKey2"
                        }
                    };
                    var feedResponseMock = new Mock<FeedResponse<SampleModel>>();
                    feedResponseMock.Setup
                    (
                        feedResponse => feedResponse.GetEnumerator()
                    )
                    .Returns
                    (
                        sampleModels.GetEnumerator()
                    );

                    //--FeedIterator<SampleModel>
                    var feedIteratorMock = new Mock<FeedIterator<SampleModel>>();
                    var feedIteratorCalls = 0;

                    feedIteratorMock.Setup
                    (
                        feedIterator => feedIterator.HasMoreResults
                    )
                    .Returns(() =>
                    {
                        feedIteratorCalls++;
                        return feedIteratorCalls <= 1;
                    });
                    feedIteratorMock.Setup
                    (
                        feedIterator => feedIterator.ReadNextAsync
                        (
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(feedResponseMock.Object);

                    //--Container
                    var containerMock = serviceProvider.GetMock<Container>();
                    var itemResponseMock = new Mock<ItemResponse<SampleModel>>();
                    itemResponseMock
                    .Setup(itemResponse => itemResponse.Resource)
                    .Returns(sampleModel);

                    containerMock
                    .Setup
                    (
                        container => container.GetItemQueryIterator<SampleModel>
                        (
                            It.IsAny<QueryDefinition>(),
                            It.IsAny<string>(),
                            It.IsAny<QueryRequestOptions>()
                        )
                    )
                    .Throws(new Exception());

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ITelemetryService
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //--IStopwatchService
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);


                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    var sampleQuery = "SampleQuery";
                    var queryDef = new QueryDefinition(sampleQuery);
                    var queryRequestOptions = new QueryRequestOptions();

                    //Act
                    var observed = await uut.QueryAsync<SampleModel>(queryDef, queryRequestOptions);

                    //Assert

                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task QueryAsync_Runs_TelemetryServiceInsertCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.QueryAsync)}";

                    //--ILoggingService<CosmosService<SampleCosmosServiceOptions
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //--IDateTimeService
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //--CosmosClient
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();

                    //--FeedResponse<SampleModel>                    
                    var sampleModels = new List<SampleModel>
                    {
                        new SampleModel
                        {
                            id = "1",
                            coasterData = "SampleCoaterData1",
                            key = "SampleKey1"
                        },
                        new SampleModel
                        {
                            id = "2",
                            coasterData = "SampleCoaterData2",
                            key = "SampleKey2"
                        }
                    };
                    var feedResponseMock = new Mock<FeedResponse<SampleModel>>();
                    feedResponseMock.Setup
                    (
                        feedResponse => feedResponse.GetEnumerator()
                    )
                    .Returns
                    (
                        sampleModels.GetEnumerator()
                    );

                    //--FeedIterator<SampleModel>
                    var feedIteratorMock = new Mock<FeedIterator<SampleModel>>();
                    var feedIteratorCalls = 0;

                    feedIteratorMock.Setup
                    (
                        feedIterator => feedIterator.HasMoreResults
                    )
                    .Returns(() =>
                    {
                        feedIteratorCalls++;
                        return feedIteratorCalls <= 1;
                    });
                    feedIteratorMock.Setup
                    (
                        feedIterator => feedIterator.ReadNextAsync
                        (
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(feedResponseMock.Object);

                    //--Container
                    var containerMock = serviceProvider.GetMock<Container>();
                    var itemResponseMock = new Mock<ItemResponse<SampleModel>>();
                    itemResponseMock
                    .Setup(itemResponse => itemResponse.Resource)
                    .Returns(sampleModel);

                    containerMock
                    .Setup
                    (
                        container => container.GetItemQueryIterator<SampleModel>
                        (
                            It.IsAny<QueryDefinition>(),
                            It.IsAny<string>(),
                            It.IsAny<QueryRequestOptions>()
                        )
                    )
                    .Returns(feedIteratorMock.Object);

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ITelemetryService
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //--IStopwatchService
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);


                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    var sampleQuery = "SampleQuery";
                    var queryDef = new QueryDefinition(sampleQuery);
                    var queryRequestOptions = new QueryRequestOptions();

                    //Act
                    var observed = await uut.QueryAsync<SampleModel>(queryDef, queryRequestOptions);

                    //Assert
                    telemetryServiceMock
                    .Verify
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        ),
                        Times.Once
                    );

                    Assert.IsNotNull(telemetryDataObserved);

                    Assert.AreEqual(methodIdentifier, telemetryDataObserved.Name);
                    Assert.AreEqual(TelemetryType.NoSQL, telemetryDataObserved.TelemetryType);
                    Assert.AreEqual(expectedElapsedMilliseconds, telemetryDataObserved.ElapsedMilliseconds);
                    Assert.AreEqual(TelemetryState.Successful, telemetryDataObserved.TelemetryState);
                    Assert.AreEqual(expectedDateTime, telemetryDataObserved.DateTime);
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }


        #endregion

        #region FetchAsync

        [TestMethod]
        public async Task FetchAsync_Runs_GetDateTimeUTCCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.FetchAsync)}";

                    //--ILoggingService<CosmosService<SampleCosmosServiceOptions
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //--IDateTimeService
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //--CosmosClient
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();

                    //--Container
                    var containerMock = serviceProvider.GetMock<Container>();
                    var itemResponseMock = new Mock<ItemResponse<SampleModel>>();
                    itemResponseMock
                    .Setup(itemResponse => itemResponse.Resource)
                    .Returns(sampleModel);

                    containerMock
                    .Setup
                    (
                        container => container.ReadItemAsync<SampleModel>
                        (
                            It.IsAny<string>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(itemResponseMock.Object);

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ITelemetryService
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //--IStopwatchService
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);


                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    var id = "1";
                    var key = "1";

                    //Act
                    var observed = await uut.FetchAsync<SampleModel>(id, key);

                    //Assert
                    dateTimeServiceMock
                    .Verify
                    (
                        dateTimeService => dateTimeService.GetDateTimeUTC(),
                        Times.Once
                    );
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task FetchAsync_Runs_StopWatchStartCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.FetchAsync)}";

                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });


                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();
                    var itemResponseMock = new Mock<ItemResponse<SampleModel>>();
                    itemResponseMock
                    .Setup(itemResponse => itemResponse.Resource)
                    .Returns(sampleModel);

                    containerMock
                    .Setup
                    (
                        container => container.ReadItemAsync<SampleModel>
                        (
                            It.IsAny<string>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(itemResponseMock.Object);

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);


                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    var id = "1";
                    var key = "1";

                    //Act
                    var observed = await uut.FetchAsync<SampleModel>(id, key);

                    //Assert
                    stopwatchServiceMock
                    .Verify
                    (
                        stopwatchService => stopwatchService.Start(),
                        Times.Once
                    );
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task FetchAsync_Runs__CosmosContainerReadItemAsyncCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.FetchAsync)}";

                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });


                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();
                    var itemResponseMock = new Mock<ItemResponse<SampleModel>>();
                    itemResponseMock
                    .Setup(itemResponse => itemResponse.Resource)
                    .Returns(sampleModel);

                    var idObserved = (string)null;
                    var partitionKeyObserved = (PartitionKey?)null;

                    containerMock
                    .Setup
                    (
                        container => container.ReadItemAsync<SampleModel>
                        (
                            It.IsAny<string>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Callback((string id, PartitionKey partitionKey, ItemRequestOptions requestOptions, CancellationToken cancellationToken) =>
                    {
                        idObserved = id;
                        partitionKeyObserved = partitionKey;
                    })
                    .ReturnsAsync(itemResponseMock.Object);

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);


                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    var _id = "1";
                    var key = "1";

                    //Act
                    var observed = await uut.FetchAsync<SampleModel>(_id, key);

                    //Assert
                    containerMock
                    .Verify
                    (
                        container => container.ReadItemAsync<SampleModel>
                        (
                                It.IsAny<string>(),
                                It.IsAny<PartitionKey>(),
                                It.IsAny<ItemRequestOptions>(),
                                It.IsAny<CancellationToken>()
                        ),
                        Times.Once
                    );

                    Assert.AreEqual(_id, idObserved);
                    Assert.AreEqual(new PartitionKey(key).ToString(), partitionKeyObserved.Value.ToString());
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task FetchAsync_Runs_StopWatchStopCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.FetchAsync)}";

                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });


                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();
                    var itemResponseMock = new Mock<ItemResponse<SampleModel>>();
                    itemResponseMock
                    .Setup(itemResponse => itemResponse.Resource)
                    .Returns(sampleModel);

                    containerMock
                    .Setup
                    (
                        container => container.ReadItemAsync<SampleModel>
                        (
                            It.IsAny<string>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(itemResponseMock.Object);

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);


                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    var id = "1";
                    var key = "1";

                    //Act
                    var observed = await uut.FetchAsync<SampleModel>(id, key);

                    //Assert
                    stopwatchServiceMock
                    .Verify
                    (
                        stopwatchService => stopwatchService.Stop(),
                        Times.Once
                    );
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task FetchAsync_Runs_LogInformationRedactedCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.FetchAsync)}";
                    var expectedElapsedMilliseconds = (long)5000;

                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });


                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();
                    var itemResponseMock = new Mock<ItemResponse<SampleModel>>();
                    itemResponseMock
                    .Setup(itemResponse => itemResponse.Resource)
                    .Returns(sampleModel);

                    containerMock
                    .Setup
                    (
                        container => container.ReadItemAsync<SampleModel>
                        (
                            It.IsAny<string>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(itemResponseMock.Object);

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);


                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);


                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    var id = "1";
                    var key = "1";

                    //Act
                    var observed = await uut.FetchAsync<SampleModel>(id, key);

                    //Assert
                    loggingServiceMock
                    .Verify
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        ),
                        Times.Once
                    );

                    Assert.AreEqual(methodIdentifier, messageObserved);
                    Assert.AreEqual(4, propertiesObserved.Count);
                    Assert.AreEqual(id, propertiesObserved["id"]);
                    Assert.AreEqual(key, propertiesObserved["key"]);
                    Assert.AreEqual(itemResponseMock.Object, propertiesObserved["result"]);
                    Assert.AreEqual((int)expectedElapsedMilliseconds, propertiesObserved["ElapsedMilliseconds"]);
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task FetchAsync_OnExceptionThrown_LogErrorRedactedCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.FetchAsync)}";

                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogErrorRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<Exception>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, Exception exception, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();
                    var itemResponseMock = new Mock<ItemResponse<SampleModel>>();
                    itemResponseMock
                    .Setup(itemResponse => itemResponse.Resource)
                    .Returns(sampleModel);

                    containerMock
                    .Setup
                    (
                        container => container.ReadItemAsync<SampleModel>
                        (
                            It.IsAny<string>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Throws(new Exception());

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);


                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    var id = "1";
                    var key = "1";

                    //Act
                    var observed = await uut.FetchAsync<SampleModel>(id, key);

                    //Assert
                    loggingServiceMock
                    .Verify
                    (
                        loggingService => loggingService.LogErrorRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<Exception>(),
                            It.IsAny<IDictionary<string, object>>()
                        ),
                        Times.Once
                    );

                    Assert.AreEqual($"Unhandled exception {methodIdentifier}", messageObserved);
                    Assert.AreEqual(3, propertiesObserved.Count);
                    Assert.AreEqual(id, propertiesObserved["id"]);
                    Assert.AreEqual(key, propertiesObserved["key"]);
                    Assert.AreEqual((int)expectedElapsedMilliseconds, propertiesObserved["ElapsedMilliseconds"]);
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task FetchAsync_OnExceptionThrown_ThrowsException()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.FetchAsync)}";

                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogErrorRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<Exception>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, Exception exception, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();
                    var itemResponseMock = new Mock<ItemResponse<SampleModel>>();
                    itemResponseMock
                    .Setup(itemResponse => itemResponse.Resource)
                    .Returns(sampleModel);

                    containerMock
                    .Setup
                    (
                        container => container.ReadItemAsync<SampleModel>
                        (
                            It.IsAny<string>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Throws(new Exception());

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);


                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    var id = "1";
                    var key = "1";

                    //Act
                    var observed = await uut.FetchAsync<SampleModel>(id, key);

                    //Assert
                    loggingServiceMock
                    .Verify
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        ),
                        Times.Once
                    );

                    Assert.AreEqual($"Unhandled exception {methodIdentifier}", messageObserved);
                    Assert.AreEqual(3, propertiesObserved.Count);
                    Assert.AreEqual(id, propertiesObserved["id"]);
                    Assert.AreEqual(key, propertiesObserved["key"]);
                    Assert.AreEqual((int)expectedElapsedMilliseconds, propertiesObserved["ElapsedMilliseconds"]);
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task FetchAsync_Runs_TelemetryServiceInsertCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.FetchAsync)}";

                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });


                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();
                    var itemResponseMock = new Mock<ItemResponse<SampleModel>>();
                    itemResponseMock
                    .Setup(itemResponse => itemResponse.Resource)
                    .Returns(sampleModel);

                    containerMock
                    .Setup
                    (
                        container => container.ReadItemAsync<SampleModel>
                        (
                            It.IsAny<string>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(itemResponseMock.Object);

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);


                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    var id = "1";
                    var key = "1";

                    //Act
                    var observed = await uut.FetchAsync<SampleModel>(id, key);

                    //Assert
                    telemetryServiceMock
                    .Verify
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        ),
                        Times.Once
                    );

                    Assert.IsNotNull(telemetryDataObserved);

                    Assert.AreEqual(methodIdentifier, telemetryDataObserved.Name);
                    Assert.AreEqual(TelemetryType.NoSQL, telemetryDataObserved.TelemetryType);
                    Assert.AreEqual(expectedElapsedMilliseconds, telemetryDataObserved.ElapsedMilliseconds);
                    Assert.AreEqual(TelemetryState.Successful, telemetryDataObserved.TelemetryState);
                    Assert.AreEqual(expectedDateTime, telemetryDataObserved.DateTime);
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }


        #endregion

        #region InsertAsync

        [TestMethod]
        public async Task InsertAsync_Runs_GetDateTimeUTCCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.InsertAsync)}";

                    var key = "1";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();

                    containerMock
                    .Setup
                    (
                        container => container.CreateItemAsync<SampleModel>
                        (
                            It.IsAny<SampleModel>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Callback
                    (
                        (SampleModel item, PartitionKey? partitionKey, ItemRequestOptions requestOptions, CancellationToken cancellationToken) =>
                        {

                        }
                    );

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.InsertAsync(key, sampleModel);

                    //Assert
                    dateTimeServiceMock
                    .Verify
                    (
                        dateTimeService => dateTimeService.GetDateTimeUTC(),
                        Times.Once
                    );
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task InsertAsync_Runs_StopWatchStartCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.InsertAsync)}";

                    var id = "1";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();

                    containerMock
                    .Setup
                    (
                        container => container.CreateItemAsync<SampleModel>
                        (
                            It.IsAny<SampleModel>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Callback
                    (
                        (SampleModel item, PartitionKey? partitionKey, ItemRequestOptions requestOptions, CancellationToken cancellationToken) =>
                        {

                        }
                    );

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.InsertAsync(id, sampleModel);

                    //Assert
                    stopwatchServiceMock
                    .Verify
                    (
                        stopwatchService => stopwatchService.Start(),
                        Times.Once
                    );
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task InsertAsync_Runs_CreateItemAsyncCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.InsertAsync)}";

                    var key = "1";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();
                    var itemObserved = (SampleModel)null;
                    var partitionKeyObserved = (PartitionKey?)null;

                    containerMock
                    .Setup
                    (
                        container => container.CreateItemAsync<SampleModel>
                        (
                            It.IsAny<SampleModel>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Callback
                    (
                        (SampleModel item, PartitionKey? partitionKey, ItemRequestOptions requestOptions, CancellationToken cancellationToken) =>
                        {
                            itemObserved = item;
                            partitionKeyObserved = partitionKey;
                        }
                    );

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.InsertAsync(key, sampleModel);

                    //Assert
                    //Assert
                    Assert.IsNotNull(itemObserved);
                    Assert.IsNotNull(partitionKeyObserved);

                    containerMock
                    .Verify
                    (
                        container => container.CreateItemAsync
                        (
                                It.IsAny<SampleModel>(),
                                (PartitionKey)partitionKeyObserved,
                                It.IsAny<ItemRequestOptions>(),
                                It.IsAny<CancellationToken>()
                        ),
                        Times.Once
                    );

                    Assert.AreEqual(sampleModel, itemObserved);
                    Assert.AreEqual(new PartitionKey(key).ToString(), partitionKeyObserved.Value.ToString());
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task InsertAsync_Runs_StopWatchStopCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.InsertAsync)}";

                    var key = "1";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();

                    containerMock
                    .Setup
                    (
                        container => container.CreateItemAsync<SampleModel>
                        (
                            It.IsAny<SampleModel>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Callback
                    (
                        (SampleModel item, PartitionKey? partitionKey, ItemRequestOptions requestOptions, CancellationToken cancellationToken) =>
                        {

                        }
                    );

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.InsertAsync(key, sampleModel);

                    //Assert
                    stopwatchServiceMock
                    .Verify
                    (
                        stopwatchService => stopwatchService.Stop(),
                        Times.Once
                    );
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task InsertAsync_Runs_LogInformationRedactedCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.InsertAsync)}";

                    var key = "1";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();

                    containerMock
                    .Setup
                    (
                        container => container.CreateItemAsync<SampleModel>
                        (
                            It.IsAny<SampleModel>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Callback
                    (
                        (SampleModel item, PartitionKey? partitionKey, ItemRequestOptions requestOptions, CancellationToken cancellationToken) =>
                        {

                        }
                    );

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.InsertAsync(key, sampleModel);

                    //Assert
                    loggingServiceMock
                    .Verify
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        ),
                        Times.Once
                    );

                    Assert.AreEqual(methodIdentifier, messageObserved);
                    Assert.AreEqual(3, propertiesObserved.Count);
                    Assert.AreEqual(key, propertiesObserved["key"]);
                    Assert.AreEqual(sampleModel, propertiesObserved["value"]);
                    Assert.AreEqual((int)expectedElapsedMilliseconds, propertiesObserved["ElapsedMilliseconds"]);
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task InsertAsync_Runs_LogErrorRedactedCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.InsertAsync)}";

                    var key = "1";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();

                    containerMock
                    .Setup
                    (
                        container => container.CreateItemAsync<SampleModel>
                        (
                            It.IsAny<SampleModel>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Throws(new Exception());

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.InsertAsync(key, sampleModel);

                    //Assert
                    loggingServiceMock
                    .Verify
                    (
                        loggingService => loggingService.LogErrorRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<Exception>(),
                            It.IsAny<IDictionary<string, object>>()
                        ),
                        Times.Once
                    );

                    Assert.AreEqual($"Unhandled exception {methodIdentifier}", messageObserved);
                    Assert.AreEqual(3, propertiesObserved.Count);
                    Assert.AreEqual(key, propertiesObserved["key"]);
                    Assert.AreEqual(sampleModel, propertiesObserved["value"]);
                    Assert.AreEqual((int)expectedElapsedMilliseconds, propertiesObserved["ElapsedMilliseconds"]);
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task InsertAsync_OnExceptionThrown_ThrowsException()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.InsertAsync)}";

                    var key = "1";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();

                    containerMock
                    .Setup
                    (
                        container => container.CreateItemAsync<SampleModel>
                        (
                            It.IsAny<SampleModel>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Throws(new Exception());

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.InsertAsync(key, sampleModel);

                    //Assert
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task InsertAsync_Runs_TelemetryServiceInsert()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.InsertAsync)}";

                    var id = "1";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();

                    containerMock
                    .Setup
                    (
                        container => container.CreateItemAsync<SampleModel>
                        (
                            It.IsAny<SampleModel>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Callback
                    (
                        (SampleModel item, PartitionKey? partitionKey, ItemRequestOptions requestOptions, CancellationToken cancellationToken) =>
                        {

                        }
                    );

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.InsertAsync(id, sampleModel);

                    //Assert
                    telemetryServiceMock
                    .Verify
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        ),
                        Times.Once
                    );

                    Assert.IsNotNull(telemetryDataObserved);

                    Assert.AreEqual(methodIdentifier, telemetryDataObserved.Name);
                    Assert.AreEqual(TelemetryType.NoSQL, telemetryDataObserved.TelemetryType);
                    Assert.AreEqual(expectedElapsedMilliseconds, telemetryDataObserved.ElapsedMilliseconds);
                    Assert.AreEqual(TelemetryState.Successful, telemetryDataObserved.TelemetryState);
                    Assert.AreEqual(expectedDateTime, telemetryDataObserved.DateTime);
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }


        #endregion

        #region UpsertAsync

        [TestMethod]
        public async Task UpsertAsync_Runs_GetDateTimeUTCCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.UpsertAsync)}";

                    var key = "1";
                    var eTag = "1";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();

                    containerMock
                    .Setup
                    (
                        container => container.UpsertItemAsync
                        (
                            It.IsAny<SampleModel>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Callback
                    (
                        (SampleModel item, PartitionKey? partitionKey, ItemRequestOptions requestOptions, CancellationToken cancellationToken) =>
                        {

                        }
                    );

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.UpsertAsync(key, eTag, sampleModel);

                    //Assert
                    dateTimeServiceMock
                    .Verify
                    (
                        dateTimeService => dateTimeService.GetDateTimeUTC(),
                        Times.Once
                    );
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task UpsertAsync_Runs_StopWatchStartCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.UpsertAsync)}";

                    var id = "1";
                    var eTag = "1";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();

                    containerMock
                    .Setup
                    (
                        container => container.UpsertItemAsync
                        (
                            It.IsAny<SampleModel>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Callback
                    (
                        (SampleModel item, PartitionKey? partitionKey, ItemRequestOptions requestOptions, CancellationToken cancellationToken) =>
                        {

                        }
                    );

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.UpsertAsync(id, eTag, sampleModel);

                    //Assert
                    stopwatchServiceMock
                    .Verify
                    (
                        stopwatchService => stopwatchService.Start(),
                        Times.Once
                    );
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task UpsertAsync_Runs_CreateItemAsyncCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.UpsertAsync)}";

                    var key = "1";
                    var eTag = "1";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();
                    var itemObserved = (SampleModel)null;
                    var partitionKeyObserved = (PartitionKey?)null;

                    containerMock
                    .Setup
                    (
                        container => container.UpsertItemAsync
                        (
                            It.IsAny<SampleModel>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Callback
                    (
                        (SampleModel item, PartitionKey? partitionKey, ItemRequestOptions requestOptions, CancellationToken cancellationToken) =>
                        {
                            itemObserved = item;
                            partitionKeyObserved = partitionKey;
                        }
                    );

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.UpsertAsync(key, eTag, sampleModel);

                    //Assert
                    //Assert
                    Assert.IsNotNull(itemObserved);
                    Assert.IsNotNull(partitionKeyObserved);

                    containerMock
                    .Verify
                    (
                        container => container.UpsertItemAsync
                        (
                                It.IsAny<SampleModel>(),
                                (PartitionKey)partitionKeyObserved,
                                It.IsAny<ItemRequestOptions>(),
                                It.IsAny<CancellationToken>()
                        ),
                        Times.Once
                    );

                    Assert.AreEqual(sampleModel, itemObserved);
                    Assert.AreEqual(new PartitionKey(key).ToString(), partitionKeyObserved.Value.ToString());
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task UpsertAsync_Runs_StopWatchStopCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.UpsertAsync)}";

                    var key = "1";
                    var eTag = "1";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();
                    var itemObserved = (SampleModel)null;
                    var partitionKeyObserved = (PartitionKey?)null;

                    containerMock
                    .Setup
                    (
                        container => container.UpsertItemAsync
                        (
                            It.IsAny<SampleModel>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Callback
                    (
                        (SampleModel item, PartitionKey? partitionKey, ItemRequestOptions requestOptions, CancellationToken cancellationToken) =>
                        {
                            itemObserved = item;
                            partitionKeyObserved = partitionKey;
                        }
                    );

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.UpsertAsync(key, eTag, sampleModel);

                    //Assert
                    stopwatchServiceMock
                    .Verify
                    (
                        stopwatchService => stopwatchService.Stop(),
                        Times.Once
                    );
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task UpsertAsync_Runs_LogInformationRedactedCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.UpsertAsync)}";

                    var key = "1";
                    var eTag = "1";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();
                    var itemObserved = (SampleModel)null;
                    var partitionKeyObserved = (PartitionKey?)null;

                    containerMock
                    .Setup
                    (
                        container => container.UpsertItemAsync
                        (
                            It.IsAny<SampleModel>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Callback
                    (
                        (SampleModel item, PartitionKey? partitionKey, ItemRequestOptions requestOptions, CancellationToken cancellationToken) =>
                        {
                            itemObserved = item;
                            partitionKeyObserved = partitionKey;
                        }
                    );

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.UpsertAsync(key, eTag, sampleModel);

                    //Assert
                    loggingServiceMock
                    .Verify
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        ),
                        Times.Once
                    );

                    Assert.AreEqual(methodIdentifier, messageObserved);
                    Assert.AreEqual(3, propertiesObserved.Count);
                    Assert.AreEqual(key, propertiesObserved["key"]);
                    Assert.AreEqual(sampleModel, propertiesObserved["value"]);
                    Assert.AreEqual((int)expectedElapsedMilliseconds, propertiesObserved["ElapsedMilliseconds"]);
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task UpsertAsync_OnExceptionThrown_LogErrorRedactedCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.UpsertAsync)}";

                    var key = "1";
                    var eTag = "1";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();

                    containerMock
                    .Setup
                    (
                        container => container.UpsertItemAsync
                        (
                            It.IsAny<SampleModel>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Throws(new Exception());

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.UpsertAsync(key, eTag, sampleModel);

                    //Assert

                    //Assert
                    loggingServiceMock
                    .Verify
                    (
                        loggingService => loggingService.LogErrorRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<Exception>(),
                            It.IsAny<IDictionary<string, object>>()
                        ),
                        Times.Once
                    );

                    Assert.AreEqual($"Unhandled exception {methodIdentifier}", messageObserved);
                    Assert.AreEqual(3, propertiesObserved.Count);
                    Assert.AreEqual(key, propertiesObserved["key"]);
                    Assert.AreEqual(sampleModel, propertiesObserved["value"]);
                    Assert.AreEqual((int)expectedElapsedMilliseconds, propertiesObserved["ElapsedMilliseconds"]);
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task UpsertAsync_OnExceptionThrown_ThrowsException()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.UpsertAsync)}";

                    var key = "1";
                    var eTag = "1";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();

                    containerMock
                    .Setup
                    (
                        container => container.UpsertItemAsync
                        (
                            It.IsAny<SampleModel>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Throws(new Exception());

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.UpsertAsync(key, eTag, sampleModel);

                    //Assert
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        [ExpectedException(typeof(CosmosException))]
        public async Task UpsertAsync_OnCosmosExceptionWithPreconditionFailedThrown_LogErrorRedactedCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.UpsertAsync)}";

                    var key = "1";
                    var eTag = "1";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();

                    containerMock
                    .Setup
                    (
                        container => container.UpsertItemAsync
                        (
                            It.IsAny<SampleModel>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Throws(new CosmosException("", System.Net.HttpStatusCode.PreconditionFailed, 0, "", 0));

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.UpsertAsync(key, eTag, sampleModel);

                    //Assert

                    //Assert
                    loggingServiceMock
                    .Verify
                    (
                        loggingService => loggingService.LogErrorRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<Exception>(),
                            It.IsAny<IDictionary<string, object>>()
                        ),
                        Times.Once
                    );

                    Assert.AreEqual($"PreconditionFailed {methodIdentifier}", messageObserved);
                    Assert.AreEqual(3, propertiesObserved.Count);
                    Assert.AreEqual(key, propertiesObserved["key"]);
                    Assert.AreEqual(sampleModel, propertiesObserved["value"]);
                    Assert.AreEqual((int)expectedElapsedMilliseconds, propertiesObserved["ElapsedMilliseconds"]);
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        [ExpectedException(typeof(CosmosException))]
        public async Task UpsertAsync_OnCosmosExceptionWithPreconditionFailedThrown_ThrowsException()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.UpsertAsync)}";

                    var key = "1";
                    var eTag = "1";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();

                    containerMock
                    .Setup
                    (
                        container => container.UpsertItemAsync
                        (
                            It.IsAny<SampleModel>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Throws(new CosmosException("", System.Net.HttpStatusCode.PreconditionFailed, 0, "", 0));

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.UpsertAsync(key, eTag, sampleModel);

                    //Assert
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task UpsertAsync_Runs_TelemetryServiceInsert()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.UpsertAsync)}";

                    var key = "1";
                    var eTag = "1";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();
                    var itemObserved = (SampleModel)null;
                    var partitionKeyObserved = (PartitionKey?)null;

                    containerMock
                    .Setup
                    (
                        container => container.UpsertItemAsync
                        (
                            It.IsAny<SampleModel>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Callback
                    (
                        (SampleModel item, PartitionKey? partitionKey, ItemRequestOptions requestOptions, CancellationToken cancellationToken) =>
                        {
                            itemObserved = item;
                            partitionKeyObserved = partitionKey;
                        }
                    );

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.UpsertAsync(key, eTag, sampleModel);

                    //Assert
                    telemetryServiceMock
                    .Verify
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        ),
                        Times.Once
                    );

                    Assert.IsNotNull(telemetryDataObserved);

                    Assert.AreEqual(methodIdentifier, telemetryDataObserved.Name);
                    Assert.AreEqual(TelemetryType.NoSQL, telemetryDataObserved.TelemetryType);
                    Assert.AreEqual(expectedElapsedMilliseconds, telemetryDataObserved.ElapsedMilliseconds);
                    Assert.AreEqual(TelemetryState.Successful, telemetryDataObserved.TelemetryState);
                    Assert.AreEqual(expectedDateTime, telemetryDataObserved.DateTime);
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }


        #endregion

        #region DeleteAsync

        [TestMethod]
        public async Task DeleteAsync_Runs_GetDateTimeUTCCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.DeleteAsync)}";

                    var key = "1";
                    var id = "2";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();
                    var idObserved = (string)null;
                    var partitionKeyObserved = (PartitionKey?)null;

                    containerMock
                    .Setup
                    (
                        container => container.DeleteItemAsync<object>
                        (
                            It.IsAny<string>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Callback
                    (
                        (string item, PartitionKey? partitionKey, ItemRequestOptions requestOptions, CancellationToken cancellationToken) =>
                        {
                            idObserved = id;
                            partitionKeyObserved = partitionKey;
                        }
                    );

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.DeleteAsync(id, key);

                    //Assert
                    dateTimeServiceMock
                    .Verify
                    (
                        dateTimeService => dateTimeService.GetDateTimeUTC(),
                        Times.Once
                    );
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task DeleteAsync_Runs_StopWatchStartCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.DeleteAsync)}";

                    var id = "1";
                    var key = "2";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();
                    var idObserved = (string)null;
                    var partitionKeyObserved = (PartitionKey?)null;

                    containerMock
                    .Setup
                    (
                        container => container.DeleteItemAsync<object>
                        (
                            It.IsAny<string>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Callback
                    (
                        (string item, PartitionKey? partitionKey, ItemRequestOptions requestOptions, CancellationToken cancellationToken) =>
                        {
                            idObserved = id;
                            partitionKeyObserved = partitionKey;
                        }
                    );

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);
            
                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.DeleteAsync(id, key);

                    //Assert
                    stopwatchServiceMock
                    .Verify
                    (
                        stopwatchService => stopwatchService.Start(),
                        Times.Once
                    );
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task DeleteAsync_Runs_CreateItemAsyncCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.DeleteAsync)}";

                    var key = "1";
                    var id = "2";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();
                    var idObserved = (string)null;
                    var partitionKeyObserved = (PartitionKey?)null;

                    containerMock
                    .Setup
                    (
                        container => container.DeleteItemAsync<object>
                        (
                            It.IsAny<string>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Callback
                    (
                        (string item, PartitionKey? partitionKey, ItemRequestOptions requestOptions, CancellationToken cancellationToken) =>
                        {
                            idObserved = id;
                            partitionKeyObserved = partitionKey;
                        }
                    );

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.DeleteAsync(id, key);

                    //Assert
                    //Assert
                    Assert.IsNotNull(id);
                    Assert.IsNotNull(partitionKeyObserved);

                    containerMock
                    .Verify
                    (
                        container => container.DeleteItemAsync<object>
                        (
                            It.IsAny<string>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        ),
                        Times.Once
                    );

                    Assert.AreEqual(id, idObserved);
                    Assert.AreEqual(new PartitionKey(key).ToString(), partitionKeyObserved.Value.ToString());
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task DeleteAsync_Runs_StopWatchStopCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.DeleteAsync)}";

                    var key = "1";
                    var id = "2";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();
                    var idObserved = (string)null;
                    var partitionKeyObserved = (PartitionKey?)null;

                    containerMock
                    .Setup
                    (
                        container => container.DeleteItemAsync<object>
                        (
                            It.IsAny<string>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Callback
                    (
                        (string item, PartitionKey? partitionKey, ItemRequestOptions requestOptions, CancellationToken cancellationToken) =>
                        {
                            idObserved = id;
                            partitionKeyObserved = partitionKey;
                        }
                    );

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.DeleteAsync(id, key);

                    //Assert
                    stopwatchServiceMock
                    .Verify
                    (
                        stopwatchService => stopwatchService.Stop(),
                        Times.Once
                    );
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task DeleteAsync_Runs_LogInformationRedactedCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.DeleteAsync)}";

                    var key = "1";
                    var id = "2";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();
                    var idObserved = (string)null;
                    var partitionKeyObserved = (PartitionKey?)null;

                    containerMock
                    .Setup
                    (
                        container => container.DeleteItemAsync<object>
                        (
                            It.IsAny<string>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Callback
                    (
                        (string item, PartitionKey? partitionKey, ItemRequestOptions requestOptions, CancellationToken cancellationToken) =>
                        {
                            idObserved = id;
                            partitionKeyObserved = partitionKey;
                        }
                    );

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.DeleteAsync(id, key);

                    //Assert
                    loggingServiceMock
                    .Verify
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        ),
                        Times.Once
                    );

                    Assert.AreEqual(methodIdentifier, messageObserved);
                    Assert.AreEqual(3, propertiesObserved.Count);
                    Assert.AreEqual(key, propertiesObserved["key"]);
                    Assert.AreEqual(id, propertiesObserved["id"]);
                    Assert.AreEqual((int)expectedElapsedMilliseconds, propertiesObserved["ElapsedMilliseconds"]);
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task DeleteAsync_OnExceptionThrown_LogErrorRedactedCalled()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.DeleteAsync)}";

                    var key = "1";
                    var id = "2";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();

                    containerMock
                    .Setup
                    (
                        container => container.DeleteItemAsync<object>
                        (
                            It.IsAny<string>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Throws(new Exception());

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.DeleteAsync(id, key);

                    //Assert

                    //Assert
                    loggingServiceMock
                    .Verify
                    (
                        loggingService => loggingService.LogErrorRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<Exception>(),
                            It.IsAny<IDictionary<string, object>>()
                        ),
                        Times.Once
                    );

                    Assert.AreEqual($"Unhandled exception {methodIdentifier}", messageObserved);
                    Assert.AreEqual(3, propertiesObserved.Count);
                    Assert.AreEqual(key, propertiesObserved["key"]);
                    Assert.AreEqual(id, propertiesObserved["id"]);
                    Assert.AreEqual((int)expectedElapsedMilliseconds, propertiesObserved["ElapsedMilliseconds"]);
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task DeleteAsync_OnExceptionThrown_ThrowsException()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.DeleteAsync)}";

                    var key = "1";
                    var id = "2";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();

                    containerMock
                    .Setup
                    (
                        container => container.DeleteItemAsync<object>
                        (
                            It.IsAny<string>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                   .Throws(new Exception());

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.DeleteAsync(id, key);

                    //Assert
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }

        [TestMethod]
        public async Task DeleteAsync_Runs_TelemetryServiceInsert()
        {
            await RunDependencyInjectedTestAsync
            (
                async (serviceProvider) =>
                {
                    //Setup
                    var sampleModel = new SampleModel
                    {
                        coasterData = "abc"
                    };

                    var methodIdentifier = $"{nameof(CosmosService<SampleCosmosServiceOptions>)}.{nameof(CosmosService<SampleCosmosServiceOptions>.DeleteAsync)}";

                    var id = "1";
                    var key = "2";
                    var expectedDateTime = new System.DateTime(2020, 1, 1);
                    var expectedElapsedMilliseconds = (long)5000;
                    var loggingServiceMock = serviceProvider.GetMock<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>();
                    var messageObserved = (string)null;
                    var propertiesObserved = (IDictionary<string, object>)null;

                    //-Logger
                    loggingServiceMock
                    .Setup
                    (
                        loggingService => loggingService.LogInformationRedacted
                        (
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, object>>()
                        )
                    )
                    .Callback((string message, IDictionary<string, object> properties) =>
                    {
                        messageObserved = message;
                        propertiesObserved = properties;
                    });

                    //-DateTime
                    var dateTimeServiceMock = serviceProvider.GetMock<IDateTimeService>();
                    dateTimeServiceMock
                    .Setup(dateTimeService => dateTimeService.GetDateTimeUTC())
                    .Returns(expectedDateTime);

                    //-StopWatch
                    var stopwatchServiceMock = serviceProvider.GetMock<IStopwatchService>();
                    stopwatchServiceMock
                    .SetupGet(stopwatchService => stopwatchService.ElapsedMilliseconds)
                    .Returns(expectedElapsedMilliseconds);

                    //-CosmosContainer
                    var cosmosClientMock = serviceProvider.GetMock<CosmosClient>();
                    var containerMock = serviceProvider.GetMock<Container>();
                    var idObserved = (string)null;
                    var partitionKeyObserved = (PartitionKey?)null;

                    containerMock
                    .Setup
                    (
                        container => container.DeleteItemAsync<object>
                        (
                            It.IsAny<string>(),
                            It.IsAny<PartitionKey>(),
                            It.IsAny<ItemRequestOptions>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .Callback
                    (
                        (string item, PartitionKey? partitionKey, ItemRequestOptions requestOptions, CancellationToken cancellationToken) =>
                        {
                            idObserved = id;
                            partitionKeyObserved = partitionKey;
                        }
                    );

                    cosmosClientMock
                    .Setup
                    (
                        cosmosClient => cosmosClient.GetContainer
                        (
                            It.IsAny<string>(),
                            It.IsAny<string>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //--ICosmosFactory
                    var cosmosFactoryMock = serviceProvider.GetMock<ICosmosFactory>();

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.CreateCosmosClient
                        (
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(cosmosClientMock.Object);

                    cosmosFactoryMock
                    .Setup
                    (
                        cosmosFactory => cosmosFactory.GetContainer
                        (
                            It.IsAny<CosmosClient>(),
                            It.IsAny<CosmosServiceOptions>()
                        )
                    )
                    .Returns(containerMock.Object);

                    //-Telemetry
                    var telemetryServiceMock = serviceProvider.GetMock<ITelemetryService>();
                    var telemetryDataObserved = (TelemetryData)null;
                    telemetryServiceMock.Setup
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        )
                    )
                    .Callback((TelemetryData telemetryData) =>
                    {
                        telemetryDataObserved = telemetryData;
                    });

                    //-UUT
                    var uut = serviceProvider.GetRequiredService<ICosmosService<SampleCosmosServiceOptions>>();
                    var uutConcrete = (CosmosService<SampleCosmosServiceOptions>)uut;

                    //Act
                    await uut.DeleteAsync(id, key);

                    //Assert
                    telemetryServiceMock
                    .Verify
                    (
                        telemetryService => telemetryService.Insert
                        (
                            It.IsAny<TelemetryData>()
                        ),
                        Times.Once
                    );

                    Assert.IsNotNull(telemetryDataObserved);

                    Assert.AreEqual(methodIdentifier, telemetryDataObserved.Name);
                    Assert.AreEqual(TelemetryType.NoSQL, telemetryDataObserved.TelemetryType);
                    Assert.AreEqual(expectedElapsedMilliseconds, telemetryDataObserved.ElapsedMilliseconds);
                    Assert.AreEqual(TelemetryState.Successful, telemetryDataObserved.TelemetryState);
                    Assert.AreEqual(expectedDateTime, telemetryDataObserved.DateTime);
                },
               serviceCollection => ConfigureServices(serviceCollection)
           );
        }


        #endregion

        #region Helpers

        private IServiceCollection ConfigureServices(IServiceCollection serviceCollection)
        {
           serviceCollection.AddSingleton<ICosmosService<SampleCosmosServiceOptions>, CosmosService<SampleCosmosServiceOptions>>();
            serviceCollection.AddSingleton(Mock.Of<Container>());
            serviceCollection.AddSingleton(Mock.Of<CosmosClient>());
            serviceCollection.AddSingleton(Mock.Of<ITelemetryService>());
            serviceCollection.AddSingleton(Mock.Of<ICosmosFactory>());
            serviceCollection.AddSingleton(Mock.Of<IDateTimeService>());
            serviceCollection.AddSingleton(Mock.Of<ILoggingService<CosmosService<SampleCosmosServiceOptions>>>());
            serviceCollection.AddSingleton(Mock.Of<IStopwatchService>());

            var cosmosServiceOptions = new SampleCosmosServiceOptions
            {
                ConnectionString = "",
                ContainerId = "",
                DatabaseId = "",
                EndpointUri = "",
                PrimaryKey = ""
            };
            var options = Options.Create(cosmosServiceOptions);
            serviceCollection.AddSingleton<IOptions<SampleCosmosServiceOptions>>(options);
            return serviceCollection;
        }
        #endregion
    }
}








//        //Add Fail Log Fail

//        //Add Fail Log Fail

//        //Add GetDateTimeUTC


//        //Add Result Check on Run

//        //Get Stopwatch
//        //Start
//        //ReadItem
//        //Stop
//        //Log Info
//        //Return
//        //Telemetry Insert

//        //Get Stopwatch
//        //Start
//        //ReadItem
//        //Stop
//        //Log Error
//        //Throw
//        //Telemetry Insert