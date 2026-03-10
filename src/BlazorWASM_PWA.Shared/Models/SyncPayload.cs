namespace BlazorWASM_PWA.Shared.Models;

public class SyncPayload
{
    public List<SyncOperation> Operations { get; set; } = [];
    public DateTimeOffset? LastSyncTimestamp { get; set; }
}

public class SyncOperation
{
    public Guid OperationId { get; set; }
    public OperationType OperationType { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? PayloadJson { get; set; }
    public List<string> BlobReferenceIds { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
}
