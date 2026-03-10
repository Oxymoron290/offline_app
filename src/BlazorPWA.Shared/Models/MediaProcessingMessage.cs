namespace BlazorPWA.Shared.Models;

using BlazorPWA.Shared.Enums;

public class MediaProcessingMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReportId { get; set; }
    public Guid AttachmentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public MediaType MediaType { get; set; }
    public string BlobContainerName { get; set; } = "media";
    public string BlobPath { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
