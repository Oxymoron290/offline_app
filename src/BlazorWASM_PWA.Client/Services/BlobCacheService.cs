using BlazorWASM_PWA.Client.Models;

namespace BlazorWASM_PWA.Client.Services;

public class BlobCacheService
{
    private readonly IIndexedDbService _indexedDb;

    public BlobCacheService(IIndexedDbService indexedDb)
    {
        _indexedDb = indexedDb;
    }

    public async Task<BlobReference> CacheBlobAsync(
        string entityId,
        string fileName,
        string contentType,
        string blobType,
        byte[] data)
    {
        var blob = new BlobReference
        {
            EntityId = entityId,
            FileName = fileName,
            ContentType = contentType,
            BlobType = blobType,
            Data = data,
            SizeBytes = data.Length,
            IsSynced = false
        };

        await _indexedDb.AddBlobAsync(blob);
        return blob;
    }

    public async Task<BlobReference?> GetCachedBlobAsync(string id)
    {
        return await _indexedDb.GetBlobAsync(id);
    }

    public async Task<List<BlobReference>> GetCachedBlobsForEntityAsync(string entityId)
    {
        return await _indexedDb.GetBlobsByEntityAsync(entityId);
    }

    public async Task RemoveCachedBlobAsync(string id)
    {
        await _indexedDb.DeleteBlobAsync(id);
    }

    public async Task<long> GetCacheSizeAsync()
    {
        var allEntities = await _indexedDb.GetAllEntitiesAsync();
        long totalSize = 0;

        foreach (var entity in allEntities)
        {
            var blobs = await _indexedDb.GetBlobsByEntityAsync(entity.Id);
            totalSize += blobs.Sum(b => b.SizeBytes);
        }

        return totalSize;
    }
}
