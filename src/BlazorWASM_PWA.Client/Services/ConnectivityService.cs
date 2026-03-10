using Microsoft.JSInterop;

namespace BlazorWASM_PWA.Client.Services;

public class ConnectivityService : IConnectivityService
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<ConnectivityService>? _dotNetRef;

    public bool IsOnline { get; private set; } = true;
    public event Action<bool> OnConnectivityChanged = delegate { };

    public ConnectivityService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        _dotNetRef = DotNetObjectReference.Create(this);
        IsOnline = await _jsRuntime.InvokeAsync<bool>("connectivity.initialize", _dotNetRef);
    }

    [JSInvokable]
    public void OnStatusChanged(bool isOnline)
    {
        IsOnline = isOnline;
        OnConnectivityChanged.Invoke(isOnline);
    }

    public async ValueTask DisposeAsync()
    {
        if (_dotNetRef is not null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("connectivity.dispose");
            }
            catch (JSDisconnectedException)
            {
                // Circuit disconnected, nothing to clean up on the JS side
            }

            _dotNetRef.Dispose();
            _dotNetRef = null;
        }

        GC.SuppressFinalize(this);
    }
}
