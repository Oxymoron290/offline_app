using BlazorHybrid.App.Data.Models;

namespace BlazorHybrid.App.Data.Repositories;

public class MediaRepository : IMediaRepository
{
    private readonly AppDbContext _db;

    public MediaRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<PhotoRecord>> GetPhotosByEntityIdAsync(string entityId)
        => await _db.Connection.Table<PhotoRecord>()
            .Where(p => p.EntityId == entityId && !p.IsDeleted)
            .OrderByDescending(p => p.CapturedAt)
            .ToListAsync();

    public async Task<List<VideoRecord>> GetVideosByEntityIdAsync(string entityId)
        => await _db.Connection.Table<VideoRecord>()
            .Where(v => v.EntityId == entityId && !v.IsDeleted)
            .OrderByDescending(v => v.CapturedAt)
            .ToListAsync();

    public async Task<List<DocumentRecord>> GetDocumentsByEntityIdAsync(string entityId)
        => await _db.Connection.Table<DocumentRecord>()
            .Where(d => d.EntityId == entityId && !d.IsDeleted)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

    public async Task<PhotoRecord?> GetPhotoByIdAsync(string id)
        => await _db.Connection.Table<PhotoRecord>().FirstOrDefaultAsync(p => p.Id == id);

    public async Task<VideoRecord?> GetVideoByIdAsync(string id)
        => await _db.Connection.Table<VideoRecord>().FirstOrDefaultAsync(v => v.Id == id);

    public async Task<DocumentRecord?> GetDocumentByIdAsync(string id)
        => await _db.Connection.Table<DocumentRecord>().FirstOrDefaultAsync(d => d.Id == id);

    public async Task<int> InsertPhotoAsync(PhotoRecord photo)
        => await _db.Connection.InsertAsync(photo);

    public async Task<int> InsertVideoAsync(VideoRecord video)
        => await _db.Connection.InsertAsync(video);

    public async Task<int> InsertDocumentAsync(DocumentRecord document)
        => await _db.Connection.InsertAsync(document);

    public async Task<int> UpdatePhotoAsync(PhotoRecord photo)
        => await _db.Connection.UpdateAsync(photo);

    public async Task<int> UpdateVideoAsync(VideoRecord video)
        => await _db.Connection.UpdateAsync(video);

    public async Task<int> UpdateDocumentAsync(DocumentRecord document)
        => await _db.Connection.UpdateAsync(document);

    public async Task<List<PhotoRecord>> GetUnsyncedPhotosAsync()
        => await _db.Connection.Table<PhotoRecord>()
            .Where(p => !p.IsSynced && !p.IsDeleted)
            .ToListAsync();

    public async Task<List<VideoRecord>> GetUnsyncedVideosAsync()
        => await _db.Connection.Table<VideoRecord>()
            .Where(v => !v.IsSynced && !v.IsDeleted)
            .ToListAsync();

    public async Task<List<DocumentRecord>> GetUnsyncedDocumentsAsync()
        => await _db.Connection.Table<DocumentRecord>()
            .Where(d => !d.IsSynced && !d.IsDeleted)
            .ToListAsync();
}
