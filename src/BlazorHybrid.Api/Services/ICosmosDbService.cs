using BlazorHybrid.Shared.DTOs;

namespace BlazorHybrid.Api.Services;

public interface ICosmosDbService
{
    Task<EntityDto> CreateEntityAsync(EntityDto entity);
    Task<EntityDto?> GetEntityAsync(string id, string caseWorkerId);
    Task<List<EntityDto>> GetEntitiesAsync(string caseWorkerId);
    Task<EntityDto> UpdateEntityAsync(EntityDto entity);
    Task DeleteEntityAsync(string id, string caseWorkerId);
    Task<SyncResultDto> ProcessSyncBatchAsync(SyncBatchDto batch);
}
