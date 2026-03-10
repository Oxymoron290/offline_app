using BlazorHybrid.Shared.Enums;

namespace BlazorHybrid.Shared.DTOs;

public class SyncBatchDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string CaseWorkerId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public List<SyncOperationDto> Operations { get; set; } = new();
}

public class SyncOperationDto
{
    public string Id { get; set; } = string.Empty;
    public OperationType OperationType { get; set; }
    public EntityType EntityType { get; set; }
    public string EntityId { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
    public string? FilePath { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class SyncResultDto
{
    public bool Success { get; set; }
    public List<SyncOperationResultDto> Results { get; set; } = new();
    public DateTimeOffset ServerTimestamp { get; set; }
}

public class SyncOperationResultDto
{
    public string OperationId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ServerEntityId { get; set; }
}
