namespace BlazorPWA.Shared.Models;

public class ConflictInfo
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int ClientVersion { get; set; }
    public int ServerVersion { get; set; }
    public string ClientPayload { get; set; } = string.Empty;
    public string ServerPayload { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public string? Resolution { get; set; }
    public bool IsResolved { get; set; }
}
