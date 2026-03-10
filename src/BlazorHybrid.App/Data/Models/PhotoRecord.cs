using SQLite;

namespace BlazorHybrid.App.Data.Models;

[Table("Photos")]
public class PhotoRecord
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Indexed]
    public string EntityId { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    /// <summary>Local file path on device.</summary>
    public string LocalFilePath { get; set; } = string.Empty;

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Caption { get; set; }

    /// <summary>Azure Blob URL after sync.</summary>
    public string? BlobUrl { get; set; }

    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsDeleted { get; set; }
    public bool IsSynced { get; set; }
}
