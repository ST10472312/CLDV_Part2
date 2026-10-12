using Azure.Data.Tables;
using Azure.Storage.Queues;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using CoffeeNChillFunctions.Services;


var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.Services.AddSingleton<TableStorageService>();
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

// ---- Part 2 / Member 1: queue producer ----
var storageConnection =
    Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";

// Base64 is required: the Functions queue trigger decodes Base64 by default.
builder.Services.AddSingleton(new QueueServiceClient(
    storageConnection,
    new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }));
builder.Services.AddSingleton<IOrderQueueService, OrderQueueService>();

builder.Services.AddSingleton(new TableServiceClient(storageConnection));
builder.Services.AddSingleton<IOrderTableService, OrderTableService>();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Build().Run();
