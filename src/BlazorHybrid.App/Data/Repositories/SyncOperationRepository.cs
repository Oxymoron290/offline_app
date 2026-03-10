using BlazorHybrid.App.Data.Models;

namespace BlazorHybrid.App.Data.Repositories;

public class SyncOperationRepository : ISyncOperationRepository
{
    private readonly AppDbContext _db;

    public SyncOperationRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<SyncOperation>> GetPendingAsync(int limit = 50)
    {
        return await _db.Connection
            .Table<SyncOperation>()
            .Where(s => s.Status == "Pending" || s.Status == "Failed")
            .OrderBy(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<SyncOperation?> GetByIdAsync(string id)
    {
        return await _db.Connection
            .Table<SyncOperation>()
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<int> InsertAsync(SyncOperation operation)
    {
        return await _db.Connection.InsertAsync(operation);
    }

    public async Task<int> UpdateAsync(SyncOperation operation)
    {
        return await _db.Connection.UpdateAsync(operation);
    }

    public async Task<int> DeleteCompletedAsync()
    {
        return await _db.Connection
            .Table<SyncOperation>()
            .DeleteAsync(s => s.Status == "Completed");
    }

    public async Task<int> GetPendingCountAsync()
    {
        return await _db.Connection
            .Table<SyncOperation>()
            .CountAsync(s => s.Status == "Pending");
    }

    public async Task<int> GetFailedCountAsync()
    {
        return await _db.Connection
            .Table<SyncOperation>()
            .CountAsync(s => s.Status == "Failed");
    }
}
