using BlazorHybrid.Api.Services;
using BlazorHybrid.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BlazorHybrid.Api.Functions;

public class MediaFunctions
{
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<MediaFunctions> _logger;

    public MediaFunctions(IBlobStorageService blobStorage, ILogger<MediaFunctions> logger)
    {
        _blobStorage = blobStorage;
        _logger = logger;
    }

    [Function("GetUploadSasToken")]
    public async Task<IActionResult> GetUploadSasToken(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "media/sas-token")] HttpRequest req)
    {
        var body = await req.ReadFromJsonAsync<SasTokenRequest>();
        if (body is null || string.IsNullOrEmpty(body.Path))
            return new BadRequestObjectResult("path is required");

        var sasToken = await _blobStorage.GenerateUploadSasTokenAsync(body.Path);
        _logger.LogInformation("Generated SAS token for path: {Path}", body.Path);
        return new OkObjectResult(sasToken);
    }

    [Function("UploadMedia")]
    public async Task<IActionResult> UploadMedia(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "media/upload")] HttpRequest req)
    {
        var path = req.Query["path"].ToString();
        if (string.IsNullOrEmpty(path))
            return new BadRequestObjectResult("path query parameter is required");

        var contentType = req.ContentType ?? "application/octet-stream";
        var blobUrl = await _blobStorage.UploadAsync(path, req.Body, contentType);

        return new OkObjectResult(new MediaUploadResultDto
        {
            BlobUrl = blobUrl,
            FileName = Path.GetFileName(path),
            FileSizeBytes = req.ContentLength ?? 0
        });
    }

    private class SasTokenRequest
    {
        public string Path { get; set; } = string.Empty;
    }
}
