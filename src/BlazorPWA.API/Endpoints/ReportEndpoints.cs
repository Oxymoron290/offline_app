using BlazorPWA.API.Data;
using BlazorPWA.Shared.Models;
using BlazorPWA.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace BlazorPWA.API.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/reports").WithTags("Reports");

        group.MapGet("/", async (AppDbContext db, int page = 1, int pageSize = 20) =>
        {
            var reports = await db.InspectionReports
                .OrderByDescending(r => r.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(r => r.Attachments)
                .Include(r => r.InspectionNotes)
                .AsNoTracking()
                .ToListAsync();
            return Results.Ok(reports);
        }).WithName("GetReports");

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var report = await db.InspectionReports
                .Include(r => r.Attachments)
                .Include(r => r.InspectionNotes)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);
            return report is not null ? Results.Ok(report) : Results.NotFound();
        }).WithName("GetReport");

        group.MapPost("/", async (InspectionReport report, AppDbContext db) =>
        {
            report.CreatedAt = DateTime.UtcNow;
            report.UpdatedAt = DateTime.UtcNow;
            report.SyncStatus = SyncStatus.Completed;
            db.InspectionReports.Add(report);
            await db.SaveChangesAsync();
            return Results.Created($"/api/reports/{report.Id}", report);
        }).WithName("CreateReport");

        group.MapPut("/{id:guid}", async (Guid id, InspectionReport updated, AppDbContext db) =>
        {
            var report = await db.InspectionReports.FindAsync(id);
            if (report is null) return Results.NotFound();

            report.Title = updated.Title;
            report.Description = updated.Description;
            report.FacilityName = updated.FacilityName;
            report.FacilityAddress = updated.FacilityAddress;
            report.Notes = updated.Notes;
            report.Status = updated.Status;
            report.Version++;
            report.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(report);
        }).WithName("UpdateReport");

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var report = await db.InspectionReports.FindAsync(id);
            if (report is null) return Results.NotFound();
            db.InspectionReports.Remove(report);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("DeleteReport");
    }
}
