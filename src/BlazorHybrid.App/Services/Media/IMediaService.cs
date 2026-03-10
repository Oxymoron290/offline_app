namespace BlazorHybrid.App.Services.Media;

public interface IMediaService
{
    Task<string?> CapturePhotoAsync();
    Task<string?> CaptureVideoAsync();
    Task<string?> PickDocumentAsync();
    Task<string> GetMediaDirectoryAsync();
    Task<bool> FileExistsAsync(string filePath);
    Task DeleteFileAsync(string filePath);
}
