using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace BlazorPWA.Worker;

public class BlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(BlobServiceClient blobServiceClient, ILogger<BlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<string> UploadAsync(string containerName, string blobPath, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobPath);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            }
        };

        await blobClient.UploadAsync(content, uploadOptions, cancellationToken);

        _logger.LogInformation("Uploaded blob {BlobPath} to container {Container}", blobPath, containerName);
        return blobClient.Uri.ToString();
    }

    public async Task<bool> ExistsAsync(string containerName, string blobPath, CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);
        return await blobClient.ExistsAsync(cancellationToken);
    }
}
