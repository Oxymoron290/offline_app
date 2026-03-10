using BlazorHybrid.App.Data.Models;

namespace BlazorHybrid.App.Data.Repositories;

public interface ISyncOperationRepository
{
    Task<List<SyncOperation>> GetPendingAsync(int limit = 50);
    Task<SyncOperation?> GetByIdAsync(string id);
    Task<int> InsertAsync(SyncOperation operation);
    Task<int> UpdateAsync(SyncOperation operation);
    Task<int> DeleteCompletedAsync();
    Task<int> GetPendingCountAsync();
    Task<int> GetFailedCountAsync();
}
