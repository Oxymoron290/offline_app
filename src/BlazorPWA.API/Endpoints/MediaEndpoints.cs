using BlazorPWA.API.Data;
using BlazorPWA.API.Services;
using BlazorPWA.Shared;
using BlazorPWA.Shared.Models;
using BlazorPWA.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace BlazorPWA.API.Endpoints;

public static class MediaEndpoints
{
    public static void MapMediaEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/media").WithTags("Media");

        group.MapPost("/upload/{reportId:guid}", async (
            Guid reportId,
            IFormFile file,
            AppDbContext db,
            IServiceBusPublisher publisher,
            ILogger<Program> logger) =>
        {
            var report = await db.InspectionReports.FindAsync(reportId);
            if (report is null)
                return Results.NotFound("Report not found");

            var mediaType = GetMediaType(file.ContentType);
            var maxSize = GetMaxSize(mediaType);

            if (file.Length > maxSize)
                return Results.BadRequest($"File size exceeds {maxSize / (1024 * 1024)}MB limit for {mediaType}");

            var attachment = new MediaAttachment
            {
                ReportId = reportId,
                FileName = file.FileName,
                ContentType = file.ContentType,
                MediaType = mediaType,
                FileSize = file.Length,
                SyncStatus = SyncStatus.InProgress
            };

            db.MediaAttachments.Add(attachment);
            await db.SaveChangesAsync();

            // Publish to Service Bus for async processing
            var message = new MediaProcessingMessage
            {
                ReportId = reportId,
                AttachmentId = attachment.Id,
                FileName = file.FileName,
                ContentType = file.ContentType,
                MediaType = mediaType,
                BlobPath = $"{GetFolder(mediaType)}/{reportId}/{attachment.Id}/{file.FileName}"
            };

            // Store file temporarily for the worker to pick up
            var tempPath = Path.Combine(Path.GetTempPath(), "blazorpwa", attachment.Id.ToString());
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);
            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            await publisher.PublishMediaProcessingJobAsync(message);

            logger.LogInformation("Media upload initiated for attachment {AttachmentId} on report {ReportId}",
                attachment.Id, reportId);

            return Results.Accepted($"/api/media/{attachment.Id}", attachment);
        }).DisableAntiforgery().WithName("UploadMedia");

        group.MapGet("/{attachmentId:guid}", async (Guid attachmentId, AppDbContext db) =>
        {
            var attachment = await db.MediaAttachments.FindAsync(attachmentId);
            return attachment is not null ? Results.Ok(attachment) : Results.NotFound();
        }).WithName("GetMediaAttachment");

        group.MapGet("/report/{reportId:guid}", async (Guid reportId, AppDbContext db) =>
        {
            var attachments = await db.MediaAttachments
                .Where(a => a.ReportId == reportId)
                .AsNoTracking()
                .ToListAsync();
            return Results.Ok(attachments);
        }).WithName("GetReportAttachments");
    }

    private static MediaType GetMediaType(string contentType) => contentType.ToLower() switch
    {
        var ct when ct.StartsWith("image/") => MediaType.Photo,
        var ct when ct.StartsWith("video/") => MediaType.Video,
        _ => MediaType.Document
    };

    private static long GetMaxSize(MediaType mediaType) => mediaType switch
    {
        MediaType.Photo => Constants.MaxPhotoSizeMb * 1024L * 1024L,
        MediaType.Video => Constants.MaxVideoSizeMb * 1024L * 1024L,
        _ => Constants.MaxDocumentSizeMb * 1024L * 1024L
    };

    private static string GetFolder(MediaType mediaType) => mediaType switch
    {
        MediaType.Photo => Constants.PhotosFolder,
        MediaType.Video => Constants.VideosFolder,
        _ => Constants.DocumentsFolder
    };
}
