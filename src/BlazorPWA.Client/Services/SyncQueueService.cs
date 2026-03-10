using BlazorPWA.Shared;
using BlazorPWA.Shared.Enums;
using BlazorPWA.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace BlazorPWA.Client.Services;

public class SyncQueueService
{
    private readonly IndexedDbService _indexedDb;
    private readonly ConnectivityService _connectivity;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SyncQueueService> _logger;
    private bool _isSyncing;

    public event Action? OnSyncStatusChanged;
    public int PendingCount { get; private set; }
    public bool IsSyncing => _isSyncing;

    public SyncQueueService(
        IndexedDbService indexedDb,
        ConnectivityService connectivity,
        HttpClient httpClient,
        ILogger<SyncQueueService> logger)
    {
        _indexedDb = indexedDb;
        _connectivity = connectivity;
        _httpClient = httpClient;
        _logger = logger;

        _connectivity.OnConnectivityChanged += async (isOnline) =>
        {
            if (isOnline) await TrySyncAsync();
        };
    }

    public async Task EnqueueOperationAsync(OperationType operationType, string entityId, string entityType, object payload, int clientVersion = 0)
    {
        var operation = new
        {
            id = Guid.NewGuid().ToString(),
            operationType = operationType.ToString(),
            entityId,
            entityType,
            deviceId = await GetDeviceIdAsync(),
            payload = JsonSerializer.Serialize(payload),
            status = "Pending",
            clientVersion,
            serverVersion = 0,
            retryCount = 0,
            createdAt = DateTime.UtcNow.ToString("O")
        };

        await _indexedDb.AddToSyncQueueAsync(operation);
        PendingCount = await _indexedDb.GetSyncQueueCountAsync();
        OnSyncStatusChanged?.Invoke();

        if (_connectivity.IsOnline)
            await TrySyncAsync();
    }

    public async Task TrySyncAsync()
    {
        if (_isSyncing || !_connectivity.IsOnline) return;

        _isSyncing = true;
        OnSyncStatusChanged?.Invoke();

        try
        {
            var pendingOps = await _indexedDb.GetPendingSyncOperationsAsync();
            if (pendingOps.Length == 0) return;

            var operations = pendingOps.Select(op => new SyncOperation
            {
                Id = Guid.Parse(op.GetProperty("id").GetString()!),
                DeviceId = op.GetProperty("deviceId").GetString() ?? "",
                OperationType = Enum.Parse<OperationType>(op.GetProperty("operationType").GetString()!),
                EntityId = op.GetProperty("entityId").GetString() ?? "",
                EntityType = op.GetProperty("entityType").GetString() ?? "",
                Payload = op.GetProperty("payload").GetString() ?? "",
                ClientVersion = op.TryGetProperty("clientVersion", out var cv) ? cv.GetInt32() : 0,
                ServerVersion = op.TryGetProperty("serverVersion", out var sv) ? sv.GetInt32() : 0,
                CreatedAt = DateTime.Parse(op.GetProperty("createdAt").GetString()!)
            }).ToList();

            // Send in batches
            for (int i = 0; i < operations.Count; i += Constants.SyncBatchSize)
            {
                var batch = operations.Skip(i).Take(Constants.SyncBatchSize).ToList();
                var response = await _httpClient.PostAsJsonAsync(Constants.ApiRoutes.SyncOperations + "/batch", batch);

                if (response.IsSuccessStatusCode)
                {
                    foreach (var op in batch)
                    {
                        await _indexedDb.UpdateSyncOperationStatusAsync(op.Id.ToString(), "Completed");
                    }
                    _logger.LogInformation("Synced batch of {Count} operations", batch.Count);
                }
                else
                {
                    _logger.LogWarning("Sync batch failed with status {StatusCode}", response.StatusCode);
                    foreach (var op in batch)
                    {
                        await _indexedDb.UpdateSyncOperationStatusAsync(op.Id.ToString(), "Failed", response.ReasonPhrase);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed");
        }
        finally
        {
            _isSyncing = false;
            PendingCount = await _indexedDb.GetSyncQueueCountAsync();
            OnSyncStatusChanged?.Invoke();
        }
    }

    private async Task<string> GetDeviceIdAsync()
    {
        var deviceId = await _indexedDb.GetConfigAsync("deviceId");
        if (string.IsNullOrEmpty(deviceId))
        {
            deviceId = Guid.NewGuid().ToString();
            await _indexedDb.SetConfigAsync("deviceId", deviceId);
        }
        return deviceId;
    }
}
