using Microsoft.JSInterop;
using BlazorWASM_PWA.Client.Models;

namespace BlazorWASM_PWA.Client.Services;

public class IndexedDbService : IIndexedDbService
{
    private readonly IJSRuntime _jsRuntime;

    public IndexedDbService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeDatabaseAsync()
    {
        await _jsRuntime.InvokeVoidAsync("indexedDb.initialize");
    }

    // Entities

    public async Task AddEntityAsync(EntityRecord entity)
    {
        await _jsRuntime.InvokeVoidAsync("indexedDb.addEntity", entity);
    }

    public async Task<EntityRecord?> GetEntityAsync(string id)
    {
        return await _jsRuntime.InvokeAsync<EntityRecord?>("indexedDb.getEntity", id);
    }

    public async Task<List<EntityRecord>> GetAllEntitiesAsync(string? entityType = null)
    {
        return await _jsRuntime.InvokeAsync<List<EntityRecord>>("indexedDb.getAllEntities", entityType)
            ?? [];
    }

    public async Task UpdateEntityAsync(EntityRecord entity)
    {
        await _jsRuntime.InvokeVoidAsync("indexedDb.updateEntity", entity);
    }

    public async Task DeleteEntityAsync(string id)
    {
        await _jsRuntime.InvokeVoidAsync("indexedDb.deleteEntity", id);
    }

    // Blobs

    public async Task AddBlobAsync(BlobReference blob)
    {
        await _jsRuntime.InvokeVoidAsync("indexedDb.addBlob", blob);
    }

    public async Task<BlobReference?> GetBlobAsync(string id)
    {
        return await _jsRuntime.InvokeAsync<BlobReference?>("indexedDb.getBlob", id);
    }

    public async Task<List<BlobReference>> GetBlobsByEntityAsync(string entityId)
    {
        return await _jsRuntime.InvokeAsync<List<BlobReference>>("indexedDb.getBlobsByEntity", entityId)
            ?? [];
    }

    public async Task DeleteBlobAsync(string id)
    {
        await _jsRuntime.InvokeVoidAsync("indexedDb.deleteBlob", id);
    }

    // Operations queue

    public async Task AddOperationAsync(OfflineOperation operation)
    {
        await _jsRuntime.InvokeVoidAsync("indexedDb.addOperation", operation);
    }

    public async Task<List<OfflineOperation>> GetPendingOperationsAsync()
    {
        return await _jsRuntime.InvokeAsync<List<OfflineOperation>>("indexedDb.getPendingOperations")
            ?? [];
    }

    public async Task UpdateOperationStatusAsync(string id, SyncStatus status, string? errorMessage = null)
    {
        await _jsRuntime.InvokeVoidAsync("indexedDb.updateOperationStatus", id, status.ToString(), errorMessage);
    }

    public async Task<int> GetOperationCountAsync(SyncStatus? status = null)
    {
        return await _jsRuntime.InvokeAsync<int>("indexedDb.getOperationCount", status?.ToString());
    }

    public async Task ClearCompletedOperationsAsync()
    {
        await _jsRuntime.InvokeVoidAsync("indexedDb.clearCompletedOperations");
    }

    // Sync helpers

    public async Task<List<EntityRecord>> GetUnsyncedEntitiesAsync()
    {
        return await _jsRuntime.InvokeAsync<List<EntityRecord>>("indexedDb.getUnsyncedEntities")
            ?? [];
    }

    public async Task MarkEntitiesSyncedAsync(List<string> ids)
    {
        await _jsRuntime.InvokeVoidAsync("indexedDb.markEntitiesSynced", ids);
    }

    public async Task MarkBlobsSyncedAsync(List<string> ids)
    {
        await _jsRuntime.InvokeVoidAsync("indexedDb.markBlobsSynced", ids);
    }
}
