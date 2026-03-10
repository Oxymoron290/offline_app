using System.Text.Json;
using BlazorHybrid.App.Data.Models;
using BlazorHybrid.App.Data.Repositories;
using BlazorHybrid.App.Services.Api;
using BlazorHybrid.Shared.DTOs;
using BlazorHybrid.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace BlazorHybrid.App.Services.Sync;

public class SyncService : ISyncService
{
    private readonly ISyncOperationRepository _syncRepo;
    private readonly IApiClient _apiClient;
    private readonly ILogger<SyncService> _logger;
    private const int MaxRetries = 5;

    public event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;

    public SyncService(
        ISyncOperationRepository syncRepo,
        IApiClient apiClient,
        ILogger<SyncService> logger)
    {
        _syncRepo = syncRepo;
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task EnqueueAsync(string operationType, string entityType, string entityId, string? payloadJson, string? filePath = null)
    {
        var operation = new SyncOperation
        {
            OperationType = operationType,
            EntityType = entityType,
            EntityId = entityId,
            PayloadJson = payloadJson,
            FilePath = filePath,
            Status = "Pending"
        };

        await _syncRepo.InsertAsync(operation);
        _logger.LogInformation("Enqueued sync operation: {Type} {EntityType} {EntityId}",
            operationType, entityType, entityId);
    }

    public async Task ProcessQueueAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _syncRepo.GetPendingAsync();
        if (pending.Count == 0) return;

        _logger.LogInformation("Processing {Count} sync operations", pending.Count);

        var batch = new SyncBatchDto
        {
            DeviceId = DeviceInfo.Current.Idiom.ToString(),
            CaseWorkerId = Preferences.Default.Get("CaseWorkerId", "unknown"),
            Timestamp = DateTimeOffset.UtcNow,
            Operations = pending.Select(op => new SyncOperationDto
            {
                Id = op.Id,
                OperationType = Enum.Parse<OperationType>(op.OperationType),
                EntityType = Enum.Parse<Shared.Enums.EntityType>(op.EntityType),
                EntityId = op.EntityId,
                PayloadJson = op.PayloadJson,
                FilePath = op.FilePath,
                CreatedAt = op.CreatedAt
            }).ToList()
        };

        int completed = 0;
        int failed = 0;

        try
        {
            // Upload any media files first
            foreach (var op in pending.Where(o => !string.IsNullOrEmpty(o.FilePath)))
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    await UploadMediaFileAsync(op);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upload media for operation {Id}", op.Id);
                    op.RetryCount++;
                    op.LastError = ex.Message;
                    op.Status = op.RetryCount >= MaxRetries ? "Failed" : "Pending";
                    await _syncRepo.UpdateAsync(op);
                    failed++;
                }
            }

            // Send the batch
            var result = await _apiClient.SyncBatchAsync(batch);

            foreach (var opResult in result.Results)
            {
                var op = pending.FirstOrDefault(o => o.Id == opResult.OperationId);
                if (op is null) continue;

                if (opResult.Success)
                {
                    op.Status = "Completed";
                    completed++;
                }
                else
                {
                    op.RetryCount++;
                    op.LastError = opResult.ErrorMessage;
                    op.Status = op.RetryCount >= MaxRetries ? "Failed" : "Pending";
                    failed++;
                }

                await _syncRepo.UpdateAsync(op);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch sync failed");
            failed = pending.Count;
        }

        SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs
        {
            TotalOperations = pending.Count,
            CompletedOperations = completed,
            FailedOperations = failed,
            IsSyncing = false
        });

        // Clean up completed operations
        await _syncRepo.DeleteCompletedAsync();
    }

    private async Task UploadMediaFileAsync(SyncOperation operation)
    {
        if (string.IsNullOrEmpty(operation.FilePath) || !File.Exists(operation.FilePath))
            return;

        var containerPath = $"{operation.EntityType.ToLowerInvariant()}/{operation.EntityId}/{Path.GetFileName(operation.FilePath)}";
        var sasToken = await _apiClient.GetUploadSasTokenAsync(containerPath);

        await using var stream = File.OpenRead(operation.FilePath);
        var contentType = GetContentType(operation.FilePath);
        await _apiClient.UploadMediaAsync(sasToken.SasUri, stream, contentType);
    }

    private static string GetContentType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".mp4" => "video/mp4",
            ".pdf" => "application/pdf",
            ".doc" or ".docx" => "application/msword",
            _ => "application/octet-stream"
        };
    }

    public Task<int> GetPendingCountAsync() => _syncRepo.GetPendingCountAsync();
    public Task<int> GetFailedCountAsync() => _syncRepo.GetFailedCountAsync();
}
