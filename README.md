CosmosDb Job Store for Quartz.NET
[![Downloads](https://img.shields.io/nuget/dt/Ozone3.Quartz.CosmosDbJobStore.svg)](https://www.nuget.org/packages/Ozone3.Quartz.CosmosDbJobStore/)
================================

A distributed job store implementation for Quartz.NET that uses Azure CosmosDB as the backing store. This enables you to run Quartz.NET in a clustered environment with Azure CosmosDB as the central job repository.

> **Note:** This is a fork of the original [Oriflame/cosmosdb-quartznet](https://github.com/Oriflame/cosmosdb-quartznet) project by [Frantisek Jandos](https://github.com/frantisekvasa). We're grateful for the original work and continue to maintain and enhance this library.

Originally based on [Quartz.NET MongoDb Job Store](https://github.com/chrisdrobison/mongodb-quartz-net), this implementation targets the Microsoft.Azure.Cosmos v3 SDK.

## Features

- ✅ **.NET 8 Support** - Built for the latest .NET platform
- ✅ **Quartz.NET 3.15.1** - Latest stable version of Quartz.NET
- ✅ **Azure Cosmos DB SDK 3.54.0** - Latest Azure Cosmos DB client library
- ✅ **Clustering Support** - Run multiple scheduler instances safely
- ✅ **Distributed Locking** - Built-in distributed lock mechanism using Cosmos DB
- ✅ **Flexible Partitioning** - Partition by instance name or entity type
- ✅ **JSON Serialization** - Efficient job data serialization
- ✅ **Connection Modes** - Support for both Direct and Gateway connection modes

## Requirements

- .NET 8.0 or higher
- Azure Cosmos DB account
- Quartz.NET 3.15.1 or higher

## Installation

### Via NuGet Package Manager
```
Install-Package Ozone3.Quartz.CosmosDbJobStore
```

### Via .NET CLI
```bash
dotnet add package Ozone3.Quartz.CosmosDbJobStore
```

## Quick Start

### Basic Configuration

```csharp
using Quartz;
using Quartz.Impl;
using Quartz.Spi.CosmosDbJobStore;
using System.Collections.Specialized;

var properties = new NameValueCollection
{
    // Scheduler configuration
    ["quartz.scheduler.instanceName"] = "MyScheduler",
    ["quartz.scheduler.instanceId"] = $"{Environment.MachineName}-{Guid.NewGuid()}",

    // Job store configuration
    ["quartz.jobStore.type"] = typeof(CosmosDbJobStore).AssemblyQualifiedName,
    ["quartz.serializer.type"] = "json",

    // Cosmos DB connection
    ["quartz.jobStore.Endpoint"] = "https://your-account.documents.azure.com:443/",
    ["quartz.jobStore.Key"] = "your-cosmos-db-key",
    ["quartz.jobStore.DatabaseId"] = "quartz-db",
    ["quartz.jobStore.CollectionId"] = "quartz-jobs",

    // Clustering
    ["quartz.jobStore.clustered"] = "true"
};

var schedulerFactory = new StdSchedulerFactory(properties);
var scheduler = await schedulerFactory.GetScheduler();
await scheduler.Start();
```

### Using Local Cosmos DB Emulator

For development, you can use the Azure Cosmos DB Emulator:

```csharp
properties["quartz.jobStore.Endpoint"] = "https://localhost:8081/";
properties["quartz.jobStore.Key"] = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
```

## Configuration Options

### Core Settings

| Property | Description | Default | Required |
|----------|-------------|---------|----------|
| `quartz.jobStore.Endpoint` | Cosmos DB endpoint URL | - | Yes |
| `quartz.jobStore.Key` | Cosmos DB access key | - | Yes |
| `quartz.jobStore.DatabaseId` | Database name | - | Yes |
| `quartz.jobStore.CollectionId` | Container name | - | Yes |
| `quartz.jobStore.clustered` | Enable clustering | `false` | No |

### Advanced Settings

#### Partition Strategy

By default, the collection is partitioned by scheduler instance name. You can partition by entity type instead:

```csharp
properties["quartz.jobStore.PartitionPerEntityType"] = "true";
```

**When to use:**
- **Instance Name Partitioning (default)**: Best for multiple independent scheduler clusters
- **Entity Type Partitioning**: Best for analyzing job data by type

#### Connection Mode

Choose between Direct (TCP) or Gateway (HTTPS) connection modes:

```csharp
// Direct mode (default - better performance)
properties["quartz.jobStore.ConnectionMode"] = "0";

// Gateway mode (firewall-friendly)
properties["quartz.jobStore.ConnectionMode"] = "1";
```

#### Lock Configuration

```csharp
// Lock timeout in seconds (default: 30)
properties["quartz.jobStore.LockTimeout"] = "60";
```

## ASP.NET Core Integration

### Using Dependency Injection

```csharp
using Quartz;
using Quartz.Spi.CosmosDbJobStore;

var builder = WebApplication.CreateBuilder(args);

// Add Quartz services
builder.Services.AddQuartz(q =>
{
    // Use unique instance ID
    q.SchedulerId = $"AUTO-{Guid.NewGuid()}";

    // Use Cosmos DB job store
    q.UsePersistentStore(options =>
    {
        options.UseProperties = true;
        options.UseJsonSerializer();

        // Configure Cosmos DB
        options.SetProperty("quartz.jobStore.type", typeof(CosmosDbJobStore).AssemblyQualifiedName);
        options.SetProperty("quartz.jobStore.Endpoint", builder.Configuration["CosmosDb:Endpoint"]);
        options.SetProperty("quartz.jobStore.Key", builder.Configuration["CosmosDb:Key"]);
        options.SetProperty("quartz.jobStore.DatabaseId", "quartz-db");
        options.SetProperty("quartz.jobStore.CollectionId", "quartz-jobs");
        options.SetProperty("quartz.jobStore.clustered", "true");
    });
});

// Add Quartz hosted service
builder.Services.AddQuartzHostedService(options =>
{
    options.WaitForJobsToComplete = true;
});

var app = builder.Build();
app.Run();
```

### appsettings.json Configuration

```json
{
  "CosmosDb": {
    "Endpoint": "https://your-account.documents.azure.com:443/",
    "Key": "your-cosmos-db-key",
    "DatabaseId": "quartz-db",
    "CollectionId": "quartz-jobs"
  }
}
```

## Creating Jobs

### Simple Job Example

```csharp
public class HelloJob : IJob
{
    private readonly ILogger<HelloJob> _logger;

    public HelloJob(ILogger<HelloJob> logger)
    {
        _logger = logger;
    }

    public Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Hello from Quartz.NET!");
        return Task.CompletedTask;
    }
}

// Schedule the job
var job = JobBuilder.Create<HelloJob>()
    .WithIdentity("helloJob", "group1")
    .Build();

var trigger = TriggerBuilder.Create()
    .WithIdentity("helloTrigger", "group1")
    .StartNow()
    .WithSimpleSchedule(x => x
        .WithIntervalInSeconds(10)
        .RepeatForever())
    .Build();

await scheduler.ScheduleJob(job, trigger);
```

## Clustering Setup

For a clustered environment, ensure each instance has:

1. **Unique Instance ID**: Use machine name + GUID
2. **Clustering Enabled**: Set `clustered = true`
3. **Same Database**: All instances point to the same Cosmos DB
4. **Proper Locks**: Lock TTL configured appropriately

```csharp
properties["quartz.scheduler.instanceName"] = "MyClusteredScheduler";
properties["quartz.scheduler.instanceId"] = $"{Environment.MachineName}-{Guid.NewGuid()}";
properties["quartz.jobStore.clustered"] = "true";
properties["quartz.jobStore.LockTimeout"] = "30"; // seconds
```

## Performance Tuning

### Cosmos DB Throughput

- **Development**: 400 RU/s minimum
- **Production**: Start with 1000 RU/s and monitor
- **High load**: Consider autoscale (1000-4000 RU/s)

### Indexing Policy

Create a custom indexing policy for better performance:

```json
{
  "indexingMode": "consistent",
  "automatic": true,
  "includedPaths": [
    {
      "path": "/Type/?"
    },
    {
      "path": "/InstanceName/?"
    },
    {
      "path": "/State/?"
    }
  ],
  "excludedPaths": [
    {
      "path": "/*"
    }
  ]
}
```

## Troubleshooting

### Common Issues

#### 1. Connection Timeout

**Problem**: Scheduler fails to connect to Cosmos DB

**Solution**:
```csharp
// Try Gateway mode
properties["quartz.jobStore.ConnectionMode"] = "1";

// Increase timeout
properties["quartz.jobStore.RequestTimeout"] = "60";
```

#### 2. Lock Conflicts in Cluster

**Problem**: Jobs not firing in clustered environment

**Solution**:
```csharp
// Increase lock timeout
properties["quartz.jobStore.LockTimeout"] = "60";

// Ensure unique instance IDs
properties["quartz.scheduler.instanceId"] = $"{Environment.MachineName}-{Guid.NewGuid()}";
```

#### 3. Throttling (429 Errors)

**Problem**: Request rate too large

**Solution**:
- Increase Cosmos DB throughput (RU/s)
- Reduce scheduler polling frequency
- Implement retry policy

### Logging

Enable detailed logging to diagnose issues:

```csharp
using Microsoft.Extensions.Logging;

services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
```

## Migration Guide

### Upgrading from Previous Versions

If upgrading from version 1.x to 2.0:

1. **Target Framework**: Update to .NET 8.0
2. **Dependencies**: Update Quartz.NET to 3.15.1+
3. **Logging**: Replace Common.Logging with Microsoft.Extensions.Logging
4. **API Changes**: Review any breaking changes in Quartz.NET 3.15

### Data Migration

The Cosmos DB schema is compatible across versions. No data migration required.

## Version History

### Version 2.0.0
- ✅ Upgraded to .NET 8.0
- ✅ Updated to Quartz.NET 3.15.1
- ✅ Updated to Azure Cosmos DB SDK 3.54.0
- ✅ Replaced Common.Logging with Microsoft.Extensions.Logging
- ✅ Improved .NET 8 compatibility

### Version 1.1.0
- Updated to Quartz.NET 3.6.2
- Implemented ResetTriggerFromErrorState
- Switched to MIT license

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- 📖 [Quartz.NET Documentation](https://www.quartz-scheduler.net/documentation/)
- 📖 [Azure Cosmos DB Documentation](https://docs.microsoft.com/en-us/azure/cosmos-db/)
- 🐛 [Report Issues](https://github.com/ozone3950/cosmosdb-quartznet/issues)

## Acknowledgments

- Based on [Quartz.NET MongoDb Job Store](https://github.com/chrisdrobison/mongodb-quartz-net)
- Built for the Quartz.NET community
