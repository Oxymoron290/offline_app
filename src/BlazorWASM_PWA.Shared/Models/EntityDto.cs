namespace BlazorWASM_PWA.Shared.Models;

public class EntityDto
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string JsonData { get; set; } = "{}";
    public List<BlobReferenceDto> BlobReferences { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}

public class BlobReferenceDto
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string BlobUrl { get; set; } = string.Empty;
    public string BlobType { get; set; } = string.Empty; // Photo, Video, Document
    public DateTimeOffset UploadedAt { get; set; }
}
