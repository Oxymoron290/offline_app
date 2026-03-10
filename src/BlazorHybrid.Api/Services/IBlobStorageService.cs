using BlazorHybrid.Shared.DTOs;

namespace BlazorHybrid.Api.Services;

public interface IBlobStorageService
{
    Task<SasTokenDto> GenerateUploadSasTokenAsync(string containerPath);
    Task<string> UploadAsync(string containerPath, Stream content, string contentType);
    Task DeleteAsync(string blobUrl);
}
