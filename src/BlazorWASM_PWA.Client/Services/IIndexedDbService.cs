using BlazorWASM_PWA.Client.Models;

namespace BlazorWASM_PWA.Client.Services;

public interface IIndexedDbService
{
    Task InitializeDatabaseAsync();

    // Entities
    Task AddEntityAsync(EntityRecord entity);
    Task<EntityRecord?> GetEntityAsync(string id);
    Task<List<EntityRecord>> GetAllEntitiesAsync(string? entityType = null);
    Task UpdateEntityAsync(EntityRecord entity);
    Task DeleteEntityAsync(string id);

    // Blobs
    Task AddBlobAsync(BlobReference blob);
    Task<BlobReference?> GetBlobAsync(string id);
    Task<List<BlobReference>> GetBlobsByEntityAsync(string entityId);
    Task DeleteBlobAsync(string id);

    // Operations queue
    Task AddOperationAsync(OfflineOperation operation);
    Task<List<OfflineOperation>> GetPendingOperationsAsync();
    Task UpdateOperationStatusAsync(string id, SyncStatus status, string? errorMessage = null);
    Task<int> GetOperationCountAsync(SyncStatus? status = null);
    Task ClearCompletedOperationsAsync();

    // Sync helpers
    Task<List<EntityRecord>> GetUnsyncedEntitiesAsync();
    Task MarkEntitiesSyncedAsync(List<string> ids);
    Task MarkBlobsSyncedAsync(List<string> ids);
}
