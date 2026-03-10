using Microsoft.JSInterop;
using System.Text.Json;

namespace BlazorPWA.Client.Services;

public class IndexedDbService
{
    private readonly IJSRuntime _jsRuntime;

    public IndexedDbService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    // Entity operations
    public async Task PutEntityAsync<T>(string id, string type, T data, string syncStatus = "Pending")
    {
        var entity = new
        {
            id,
            type,
            data = JsonSerializer.Serialize(data),
            syncStatus,
            updatedAt = DateTime.UtcNow.ToString("O")
        };
        await _jsRuntime.InvokeVoidAsync("indexedDb.putEntity", entity);
    }

    public async Task<T?> GetEntityAsync<T>(string id)
    {
        var entity = await _jsRuntime.InvokeAsync<JsonElement?>("indexedDb.getEntity", id);
        if (entity is null) return default;
        var data = entity.Value.GetProperty("data").GetString();
        return data is not null ? JsonSerializer.Deserialize<T>(data) : default;
    }

    public async Task<List<T>> GetAllEntitiesAsync<T>(string type)
    {
        var entities = await _jsRuntime.InvokeAsync<JsonElement[]>("indexedDb.getAllEntities", type);
        var results = new List<T>();
        foreach (var entity in entities)
        {
            var data = entity.GetProperty("data").GetString();
            if (data is not null)
            {
                var item = JsonSerializer.Deserialize<T>(data);
                if (item is not null) results.Add(item);
            }
        }
        return results;
    }

    public async Task DeleteEntityAsync(string id) =>
        await _jsRuntime.InvokeVoidAsync("indexedDb.deleteEntity", id);

    // Sync queue operations
    public async Task AddToSyncQueueAsync(object operation) =>
        await _jsRuntime.InvokeVoidAsync("indexedDb.addToSyncQueue", operation);

    public async Task<JsonElement[]> GetPendingSyncOperationsAsync() =>
        await _jsRuntime.InvokeAsync<JsonElement[]>("indexedDb.getPendingSyncOperations");

    public async Task UpdateSyncOperationStatusAsync(string id, string status, string? errorMessage = null) =>
        await _jsRuntime.InvokeVoidAsync("indexedDb.updateSyncOperationStatus", id, status, errorMessage);

    public async Task RemoveSyncOperationAsync(string id) =>
        await _jsRuntime.InvokeVoidAsync("indexedDb.removeSyncOperation", id);

    public async Task<int> GetSyncQueueCountAsync() =>
        await _jsRuntime.InvokeAsync<int>("indexedDb.getSyncQueueCount");

    // Media blob operations
    public async Task PutMediaBlobAsync(object media) =>
        await _jsRuntime.InvokeVoidAsync("indexedDb.putMediaBlob", media);

    public async Task<JsonElement?> GetMediaBlobAsync(string id) =>
        await _jsRuntime.InvokeAsync<JsonElement?>("indexedDb.getMediaBlob", id);

    public async Task DeleteMediaBlobAsync(string id) =>
        await _jsRuntime.InvokeVoidAsync("indexedDb.deleteMediaBlob", id);

    // Config operations
    public async Task<string?> GetConfigAsync(string key) =>
        await _jsRuntime.InvokeAsync<string?>("indexedDb.getConfig", key);

    public async Task SetConfigAsync(string key, string value) =>
        await _jsRuntime.InvokeVoidAsync("indexedDb.setConfig", key, value);
}
