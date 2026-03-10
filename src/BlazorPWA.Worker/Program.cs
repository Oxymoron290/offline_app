using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using BlazorPWA.Worker;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// Azure Service Bus
builder.Services.AddSingleton(sp =>
{
    var connectionString = builder.Configuration["ServiceBus:ConnectionString"];
    if (!string.IsNullOrEmpty(connectionString))
        return new ServiceBusClient(connectionString);

    var fullyQualifiedNamespace = builder.Configuration["ServiceBus:FullyQualifiedNamespace"];
    return new ServiceBusClient(fullyQualifiedNamespace, new DefaultAzureCredential());
});

// Azure Blob Storage
builder.Services.AddSingleton(sp =>
{
    var connectionString = builder.Configuration["BlobStorage:ConnectionString"];
    if (!string.IsNullOrEmpty(connectionString))
        return new BlobServiceClient(connectionString);

    var blobUri = new Uri(builder.Configuration["BlobStorage:ServiceUri"]!);
    return new BlobServiceClient(blobUri, new DefaultAzureCredential());
});

builder.Services.AddSingleton<BlobStorageService>();
builder.Services.AddHostedService<MediaProcessorWorker>();

var host = builder.Build();
host.Run();
