using Microsoft.EntityFrameworkCore;
using BlazorPWA.Shared.Models;

namespace BlazorPWA.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<InspectionReport> InspectionReports => Set<InspectionReport>();
    public DbSet<MediaAttachment> MediaAttachments => Set<MediaAttachment>();
    public DbSet<InspectionNote> InspectionNotes => Set<InspectionNote>();
    public DbSet<SyncOperation> SyncOperations => Set<SyncOperation>();
    public DbSet<ConflictInfo> Conflicts => Set<ConflictInfo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InspectionReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.FacilityName).HasMaxLength(200);
            entity.Property(e => e.FacilityAddress).HasMaxLength(500);
            entity.Property(e => e.InspectorName).HasMaxLength(100);
            entity.Property(e => e.InspectorId).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.HasMany(e => e.Attachments).WithOne().HasForeignKey(a => a.ReportId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.InspectionNotes).WithOne().HasForeignKey(n => n.ReportId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MediaAttachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.BlobUrl).HasMaxLength(2048);
            entity.Property(e => e.LocalPath).HasMaxLength(500);
        });

        modelBuilder.Entity<InspectionNote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.AuthorName).HasMaxLength(100);
        });

        modelBuilder.Entity<SyncOperation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DeviceId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EntityId).HasMaxLength(100);
            entity.Property(e => e.EntityType).HasMaxLength(100);
            entity.HasIndex(e => new { e.DeviceId, e.Status });
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<ConflictInfo>(entity =>
        {
            entity.HasKey(e => e.EntityId);
            entity.Property(e => e.EntityType).HasMaxLength(100);
            entity.Property(e => e.Resolution).HasMaxLength(50);
        });
    }
}
