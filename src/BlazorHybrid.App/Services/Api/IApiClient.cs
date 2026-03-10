using BlazorHybrid.Shared.DTOs;

namespace BlazorHybrid.App.Services.Api;

public interface IApiClient
{
    // Entities
    Task<List<EntityDto>> GetEntitiesAsync();
    Task<EntityDto?> GetEntityAsync(string id);
    Task<EntityDto> CreateEntityAsync(EntityDto entity);
    Task<EntityDto> UpdateEntityAsync(EntityDto entity);
    Task DeleteEntityAsync(string id);

    // Sync
    Task<SyncResultDto> SyncBatchAsync(SyncBatchDto batch);

    // Media
    Task<SasTokenDto> GetUploadSasTokenAsync(string containerPath);
    Task<MediaUploadResultDto> UploadMediaAsync(string sasUri, Stream fileStream, string contentType);
}
