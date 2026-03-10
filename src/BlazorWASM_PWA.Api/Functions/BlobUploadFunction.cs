using Azure.Storage.Blobs;
using BlazorWASM_PWA.Api.Data;
using BlazorWASM_PWA.Api.Models;
using BlazorWASM_PWA.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlazorWASM_PWA.Api.Functions;

public class BlobUploadFunction
{
    private readonly AppDbContext _db;
    private readonly BlobServiceClient _blobService;
    private readonly IConfiguration _config;
    private readonly ILogger<BlobUploadFunction> _logger;

    public BlobUploadFunction(
        AppDbContext db,
        BlobServiceClient blobService,
        IConfiguration config,
        ILogger<BlobUploadFunction> logger)
    {
        _db = db;
        _blobService = blobService;
        _config = config;
        _logger = logger;
    }

    [Function("BlobUpload")]
    public async Task<IActionResult> Upload(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "blobs/upload")] HttpRequest req)
    {
        var form = await req.ReadFormAsync();
        var file = form.Files.GetFile("file");
        if (file is null || file.Length == 0)
            return new BadRequestObjectResult("No file provided.");

        if (!Guid.TryParse(form["entityId"], out var entityId))
            return new BadRequestObjectResult("Invalid or missing entityId.");

        var blobType = form["blobType"].ToString();
        if (string.IsNullOrEmpty(blobType))
            blobType = "Document";

        var containerName = _config["BlobContainerName"] ?? "uploads";
        var container = _blobService.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync();

        var blobId = Guid.NewGuid();
        var blobName = $"{entityId}/{blobId}/{file.FileName}";
        var blobClient = container.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, overwrite: true);

        var metadata = new BlobMetadata
        {
            Id = blobId,
            EntityId = entityId,
            FileName = file.FileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            BlobType = blobType,
            BlobUrl = blobClient.Uri.ToString(),
            UploadedAt = DateTimeOffset.UtcNow
        };

        _db.BlobMetadata.Add(metadata);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Uploaded blob {BlobId} for entity {EntityId}", blobId, entityId);

        var dto = MapToDto(metadata);
        return new CreatedResult($"/api/blobs/{blobId}", dto);
    }

    [Function("BlobGet")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blobs/{id:guid}")] HttpRequest req,
        Guid id)
    {
        var blob = await _db.BlobMetadata.FindAsync(id);
        if (blob is null)
            return new NotFoundResult();

        return new OkObjectResult(MapToDto(blob));
    }

    [Function("BlobListByEntity")]
    public async Task<IActionResult> ListByEntity(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blobs/entity/{entityId:guid}")] HttpRequest req,
        Guid entityId)
    {
        var blobs = await _db.BlobMetadata
            .Where(b => b.EntityId == entityId)
            .OrderByDescending(b => b.UploadedAt)
            .ToListAsync();

        var dtos = blobs.Select(MapToDto).ToList();
        return new OkObjectResult(dtos);
    }

    [Function("BlobDelete")]
    public async Task<IActionResult> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "blobs/{id:guid}")] HttpRequest req,
        Guid id)
    {
        var blob = await _db.BlobMetadata.FindAsync(id);
        if (blob is null)
            return new NotFoundResult();

        // Delete from Azure Blob Storage
        try
        {
            var containerName = _config["BlobContainerName"] ?? "uploads";
            var container = _blobService.GetBlobContainerClient(containerName);
            var blobUri = new Uri(blob.BlobUrl);
            var blobName = string.Join("/", blobUri.Segments.Skip(2).Select(s => s.TrimEnd('/')));
            var blobClient = container.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete blob {BlobId} from storage", id);
        }

        _db.BlobMetadata.Remove(blob);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted blob {BlobId}", id);
        return new NoContentResult();
    }

    private static BlobReferenceDto MapToDto(BlobMetadata blob) => new()
    {
        Id = blob.Id,
        EntityId = blob.EntityId,
        FileName = blob.FileName,
        ContentType = blob.ContentType,
        SizeBytes = blob.SizeBytes,
        BlobUrl = blob.BlobUrl,
        BlobType = blob.BlobType,
        UploadedAt = blob.UploadedAt
    };
}
