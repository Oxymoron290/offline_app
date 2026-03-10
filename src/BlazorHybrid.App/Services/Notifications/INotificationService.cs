namespace BlazorHybrid.App.Services.Notifications;

public enum NotificationLevel
{
    Info,
    Success,
    Warning,
    Error
}

public class AppNotification
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public string Message { get; init; } = string.Empty;
    public NotificationLevel Level { get; init; }
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.Now;
}

public interface INotificationService
{
    event Action<AppNotification>? OnNotify;
    void Notify(string message, NotificationLevel level = NotificationLevel.Info);
}
