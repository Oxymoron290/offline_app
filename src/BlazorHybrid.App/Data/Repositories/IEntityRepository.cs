using BlazorHybrid.App.Data.Models;

namespace BlazorHybrid.App.Data.Repositories;

public interface IEntityRepository
{
    Task<List<EntityRecord>> GetAllAsync();
    Task<EntityRecord?> GetByIdAsync(string id);
    Task<List<EntityRecord>> GetByCaseWorkerIdAsync(string caseWorkerId);
    Task<int> InsertAsync(EntityRecord entity);
    Task<int> UpdateAsync(EntityRecord entity);
    Task<int> DeleteAsync(string id);
    Task<List<EntityRecord>> GetUnsyncedAsync();
}
