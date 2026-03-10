using Azure.Messaging.ServiceBus;
using BlazorPWA.Shared;
using BlazorPWA.Shared.Models;
using System.Text.Json;

namespace BlazorPWA.API.Services;

public interface IServiceBusPublisher
{
    Task PublishMediaProcessingJobAsync(MediaProcessingMessage message, CancellationToken cancellationToken = default);
}

public class ServiceBusPublisher : IServiceBusPublisher, IAsyncDisposable
{
    private readonly ServiceBusSender _sender;
    private readonly ILogger<ServiceBusPublisher> _logger;

    public ServiceBusPublisher(ServiceBusClient client, ILogger<ServiceBusPublisher> logger)
    {
        _sender = client.CreateSender(Constants.MediaQueueName);
        _logger = logger;
    }

    public async Task PublishMediaProcessingJobAsync(MediaProcessingMessage message, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message);
        var sbMessage = new ServiceBusMessage(json)
        {
            ContentType = "application/json",
            MessageId = message.Id.ToString(),
            Subject = message.MediaType.ToString(),
            ApplicationProperties =
            {
                ["reportId"] = message.ReportId.ToString(),
                ["attachmentId"] = message.AttachmentId.ToString()
            }
        };

        await _sender.SendMessageAsync(sbMessage, cancellationToken);
        _logger.LogInformation("Published media processing job {MessageId} for attachment {AttachmentId}", message.Id, message.AttachmentId);
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
    }
}
