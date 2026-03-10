using BlazorHybrid.App.Data.Models;

namespace BlazorHybrid.App.Data.Repositories;

public interface IMediaRepository
{
    Task<List<PhotoRecord>> GetPhotosByEntityIdAsync(string entityId);
    Task<List<VideoRecord>> GetVideosByEntityIdAsync(string entityId);
    Task<List<DocumentRecord>> GetDocumentsByEntityIdAsync(string entityId);
    Task<PhotoRecord?> GetPhotoByIdAsync(string id);
    Task<VideoRecord?> GetVideoByIdAsync(string id);
    Task<DocumentRecord?> GetDocumentByIdAsync(string id);
    Task<int> InsertPhotoAsync(PhotoRecord photo);
    Task<int> InsertVideoAsync(VideoRecord video);
    Task<int> InsertDocumentAsync(DocumentRecord document);
    Task<int> UpdatePhotoAsync(PhotoRecord photo);
    Task<int> UpdateVideoAsync(VideoRecord video);
    Task<int> UpdateDocumentAsync(DocumentRecord document);
    Task<List<PhotoRecord>> GetUnsyncedPhotosAsync();
    Task<List<VideoRecord>> GetUnsyncedVideosAsync();
    Task<List<DocumentRecord>> GetUnsyncedDocumentsAsync();
}
