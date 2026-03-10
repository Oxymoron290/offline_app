namespace BlazorHybrid.App.Services.Sync;

public interface ISyncService
{
    Task EnqueueAsync(string operationType, string entityType, string entityId, string? payloadJson, string? filePath = null);
    Task ProcessQueueAsync(CancellationToken cancellationToken = default);
    Task<int> GetPendingCountAsync();
    Task<int> GetFailedCountAsync();
    event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;
}

public class SyncProgressEventArgs : EventArgs
{
    public int TotalOperations { get; set; }
    public int CompletedOperations { get; set; }
    public int FailedOperations { get; set; }
    public bool IsSyncing { get; set; }
}
