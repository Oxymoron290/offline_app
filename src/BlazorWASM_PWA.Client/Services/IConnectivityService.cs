namespace BlazorWASM_PWA.Client.Services;

public interface IConnectivityService : IAsyncDisposable
{
    bool IsOnline { get; }
    event Action<bool> OnConnectivityChanged;
    Task InitializeAsync();
}
