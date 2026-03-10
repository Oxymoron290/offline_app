using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using BlazorWASM_PWA.Client.Models;
using BlazorWASM_PWA.Shared.Models;

namespace BlazorWASM_PWA.Client.Services;

public class SyncService : ISyncService
{
    private const int MaxRetryCount = 5;
    private static readonly TimeSpan AutoSyncInterval = TimeSpan.FromSeconds(30);

    private readonly IIndexedDbService _indexedDb;
    private readonly IConnectivityService _connectivity;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SyncService> _logger;

    private Timer? _autoSyncTimer;
    private DateTimeOffset? _lastSyncTimestamp;
    private bool _disposed;

    public bool IsSyncing { get; private set; }
    public int PendingOperationCount { get; private set; }
    public event Action<int> OnPendingCountChanged = delegate { };
    public event Action<bool> OnSyncStatusChanged = delegate { };

    public SyncService(
        IIndexedDbService indexedDb,
        IConnectivityService connectivity,
        HttpClient httpClient,
        ILogger<SyncService> logger)
    {
        _indexedDb = indexedDb;
        _connectivity = connectivity;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        _connectivity.OnConnectivityChanged += OnConnectivityChanged;

        PendingOperationCount = await _indexedDb.GetOperationCountAsync(SyncStatus.Pending);
        OnPendingCountChanged.Invoke(PendingOperationCount);

        StartAutoSyncTimer();
    }

    public async Task QueueOperationAsync(OfflineOperation operation)
    {
        operation.Status = SyncStatus.Pending;
        await _indexedDb.AddOperationAsync(operation);

        PendingOperationCount++;
        OnPendingCountChanged.Invoke(PendingOperationCount);

        if (_connectivity.IsOnline)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await SyncNowAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background sync after queue failed");
                }
            });
        }
    }

    public async Task<bool> SyncNowAsync()
    {
        if (IsSyncing || !_connectivity.IsOnline)
            return false;

        IsSyncing = true;
        OnSyncStatusChanged.Invoke(true);

        try
        {
            var allSucceeded = await PushOperationsAsync();
            await PullServerUpdatesAsync();
            await RefreshPendingCount();

            return allSucceeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed");
            return false;
        }
        finally
        {
            IsSyncing = false;
            OnSyncStatusChanged.Invoke(false);
        }
    }

    public async Task<int> GetPendingCountAsync()
    {
        PendingOperationCount = await _indexedDb.GetOperationCountAsync(SyncStatus.Pending);
        return PendingOperationCount;
    }

    private async Task<bool> PushOperationsAsync()
    {
        var pendingOperations = await _indexedDb.GetPendingOperationsAsync();
        if (pendingOperations.Count == 0)
            return true;

        // Mark operations as in-progress
        foreach (var op in pendingOperations)
        {
            await _indexedDb.UpdateOperationStatusAsync(op.Id, SyncStatus.InProgress);
        }

        var payload = new SyncPayload
        {
            LastSyncTimestamp = _lastSyncTimestamp,
            Operations = pendingOperations.Select(op => new SyncOperation
            {
                OperationId = Guid.Parse(op.Id),
                OperationType = op.OperationType,
                EntityType = op.EntityType,
                EntityId = Guid.Parse(op.EntityId),
                PayloadJson = op.PayloadJson,
                BlobReferenceIds = op.BlobReferenceIds,
                CreatedAt = op.CreatedAt
            }).ToList()
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("/api/sync", payload);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to reach sync endpoint; operations remain pending");
            foreach (var op in pendingOperations)
            {
                await _indexedDb.UpdateOperationStatusAsync(op.Id, SyncStatus.Pending);
            }
            return false;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Sync POST returned {StatusCode}", response.StatusCode);
            foreach (var op in pendingOperations)
            {
                await _indexedDb.UpdateOperationStatusAsync(op.Id, SyncStatus.Pending);
            }
            return false;
        }

        var syncResult = await response.Content.ReadFromJsonAsync<SyncResult>();
        if (syncResult is null)
        {
            _logger.LogWarning("Sync response body was null");
            return false;
        }

        _lastSyncTimestamp = syncResult.ServerTimestamp;
        var allSucceeded = true;

        foreach (var result in syncResult.OperationResults)
        {
            var operationId = result.OperationId.ToString();

            if (result.Success)
            {
                await _indexedDb.UpdateOperationStatusAsync(operationId, SyncStatus.Completed);
            }
            else
            {
                allSucceeded = false;
                var original = pendingOperations.FirstOrDefault(o => o.Id == operationId);
                var retryCount = (original?.RetryCount ?? 0) + 1;

                if (retryCount >= MaxRetryCount)
                {
                    await _indexedDb.UpdateOperationStatusAsync(
                        operationId, SyncStatus.Failed, result.ErrorMessage);
                    _logger.LogError("Operation {OperationId} permanently failed: {Error}",
                        operationId, result.ErrorMessage);
                }
                else
                {
                    await _indexedDb.UpdateOperationStatusAsync(
                        operationId, SyncStatus.Pending, result.ErrorMessage);
                }
            }
        }

        // Mark synced entities and blobs
        var syncedEntityIds = pendingOperations
            .Where(op => syncResult.OperationResults
                .Any(r => r.OperationId.ToString() == op.Id && r.Success))
            .Select(op => op.EntityId)
            .Distinct()
            .ToList();

        if (syncedEntityIds.Count > 0)
            await _indexedDb.MarkEntitiesSyncedAsync(syncedEntityIds);

        var syncedBlobIds = pendingOperations
            .Where(op => syncResult.OperationResults
                .Any(r => r.OperationId.ToString() == op.Id && r.Success))
            .SelectMany(op => op.BlobReferenceIds)
            .Distinct()
            .ToList();

        if (syncedBlobIds.Count > 0)
            await _indexedDb.MarkBlobsSyncedAsync(syncedBlobIds);

        await _indexedDb.ClearCompletedOperationsAsync();

        return allSucceeded;
    }

    private async Task PullServerUpdatesAsync()
    {
        try
        {
            var url = _lastSyncTimestamp.HasValue
                ? $"/api/sync?since={_lastSyncTimestamp.Value:O}"
                : "/api/sync";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Pull returned {StatusCode}", response.StatusCode);
                return;
            }

            var syncResult = await response.Content.ReadFromJsonAsync<SyncResult>();
            if (syncResult is null)
                return;

            _lastSyncTimestamp = syncResult.ServerTimestamp;

            foreach (var dto in syncResult.UpdatedEntities)
            {
                var entity = MapDtoToEntityRecord(dto);
                var existing = await _indexedDb.GetEntityAsync(entity.Id);

                if (existing is null)
                {
                    await _indexedDb.AddEntityAsync(entity);
                }
                else
                {
                    await _indexedDb.UpdateEntityAsync(entity);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to pull server updates");
        }
    }

    private async Task RefreshPendingCount()
    {
        PendingOperationCount = await _indexedDb.GetOperationCountAsync(SyncStatus.Pending);
        OnPendingCountChanged.Invoke(PendingOperationCount);
    }

    private async void OnConnectivityChanged(bool isOnline)
    {
        if (isOnline)
        {
            try
            {
                await SyncNowAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync on reconnect failed");
            }
        }
    }

    private void StartAutoSyncTimer()
    {
        _autoSyncTimer = new Timer(async _ =>
        {
            if (_connectivity.IsOnline && !IsSyncing)
            {
                try
                {
                    await SyncNowAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Auto-sync failed");
                }
            }
        }, null, AutoSyncInterval, AutoSyncInterval);
    }

    private static EntityRecord MapDtoToEntityRecord(EntityDto dto)
    {
        return new EntityRecord
        {
            Id = dto.Id.ToString(),
            EntityType = dto.EntityType,
            Name = dto.Name,
            Description = dto.Description,
            JsonData = dto.JsonData,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            CreatedBy = dto.CreatedBy,
            IsDeleted = dto.IsDeleted,
            IsSynced = true
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _connectivity.OnConnectivityChanged -= OnConnectivityChanged;

        if (_autoSyncTimer is not null)
        {
            await _autoSyncTimer.DisposeAsync();
            _autoSyncTimer = null;
        }

        GC.SuppressFinalize(this);
    }
}
