using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using BlazorHybrid.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace BlazorHybrid.Api.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobClient;
    private readonly ILogger<BlobStorageService> _logger;
    private const string MediaContainer = "media";

    public BlobStorageService(ILogger<BlobStorageService> logger)
    {
        var connectionString = Environment.GetEnvironmentVariable("StorageConnectionString")
            ?? throw new InvalidOperationException("StorageConnectionString not configured");
        _blobClient = new BlobServiceClient(connectionString);
        _logger = logger;
    }

    public async Task<SasTokenDto> GenerateUploadSasTokenAsync(string containerPath)
    {
        var container = _blobClient.GetBlobContainerClient(MediaContainer);
        await container.CreateIfNotExistsAsync();

        var blobClient = container.GetBlobClient(containerPath);
        var expiresOn = DateTimeOffset.UtcNow.AddMinutes(30);

        if (blobClient.CanGenerateSasUri)
        {
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = MediaContainer,
                BlobName = containerPath,
                Resource = "b",
                ExpiresOn = expiresOn
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            return new SasTokenDto
            {
                SasUri = sasUri.ToString(),
                ExpiresAt = expiresOn
            };
        }

        throw new InvalidOperationException("Cannot generate SAS token. Ensure the storage account key is configured.");
    }

    public async Task<string> UploadAsync(string containerPath, Stream content, string contentType)
    {
        var container = _blobClient.GetBlobContainerClient(MediaContainer);
        await container.CreateIfNotExistsAsync();

        var blobClient = container.GetBlobClient(containerPath);
        await blobClient.UploadAsync(content, new Azure.Storage.Blobs.Models.BlobHttpHeaders
        {
            ContentType = contentType
        });

        _logger.LogInformation("Uploaded blob: {Path}", containerPath);
        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string blobUrl)
    {
        var uri = new Uri(blobUrl);
        var blobClient = new BlobClient(uri);
        await blobClient.DeleteIfExistsAsync();
        _logger.LogInformation("Deleted blob: {Url}", blobUrl);
    }
}
