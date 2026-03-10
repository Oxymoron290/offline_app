using BlazorHybrid.App.Data.Models;

namespace BlazorHybrid.App.Data.Repositories;

public class EntityRepository : IEntityRepository
{
    private readonly AppDbContext _db;

    public EntityRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<EntityRecord>> GetAllAsync()
    {
        return await _db.Connection
            .Table<EntityRecord>()
            .Where(e => !e.IsDeleted)
            .OrderByDescending(e => e.UpdatedAt)
            .ToListAsync();
    }

    public async Task<EntityRecord?> GetByIdAsync(string id)
    {
        return await _db.Connection
            .Table<EntityRecord>()
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<EntityRecord>> GetByCaseWorkerIdAsync(string caseWorkerId)
    {
        return await _db.Connection
            .Table<EntityRecord>()
            .Where(e => e.CaseWorkerId == caseWorkerId && !e.IsDeleted)
            .ToListAsync();
    }

    public async Task<int> InsertAsync(EntityRecord entity)
    {
        return await _db.Connection.InsertAsync(entity);
    }

    public async Task<int> UpdateAsync(EntityRecord entity)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        return await _db.Connection.UpdateAsync(entity);
    }

    public async Task<int> DeleteAsync(string id)
    {
        var entity = await GetByIdAsync(id);
        if (entity is null) return 0;

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        return await _db.Connection.UpdateAsync(entity);
    }

    public async Task<List<EntityRecord>> GetUnsyncedAsync()
    {
        return await _db.Connection
            .Table<EntityRecord>()
            .Where(e => !e.IsSynced)
            .ToListAsync();
    }
}
