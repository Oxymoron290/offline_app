namespace BlazorWASM_PWA.Shared.Models;

public class SyncResult
{
    public bool Success { get; set; }
    public List<OperationResult> OperationResults { get; set; } = [];
    public List<EntityDto> UpdatedEntities { get; set; } = [];
    public DateTimeOffset ServerTimestamp { get; set; }
}

public class OperationResult
{
    public Guid OperationId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
