using SQLite;

namespace BlazorHybrid.App.Data.Models;

[Table("Documents")]
public class DocumentRecord
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Indexed]
    public string EntityId { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    /// <summary>Local file path on device.</summary>
    public string LocalFilePath { get; set; } = string.Empty;

    /// <summary>Azure Blob URL after sync.</summary>
    public string? BlobUrl { get; set; }

    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsDeleted { get; set; }
    public bool IsSynced { get; set; }
}
