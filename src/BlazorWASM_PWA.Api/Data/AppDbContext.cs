using BlazorWASM_PWA.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorWASM_PWA.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<EntityRecord> Entities => Set<EntityRecord>();
    public DbSet<BlobMetadata> BlobMetadata => Set<BlobMetadata>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EntityRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsDeleted);
            entity.HasIndex(e => e.UpdatedAt);
        });

        modelBuilder.Entity<BlobMetadata>(blob =>
        {
            blob.HasKey(b => b.Id);
            blob.HasIndex(b => b.EntityId);
        });
    }
}
