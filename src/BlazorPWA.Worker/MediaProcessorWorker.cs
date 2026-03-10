using Azure.Messaging.ServiceBus;
using BlazorPWA.Shared;
using BlazorPWA.Shared.Models;
using BlazorPWA.Shared.Enums;
using System.Text.Json;

namespace BlazorPWA.Worker;

public class MediaProcessorWorker : BackgroundService
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly BlobStorageService _blobStorage;
    private readonly ILogger<MediaProcessorWorker> _logger;
    private ServiceBusProcessor? _processor;

    public MediaProcessorWorker(
        ServiceBusClient serviceBusClient,
        BlobStorageService blobStorage,
        ILogger<MediaProcessorWorker> logger)
    {
        _serviceBusClient = serviceBusClient;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor = _serviceBusClient.CreateProcessor(Constants.MediaQueueName, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 5,
            AutoCompleteMessages = false,
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(10)
        });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        _logger.LogInformation("Starting media processor worker...");
        await _processor.StartProcessingAsync(stoppingToken);

        // Keep running until cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Stopping media processor worker...");
        }

        await _processor.StopProcessingAsync();
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var message = args.Message;
        _logger.LogInformation("Processing media message {MessageId}", message.MessageId);

        try
        {
            var mediaMessage = JsonSerializer.Deserialize<MediaProcessingMessage>(message.Body.ToString());
            if (mediaMessage is null)
            {
                _logger.LogWarning("Could not deserialize message {MessageId}", message.MessageId);
                await args.DeadLetterMessageAsync(message, "InvalidMessage", "Could not deserialize message body");
                return;
            }

            // Read temp file
            var tempPath = Path.Combine(Path.GetTempPath(), "blazorpwa", mediaMessage.AttachmentId.ToString());
            if (!File.Exists(tempPath))
            {
                _logger.LogWarning("Temp file not found for attachment {AttachmentId}", mediaMessage.AttachmentId);
                await args.DeadLetterMessageAsync(message, "FileNotFound", "Temporary file not found");
                return;
            }

            // Upload to Blob Storage
            var containerName = GetContainerName(mediaMessage.MediaType);
            using var fileStream = File.OpenRead(tempPath);
            var blobUrl = await _blobStorage.UploadAsync(
                containerName,
                mediaMessage.BlobPath,
                fileStream,
                mediaMessage.ContentType,
                args.CancellationToken);

            _logger.LogInformation("Uploaded media {AttachmentId} to {BlobUrl}", mediaMessage.AttachmentId, blobUrl);

            // Clean up temp file
            try { File.Delete(tempPath); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete temp file {TempPath}", tempPath); }

            await args.CompleteMessageAsync(message);
            _logger.LogInformation("Completed processing message {MessageId}", message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {MessageId}", message.MessageId);
            if (message.DeliveryCount >= Constants.MaxRetryCount)
            {
                await args.DeadLetterMessageAsync(message, "MaxRetriesExceeded", ex.Message);
            }
            else
            {
                await args.AbandonMessageAsync(message);
            }
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus processing error. Source: {Source}, Entity: {Entity}",
            args.ErrorSource, args.EntityPath);
        return Task.CompletedTask;
    }

    private static string GetContainerName(MediaType mediaType) => mediaType switch
    {
        MediaType.Photo => Constants.PhotosFolder,
        MediaType.Video => Constants.VideosFolder,
        _ => Constants.DocumentsFolder
    };

    public override void Dispose()
    {
        if (_processor is not null)
        {
            _processor.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        base.Dispose();
    }
}
