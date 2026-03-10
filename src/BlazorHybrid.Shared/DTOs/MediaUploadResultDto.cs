namespace BlazorHybrid.Shared.DTOs;

public class MediaUploadResultDto
{
    public string BlobUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}

public class SasTokenDto
{
    public string SasUri { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}
