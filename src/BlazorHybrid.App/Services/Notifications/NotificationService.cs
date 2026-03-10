namespace BlazorHybrid.App.Services.Notifications;

public class NotificationService : INotificationService
{
    public event Action<AppNotification>? OnNotify;

    public void Notify(string message, NotificationLevel level = NotificationLevel.Info)
    {
        OnNotify?.Invoke(new AppNotification { Message = message, Level = level });
    }
}
