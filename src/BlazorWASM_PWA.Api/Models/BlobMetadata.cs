namespace BlazorWASM_PWA.Api.Models;

public class BlobMetadata
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string BlobType { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
}
