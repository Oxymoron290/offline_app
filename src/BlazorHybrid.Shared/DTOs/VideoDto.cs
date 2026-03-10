namespace BlazorHybrid.Shared.DTOs;

public class VideoDto
{
    public string Id { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public TimeSpan Duration { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Caption { get; set; }
    public string? BlobUrl { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
