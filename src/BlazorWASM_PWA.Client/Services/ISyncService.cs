using BlazorWASM_PWA.Client.Models;

namespace BlazorWASM_PWA.Client.Services;

public interface ISyncService : IAsyncDisposable
{
    bool IsSyncing { get; }
    int PendingOperationCount { get; }
    event Action<int> OnPendingCountChanged;
    event Action<bool> OnSyncStatusChanged;

    Task InitializeAsync();
    Task QueueOperationAsync(OfflineOperation operation);
    Task<bool> SyncNowAsync();
    Task<int> GetPendingCountAsync();
}
