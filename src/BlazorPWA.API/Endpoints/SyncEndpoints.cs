using BlazorPWA.API.Data;
using BlazorPWA.API.Services;
using BlazorPWA.Shared.Models;
using BlazorPWA.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BlazorPWA.API.Endpoints;

public static class SyncEndpoints
{
    public static void MapSyncEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/sync").WithTags("Sync");

        group.MapPost("/batch", async (List<SyncOperation> operations, AppDbContext db, IConflictResolver conflictResolver, ILogger<Program> logger) =>
        {
            var results = new List<SyncOperationResult>();

            foreach (var op in operations.OrderBy(o => o.CreatedAt))
            {
                try
                {
                    var result = await ProcessOperation(op, db, conflictResolver, logger);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing sync operation {OperationId}", op.Id);
                    results.Add(new SyncOperationResult
                    {
                        OperationId = op.Id,
                        Status = SyncStatus.Failed,
                        ErrorMessage = ex.Message
                    });
                }
            }

            await db.SaveChangesAsync();
            return Results.Ok(results);
        }).WithName("BatchSync");

        group.MapGet("/status/{deviceId}", async (string deviceId, AppDbContext db) =>
        {
            var pendingConflicts = await db.Conflicts
                .Where(c => !c.IsResolved)
                .AsNoTracking()
                .ToListAsync();

            var recentOps = await db.SyncOperations
                .Where(o => o.DeviceId == deviceId)
                .OrderByDescending(o => o.CreatedAt)
                .Take(50)
                .AsNoTracking()
                .ToListAsync();

            return Results.Ok(new { Conflicts = pendingConflicts, RecentOperations = recentOps });
        }).WithName("GetSyncStatus");

        group.MapPost("/resolve/{entityId:guid}", async (Guid entityId, ConflictResolution resolution, AppDbContext db) =>
        {
            var conflict = await db.Conflicts.FindAsync(entityId);
            if (conflict is null) return Results.NotFound();

            if (resolution.AcceptClient)
            {
                var report = await db.InspectionReports.FindAsync(entityId);
                if (report is not null)
                {
                    var clientData = JsonSerializer.Deserialize<InspectionReport>(conflict.ClientPayload);
                    if (clientData is not null)
                    {
                        report.Title = clientData.Title;
                        report.Description = clientData.Description;
                        report.Notes = clientData.Notes;
                        report.Version++;
                        report.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            conflict.IsResolved = true;
            conflict.Resolution = resolution.AcceptClient ? "client" : "server";
            await db.SaveChangesAsync();
            return Results.Ok(conflict);
        }).WithName("ResolveConflict");
    }

    private static async Task<SyncOperationResult> ProcessOperation(
        SyncOperation op, AppDbContext db, IConflictResolver conflictResolver, ILogger logger)
    {
        // Check for duplicate operations (idempotency)
        var existing = await db.SyncOperations.FindAsync(op.Id);
        if (existing is not null && existing.Status == SyncStatus.Completed)
        {
            return new SyncOperationResult
            {
                OperationId = op.Id,
                Status = SyncStatus.Completed,
                Message = "Already processed"
            };
        }

        // Get current server version for conflict detection
        int currentServerVersion = 0;
        if (Guid.TryParse(op.EntityId, out var entityId))
        {
            var entity = await db.InspectionReports.FindAsync(entityId);
            if (entity is not null)
                currentServerVersion = entity.Version;
        }

        // Resolve conflicts
        var resolution = conflictResolver.Resolve(op, currentServerVersion);

        if (resolution.Action == ResolutionAction.ManualResolution)
        {
            var conflict = new ConflictInfo
            {
                EntityId = Guid.Parse(op.EntityId),
                EntityType = op.EntityType,
                ClientVersion = op.ClientVersion,
                ServerVersion = currentServerVersion,
                ClientPayload = op.Payload,
                ServerPayload = await GetServerPayload(db, op.EntityId)
            };
            db.Conflicts.Add(conflict);

            op.Status = SyncStatus.Conflict;
            db.SyncOperations.Add(op);

            return new SyncOperationResult
            {
                OperationId = op.Id,
                Status = SyncStatus.Conflict,
                Message = resolution.Reason,
                ConflictInfo = conflict
            };
        }

        if (resolution.Action == ResolutionAction.AcceptServer)
        {
            op.Status = SyncStatus.Completed;
            db.SyncOperations.Add(op);
            return new SyncOperationResult
            {
                OperationId = op.Id,
                Status = SyncStatus.Completed,
                Message = resolution.Reason
            };
        }

        // AcceptClient: apply the operation
        await ApplyOperation(op, db);
        op.Status = SyncStatus.Completed;
        op.SyncedAt = DateTime.UtcNow;
        db.SyncOperations.Add(op);

        return new SyncOperationResult
        {
            OperationId = op.Id,
            Status = SyncStatus.Completed,
            Message = "Applied successfully"
        };
    }

    private static async Task ApplyOperation(SyncOperation op, AppDbContext db)
    {
        switch (op.OperationType)
        {
            case OperationType.CreateReport:
                var newReport = JsonSerializer.Deserialize<InspectionReport>(op.Payload);
                if (newReport is not null)
                {
                    newReport.SyncStatus = SyncStatus.Completed;
                    db.InspectionReports.Add(newReport);
                }
                break;

            case OperationType.UpdateReport:
                if (Guid.TryParse(op.EntityId, out var updateId))
                {
                    var report = await db.InspectionReports.FindAsync(updateId);
                    if (report is not null)
                    {
                        var updated = JsonSerializer.Deserialize<InspectionReport>(op.Payload);
                        if (updated is not null)
                        {
                            report.Title = updated.Title;
                            report.Description = updated.Description;
                            report.FacilityName = updated.FacilityName;
                            report.FacilityAddress = updated.FacilityAddress;
                            report.Notes = updated.Notes;
                            report.Version++;
                            report.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }
                break;

            case OperationType.AddNote:
                var note = JsonSerializer.Deserialize<InspectionNote>(op.Payload);
                if (note is not null)
                    db.InspectionNotes.Add(note);
                break;

            case OperationType.CompleteReport:
                if (Guid.TryParse(op.EntityId, out var completeId))
                {
                    var rpt = await db.InspectionReports.FindAsync(completeId);
                    if (rpt is not null)
                    {
                        rpt.Status = "Completed";
                        rpt.CompletedAt = DateTime.UtcNow;
                        rpt.Version++;
                        rpt.UpdatedAt = DateTime.UtcNow;
                    }
                }
                break;

            case OperationType.DeleteRecord:
                if (Guid.TryParse(op.EntityId, out var deleteId))
                {
                    var toDelete = await db.InspectionReports.FindAsync(deleteId);
                    if (toDelete is not null)
                        db.InspectionReports.Remove(toDelete);
                }
                break;
        }
    }

    private static async Task<string> GetServerPayload(AppDbContext db, string entityId)
    {
        if (Guid.TryParse(entityId, out var id))
        {
            var entity = await db.InspectionReports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (entity is not null)
                return JsonSerializer.Serialize(entity);
        }
        return "{}";
    }
}

public class SyncOperationResult
{
    public Guid OperationId { get; set; }
    public SyncStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public ConflictInfo? ConflictInfo { get; set; }
}

public class ConflictResolution
{
    public bool AcceptClient { get; set; }
}
