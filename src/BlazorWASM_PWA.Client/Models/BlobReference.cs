namespace BlazorWASM_PWA.Client.Models;

public class BlobReference
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EntityId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string BlobType { get; set; } = string.Empty; // Photo, Video, Document
    public byte[]? Data { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsSynced { get; set; }
    public string? RemoteUrl { get; set; }
}
