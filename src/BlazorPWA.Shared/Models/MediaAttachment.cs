namespace BlazorPWA.Shared.Models;

using BlazorPWA.Shared.Enums;

public class MediaAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReportId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public MediaType MediaType { get; set; }
    public long FileSize { get; set; }
    public string? BlobUrl { get; set; }
    public string? LocalPath { get; set; }
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UploadedAt { get; set; }
}
