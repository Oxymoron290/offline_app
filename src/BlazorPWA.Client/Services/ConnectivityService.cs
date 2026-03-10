using Microsoft.JSInterop;

namespace BlazorPWA.Client.Services;

public class ConnectivityService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<ConnectivityService>? _dotNetRef;
    public bool IsOnline { get; private set; } = true;
    public event Action<bool>? OnConnectivityChanged;

    private static ConnectivityService? _instance;

    public ConnectivityService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        _dotNetRef = DotNetObjectReference.Create(this);
        IsOnline = await _jsRuntime.InvokeAsync<bool>("eval", "navigator.onLine");
        await _jsRuntime.InvokeVoidAsync("eval", @"
            window.addEventListener('online', () => DotNet.invokeMethodAsync('BlazorPWA.Client', 'OnConnectivityChange', true));
            window.addEventListener('offline', () => DotNet.invokeMethodAsync('BlazorPWA.Client', 'OnConnectivityChange', false));
        ");
    }

    [JSInvokable("OnConnectivityChange")]
    public static void OnConnectivityChange(bool isOnline)
    {
        _instance?.UpdateConnectivity(isOnline);
    }

    public void RegisterAsInstance() => _instance = this;

    private void UpdateConnectivity(bool isOnline)
    {
        IsOnline = isOnline;
        OnConnectivityChanged?.Invoke(isOnline);
    }

    public async ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();
        if (_instance == this) _instance = null;
    }
}
