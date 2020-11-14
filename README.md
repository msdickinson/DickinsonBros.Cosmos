# DickinsonBros.CosmosService
<a href="https://dev.azure.com/marksamdickinson/dickinsonbros/_build/latest?definitionId=72&amp;branchName=master"> <img alt="Azure DevOps builds (branch)" src="https://img.shields.io/azure-devops/build/marksamdickinson/DickinsonBros/72/master"> </a> <a href="https://dev.azure.com/marksamdickinson/dickinsonbros/_build/latest?definitionId=72&amp;branchName=master"> <img alt="Azure DevOps coverage (branch)" src="https://img.shields.io/azure-devops/coverage/marksamdickinson/dickinsonbros/72/master"> </a><a href="https://dev.azure.com/marksamdickinson/DickinsonBros/_release?_a=releases&view=mine&definitionId=34"> <img alt="Azure DevOps releases" src="https://img.shields.io/azure-devops/release/marksamdickinson/b5a46403-83bb-4d18-987f-81b0483ef43e/34/35"> </a><a href="https://www.nuget.org/packages/DickinsonBros.Cosmos/"><img src="https://img.shields.io/nuget/v/DickinsonBros.Cosmos"></a>

A CosmosService service

Features
* DeleteAsync, FetchAsync, InsertAsync, and UpsertAsync Methods 
* Captures Telemetry
* Adds Logging

<h2>Example Usage</h2>

```C#
  var noSQLService = provider.GetRequiredService<INoSQLService>();

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
```

```
    info: DickinsonBros.CosmosService.CosmosService[1]
      CosmosService.InsertAsync
      key: 8f5c0bba-9c17-4069-a809-0d34fabf6868
      value: {
        "key": "8f5c0bba-9c17-4069-a809-0d34fabf6868",
        "id": "8f5c0bba-9c17-4069-a809-0d34fabf6868",
        "coasterData": "***REDACTED***"
      }
      ElapsedMilliseconds: 2342

  info: DickinsonBros.CosmosService.CosmosService[1]
      CosmosService.UpsertAsync
      key: 8f5c0bba-9c17-4069-a809-0d34fabf6868
      value: {
        "key": "8f5c0bba-9c17-4069-a809-0d34fabf6868",
        "id": "8f5c0bba-9c17-4069-a809-0d34fabf6868",
        "coasterData": "***REDACTED***"
      }
      ElapsedMilliseconds: 76

info: DickinsonBros.CosmosService.CosmosService[1]
      CosmosService.FetchAsync
      id: 8f5c0bba-9c17-4069-a809-0d34fabf6868
      key: 8f5c0bba-9c17-4069-a809-0d34fabf6868
      result: {
        "Headers": [
          "x-ms-request-charge",
          "x-ms-activity-id",
          "etag",
          "x-ms-session-token",
          "x-ms-last-state-change-utc",
          "x-ms-resource-quota",
          "x-ms-resource-usage",
          "lsn",
          "x-ms-schemaversion",
          "x-ms-alt-content-path",
          "x-ms-content-path",
          "x-ms-xp-role",
          "x-ms-global-Committed-lsn",
          "x-ms-number-of-read-regions",
          "x-ms-item-lsn",
          "x-ms-transport-request-id",
          "x-ms-cosmos-llsn",
          "x-ms-cosmos-item-llsn",
          "x-ms-serviceversion"
        ],
        "Resource": {
          "key": "8f5c0bba-9c17-4069-a809-0d34fabf6868",
          "id": "8f5c0bba-9c17-4069-a809-0d34fabf6868",
          "coasterData": "***REDACTED***"
        },
        "StatusCode": 200,
        "Diagnostics": {},
        "RequestCharge": 1.0,
        "ActivityId": "d6c730fb-37a4-400d-a767-a7b5694ad30c",
        "ETag": "\"ba007157-0000-0200-0000-5f59b2c30000\""
      }
      ElapsedMilliseconds: 338

info: DickinsonBros.CosmosService.CosmosService[1]
      CosmosService.DeleteAsync
      id: 8f5c0bba-9c17-4069-a809-0d34fabf6868
      key: 8f5c0bba-9c17-4069-a809-0d34fabf6868
      ElapsedMilliseconds: 85


sampleModelValue: {"key":"8f5c0bba-9c17-4069-a809-0d34fabf6868","id":"8f5c0bba-9c17-4069-a809-0d34fabf6868","coasterData":"7009d4fc-5987-42b7-bbdc-2ebb5d1721e7"}
fetchedSampleModel: {"key":"8f5c0bba-9c17-4069-a809-0d34fabf6868","id":"8f5c0bba-9c17-4069-a809-0d34fabf6868","coasterData":"7009d4fc-5987-42b7-bbdc-2ebb5d1721e7"}
```
Note: coasterData is REDACTED via configuration in the Sample runner as its a large file and would overload logger.

<h2>Telemetry</h2>

![Alt text](https://raw.githubusercontent.com/msdickinson/DickinsonBros.CosmosService/master/Telemetry.PNG)

<h2>Example Cosmos Item</h2>

![Alt text](https://raw.githubusercontent.com/msdickinson/DickinsonBros.CosmosService/master/CosmosSampleItem.PNG)
