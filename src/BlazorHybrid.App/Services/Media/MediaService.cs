using Microsoft.Extensions.Logging;

namespace BlazorHybrid.App.Services.Media;

public class MediaService : IMediaService
{
    private readonly ILogger<MediaService> _logger;

    public MediaService(ILogger<MediaService> logger)
    {
        _logger = logger;
    }

    public async Task<string?> CapturePhotoAsync()
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                _logger.LogWarning("Photo capture is not supported on this device");
                return null;
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo is null) return null;

            var mediaDir = await GetMediaDirectoryAsync();
            var fileName = $"photo_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N[..8]}.jpg";
            var destPath = Path.Combine(mediaDir, fileName);

            await using var sourceStream = await photo.OpenReadAsync();
            await using var destStream = File.OpenWrite(destPath);
            await sourceStream.CopyToAsync(destStream);

            _logger.LogInformation("Photo captured: {Path}", destPath);
            return destPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture photo");
            return null;
        }
    }

    public async Task<string?> CaptureVideoAsync()
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                _logger.LogWarning("Video capture is not supported on this device");
                return null;
            }

            var video = await MediaPicker.Default.CaptureVideoAsync();
            if (video is null) return null;

            var mediaDir = await GetMediaDirectoryAsync();
            var fileName = $"video_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N[..8]}.mp4";
            var destPath = Path.Combine(mediaDir, fileName);

            await using var sourceStream = await video.OpenReadAsync();
            await using var destStream = File.OpenWrite(destPath);
            await sourceStream.CopyToAsync(destStream);

            _logger.LogInformation("Video captured: {Path}", destPath);
            return destPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture video");
            return null;
        }
    }

    public async Task<string?> PickDocumentAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a document"
            });

            if (result is null) return null;

            var mediaDir = await GetMediaDirectoryAsync();
            var fileName = $"doc_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{result.FileName}";
            var destPath = Path.Combine(mediaDir, fileName);

            await using var sourceStream = await result.OpenReadAsync();
            await using var destStream = File.OpenWrite(destPath);
            await sourceStream.CopyToAsync(destStream);

            _logger.LogInformation("Document picked: {Path}", destPath);
            return destPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pick document");
            return null;
        }
    }

    public Task<string> GetMediaDirectoryAsync()
    {
        var mediaDir = Path.Combine(FileSystem.AppDataDirectory, "media");
        Directory.CreateDirectory(mediaDir);
        return Task.FromResult(mediaDir);
    }

    public Task<bool> FileExistsAsync(string filePath)
    {
        return Task.FromResult(File.Exists(filePath));
    }

    public Task DeleteFileAsync(string filePath)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }
}
