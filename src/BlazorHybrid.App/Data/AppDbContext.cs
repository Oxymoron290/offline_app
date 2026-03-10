using BlazorHybrid.App.Data.Models;
using SQLite;

namespace BlazorHybrid.App.Data;

public class AppDbContext
{
    private readonly SQLiteAsyncConnection _db;

    public AppDbContext(string dbPath)
    {
        _db = new SQLiteAsyncConnection(dbPath);
    }

    public SQLiteAsyncConnection Connection => _db;

    public async Task InitializeAsync()
    {
        await _db.CreateTableAsync<EntityRecord>();
        await _db.CreateTableAsync<DocumentRecord>();
        await _db.CreateTableAsync<PhotoRecord>();
        await _db.CreateTableAsync<VideoRecord>();
        await _db.CreateTableAsync<SyncOperation>();
    }
}
