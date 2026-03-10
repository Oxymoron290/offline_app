using BlazorWASM_PWA.Api.Data;
using BlazorWASM_PWA.Api.Models;
using BlazorWASM_PWA.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlazorWASM_PWA.Api.Functions;

public class EntitiesFunction
{
    private readonly AppDbContext _db;
    private readonly ILogger<EntitiesFunction> _logger;

    public EntitiesFunction(AppDbContext db, ILogger<EntitiesFunction> logger)
    {
        _db = db;
        _logger = logger;
    }

    [Function("EntitiesList")]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "entities")] HttpRequest req)
    {
        var query = _db.Entities.Where(e => !e.IsDeleted);

        if (req.Query.TryGetValue("entityType", out var entityType) && !string.IsNullOrEmpty(entityType))
        {
            query = query.Where(e => e.EntityType == entityType.ToString());
        }

        var entities = await query.OrderByDescending(e => e.UpdatedAt).ToListAsync();

        var entityIds = entities.Select(e => e.Id).ToList();
        var blobs = await _db.BlobMetadata
            .Where(b => entityIds.Contains(b.EntityId))
            .ToListAsync();
        var blobsByEntity = blobs.GroupBy(b => b.EntityId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var dtos = entities.Select(e => MapToDto(e, blobsByEntity)).ToList();
        return new OkObjectResult(dtos);
    }

    [Function("EntitiesGet")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "entities/{id:guid}")] HttpRequest req,
        Guid id)
    {
        var entity = await _db.Entities.FindAsync(id);
        if (entity is null || entity.IsDeleted)
            return new NotFoundResult();

        var blobs = await _db.BlobMetadata.Where(b => b.EntityId == id).ToListAsync();
        var blobsByEntity = new Dictionary<Guid, List<BlobMetadata>> { [id] = blobs };

        return new OkObjectResult(MapToDto(entity, blobsByEntity));
    }

    [Function("EntitiesCreate")]
    public async Task<IActionResult> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "entities")] HttpRequest req)
    {
        var dto = await req.ReadFromJsonAsync<EntityDto>();
        if (dto is null)
            return new BadRequestObjectResult("Invalid entity data.");

        var now = DateTimeOffset.UtcNow;
        var entity = new EntityRecord
        {
            Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
            EntityType = dto.EntityType,
            Name = dto.Name,
            Description = dto.Description,
            JsonData = dto.JsonData,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = dto.CreatedBy
        };

        _db.Entities.Add(entity);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created entity {Id} of type {Type}", entity.Id, entity.EntityType);

        var resultDto = MapToDto(entity, []);
        return new CreatedResult($"/api/entities/{entity.Id}", resultDto);
    }

    [Function("EntitiesUpdate")]
    public async Task<IActionResult> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "entities/{id:guid}")] HttpRequest req,
        Guid id)
    {
        var entity = await _db.Entities.FindAsync(id);
        if (entity is null || entity.IsDeleted)
            return new NotFoundResult();

        var dto = await req.ReadFromJsonAsync<EntityDto>();
        if (dto is null)
            return new BadRequestObjectResult("Invalid entity data.");

        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.JsonData = dto.JsonData;
        entity.EntityType = dto.EntityType;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated entity {Id}", id);

        var blobs = await _db.BlobMetadata.Where(b => b.EntityId == id).ToListAsync();
        var blobsByEntity = new Dictionary<Guid, List<BlobMetadata>> { [id] = blobs };

        return new OkObjectResult(MapToDto(entity, blobsByEntity));
    }

    [Function("EntitiesDelete")]
    public async Task<IActionResult> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "entities/{id:guid}")] HttpRequest req,
        Guid id)
    {
        var entity = await _db.Entities.FindAsync(id);
        if (entity is null)
            return new NotFoundResult();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Soft-deleted entity {Id}", id);
        return new NoContentResult();
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
