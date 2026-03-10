using BlazorHybrid.Api.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Cosmos DB
builder.Services.AddSingleton(sp =>
{
    var connectionString = Environment.GetEnvironmentVariable("CosmosDbConnectionString")
        ?? throw new InvalidOperationException("CosmosDbConnectionString not configured");
    return new CosmosClient(connectionString, new CosmosClientOptions
    {
        SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        }
    });
});
builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();

// Blob Storage
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

builder.Build().Run();
