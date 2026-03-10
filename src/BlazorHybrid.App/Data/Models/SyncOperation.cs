using SQLite;

namespace BlazorHybrid.App.Data.Models;

[Table("SyncOperations")]
public class SyncOperation
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Indexed]
    public string OperationType { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    [Indexed]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>JSON payload for the entity data.</summary>
    public string? PayloadJson { get; set; }

    /// <summary>Local file path if a media file is associated.</summary>
    public string? FilePath { get; set; }

    [Indexed]
    public string Status { get; set; } = "Pending";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
}
