using BlazorWASM_PWA.Shared.Models;

namespace BlazorWASM_PWA.Client.Models;

public class OfflineOperation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public OperationType OperationType { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
    public List<string> BlobReferenceIds { get; set; } = [];
    public SyncStatus Status { get; set; } = SyncStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
}
