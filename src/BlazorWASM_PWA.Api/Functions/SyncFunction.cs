using System.Text.Json;
using BlazorWASM_PWA.Api.Data;
using BlazorWASM_PWA.Api.Models;
using BlazorWASM_PWA.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlazorWASM_PWA.Api.Functions;

public class SyncFunction
{
    private readonly AppDbContext _db;
    private readonly ILogger<SyncFunction> _logger;

    public SyncFunction(AppDbContext db, ILogger<SyncFunction> logger)
    {
        _db = db;
        _logger = logger;
    }

    [Function("Sync")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "sync")] HttpRequest req)
    {
        return req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase)
            ? await HandlePullSync(req)
            : await HandlePushSync(req);
    }

    private async Task<IActionResult> HandlePullSync(HttpRequest req)
    {
        DateTimeOffset since = DateTimeOffset.MinValue;
        if (req.Query.TryGetValue("since", out var sinceValue) &&
            DateTimeOffset.TryParse(sinceValue, out var parsed))
        {
            since = parsed;
        }

        var entities = await _db.Entities
            .Where(e => e.UpdatedAt > since)
            .OrderBy(e => e.UpdatedAt)
            .ToListAsync();

        var entityIds = entities.Select(e => e.Id).ToList();
        var blobs = await _db.BlobMetadata
            .Where(b => entityIds.Contains(b.EntityId))
            .ToListAsync();

        var blobsByEntity = blobs.GroupBy(b => b.EntityId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new SyncResult
        {
            Success = true,
            ServerTimestamp = DateTimeOffset.UtcNow,
            UpdatedEntities = entities.Select(e => MapToDto(e, blobsByEntity)).ToList()
        };

        return new OkObjectResult(result);
    }

    private async Task<IActionResult> HandlePushSync(HttpRequest req)
    {
        var payload = await req.ReadFromJsonAsync<SyncPayload>();
        if (payload is null)
            return new BadRequestObjectResult("Invalid sync payload.");

        var results = new List<OperationResult>();

        foreach (var op in payload.Operations)
        {
            try
            {
                switch (op.OperationType)
                {
                    case OperationType.CreateEntity:
                    case OperationType.UpdateEntity:
                        await UpsertEntity(op);
                        break;
                    case OperationType.DeleteEntity:
                        await SoftDeleteEntity(op.EntityId);
                        break;
                    case OperationType.UploadPhoto:
                    case OperationType.UploadVideo:
                    case OperationType.UploadDocument:
                        // Blob uploads are handled via /api/blobs/upload endpoint
                        _logger.LogInformation("Blob upload operation {OpId} should use /api/blobs/upload", op.OperationId);
                        break;
                    case OperationType.DeleteBlob:
                        _logger.LogInformation("Blob delete operation {OpId} should use /api/blobs/{{id}}", op.OperationId);
                        break;
                }

                results.Add(new OperationResult { OperationId = op.OperationId, Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process operation {OpId}", op.OperationId);
                results.Add(new OperationResult
                {
                    OperationId = op.OperationId,
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        await _db.SaveChangesAsync();

        // Return updated entities since last sync
        var updatedEntities = new List<EntityDto>();
        if (payload.LastSyncTimestamp.HasValue)
        {
            var entities = await _db.Entities
                .Where(e => e.UpdatedAt > payload.LastSyncTimestamp.Value)
                .ToListAsync();

            var entityIds = entities.Select(e => e.Id).ToList();
            var blobs = await _db.BlobMetadata
                .Where(b => entityIds.Contains(b.EntityId))
                .ToListAsync();
            var blobsByEntity = blobs.GroupBy(b => b.EntityId)
                .ToDictionary(g => g.Key, g => g.ToList());

            updatedEntities = entities.Select(e => MapToDto(e, blobsByEntity)).ToList();
        }

        return new OkObjectResult(new SyncResult
        {
            Success = results.All(r => r.Success),
            OperationResults = results,
            UpdatedEntities = updatedEntities,
            ServerTimestamp = DateTimeOffset.UtcNow
        });
    }

    private async Task UpsertEntity(SyncOperation op)
    {
        var existing = await _db.Entities.FindAsync(op.EntityId);
        var now = DateTimeOffset.UtcNow;

        if (existing is null)
        {
            var dto = op.PayloadJson is not null
                ? JsonSerializer.Deserialize<EntityDto>(op.PayloadJson)
                : null;

            var entity = new EntityRecord
            {
                Id = op.EntityId,
                EntityType = dto?.EntityType ?? op.EntityType,
                Name = dto?.Name ?? string.Empty,
                Description = dto?.Description ?? string.Empty,
                JsonData = dto?.JsonData ?? "{}",
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = dto?.CreatedBy ?? string.Empty
            };
            _db.Entities.Add(entity);
        }
        else
        {
            if (op.PayloadJson is not null)
            {
                var dto = JsonSerializer.Deserialize<EntityDto>(op.PayloadJson);
                if (dto is not null)
                {
                    existing.Name = dto.Name;
                    existing.Description = dto.Description;
                    existing.JsonData = dto.JsonData;
                    existing.EntityType = dto.EntityType;
                }
            }
            existing.UpdatedAt = now;
        }
    }

    private async Task SoftDeleteEntity(Guid entityId)
    {
        var entity = await _db.Entities.FindAsync(entityId);
        if (entity is not null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private static EntityDto MapToDto(EntityRecord record, Dictionary<Guid, List<BlobMetadata>> blobsByEntity)
    {
        var dto = new EntityDto
        {
            Id = record.Id,
            EntityType = record.EntityType,
            Name = record.Name,
            Description = record.Description,
            JsonData = record.JsonData,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt,
            CreatedBy = record.CreatedBy,
            IsDeleted = record.IsDeleted
        };

        if (blobsByEntity.TryGetValue(record.Id, out var blobs))
        {
            dto.BlobReferences = blobs.Select(b => new BlobReferenceDto
            {
                Id = b.Id,
                EntityId = b.EntityId,
                FileName = b.FileName,
                ContentType = b.ContentType,
                SizeBytes = b.SizeBytes,
                BlobUrl = b.BlobUrl,
                BlobType = b.BlobType,
                UploadedAt = b.UploadedAt
            }).ToList();
        }

        return dto;
    }
}
