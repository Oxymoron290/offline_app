using System.Net;
using BlazorHybrid.Shared.DTOs;
using BlazorHybrid.Shared.Enums;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace BlazorHybrid.Api.Services;

public class CosmosDbService : ICosmosDbService
{
    private readonly CosmosClient _client;
    private readonly ILogger<CosmosDbService> _logger;
    private const string DatabaseName = "BlazorHybridDb";

    private Container EntitiesContainer => _client.GetContainer(DatabaseName, "entities");
    private Container DocumentsContainer => _client.GetContainer(DatabaseName, "documents");
    private Container MediaContainer => _client.GetContainer(DatabaseName, "media");
    private Container SyncLogContainer => _client.GetContainer(DatabaseName, "sync-log");

    public CosmosDbService(CosmosClient client, ILogger<CosmosDbService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<EntityDto> CreateEntityAsync(EntityDto entity)
    {
        entity.CreatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        var response = await EntitiesContainer.CreateItemAsync(entity, new PartitionKey(entity.CaseWorkerId));
        return response.Resource;
    }

    public async Task<EntityDto?> GetEntityAsync(string id, string caseWorkerId)
    {
        try
        {
            var response = await EntitiesContainer.ReadItemAsync<EntityDto>(id, new PartitionKey(caseWorkerId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<EntityDto>> GetEntitiesAsync(string caseWorkerId)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.caseWorkerId = @caseWorkerId AND c.isDeleted = false")
            .WithParameter("@caseWorkerId", caseWorkerId);

        var iterator = EntitiesContainer.GetItemQueryIterator<EntityDto>(query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(caseWorkerId) });

        var results = new List<EntityDto>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public async Task<EntityDto> UpdateEntityAsync(EntityDto entity)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        var response = await EntitiesContainer.UpsertItemAsync(entity, new PartitionKey(entity.CaseWorkerId));
        return response.Resource;
    }

    public async Task DeleteEntityAsync(string id, string caseWorkerId)
    {
        var entity = await GetEntityAsync(id, caseWorkerId);
        if (entity is not null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await EntitiesContainer.UpsertItemAsync(entity, new PartitionKey(caseWorkerId));
        }
    }

    public async Task<SyncResultDto> ProcessSyncBatchAsync(SyncBatchDto batch)
    {
        var result = new SyncResultDto
        {
            ServerTimestamp = DateTimeOffset.UtcNow,
            Results = new List<SyncOperationResultDto>()
        };

        foreach (var op in batch.Operations)
        {
            var opResult = new SyncOperationResultDto { OperationId = op.Id };

            try
            {
                switch (op.EntityType)
                {
                    case Shared.Enums.EntityType.Entity:
                        await ProcessEntityOperationAsync(op, batch.CaseWorkerId);
                        break;
                    case Shared.Enums.EntityType.Document:
                    case Shared.Enums.EntityType.Photo:
                    case Shared.Enums.EntityType.Video:
                        await ProcessMediaMetadataAsync(op);
                        break;
                }

                opResult.Success = true;

                // Log the sync operation
                await SyncLogContainer.CreateItemAsync(new
                {
                    id = Guid.NewGuid().ToString(),
                    deviceId = batch.DeviceId,
                    operationId = op.Id,
                    operationType = op.OperationType.ToString(),
                    entityType = op.EntityType.ToString(),
                    entityId = op.EntityId,
                    timestamp = DateTimeOffset.UtcNow,
                    success = true
                }, new PartitionKey(batch.DeviceId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process sync operation {Id}", op.Id);
                opResult.Success = false;
                opResult.ErrorMessage = ex.Message;
            }

            result.Results.Add(opResult);
        }

        result.Success = result.Results.All(r => r.Success);
        return result;
    }

    private async Task ProcessEntityOperationAsync(SyncOperationDto op, string caseWorkerId)
    {
        switch (op.OperationType)
        {
            case OperationType.Create:
            case OperationType.Update:
                if (op.PayloadJson is not null)
                {
                    var entity = System.Text.Json.JsonSerializer.Deserialize<EntityDto>(op.PayloadJson);
                    if (entity is not null)
                    {
                        entity.CaseWorkerId = caseWorkerId;
                        await UpdateEntityAsync(entity);
                    }
                }
                break;
            case OperationType.Delete:
                await DeleteEntityAsync(op.EntityId, caseWorkerId);
                break;
        }
    }

    private async Task ProcessMediaMetadataAsync(SyncOperationDto op)
    {
        if (op.PayloadJson is null) return;

        var container = op.EntityType switch
        {
            Shared.Enums.EntityType.Document => DocumentsContainer,
            _ => MediaContainer
        };

        switch (op.OperationType)
        {
            case OperationType.Create:
            case OperationType.Update:
                await container.UpsertItemAsync(
                    System.Text.Json.JsonSerializer.Deserialize<object>(op.PayloadJson),
                    new PartitionKey(op.EntityId));
                break;
            case OperationType.Delete:
                try
                {
                    await container.DeleteItemAsync<object>(op.EntityId, new PartitionKey(op.EntityId));
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    // Already deleted
                }
                break;
        }
    }
}
