using Microsoft.JSInterop;

namespace BlazorWASM_PWA.Client.Services;

public class ConnectivityService : IConnectivityService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;
    private DotNetObjectReference<ConnectivityService>? _dotNetRef;
    private Timer? _healthCheckTimer;
    private static readonly TimeSpan HealthCheckInterval = TimeSpan.FromSeconds(30);

    public bool IsOnline { get; private set; } = true;
    public event Action<bool> OnConnectivityChanged = delegate { };

    public ConnectivityService(IJSRuntime jsRuntime, HttpClient httpClient)
    {
        _jsRuntime = jsRuntime;
        _httpClient = httpClient;
    }

    public async Task InitializeAsync()
    {
        _dotNetRef = DotNetObjectReference.Create(this);
        var browserOnline = await _jsRuntime.InvokeAsync<bool>("connectivity.initialize", _dotNetRef);

        // Browser says online — verify the API is actually reachable
        if (browserOnline)
        {
            var apiReachable = await CheckApiHealthAsync();
            SetStatus(apiReachable);
        }
        else
        {
            SetStatus(false);
        }

        _healthCheckTimer = new Timer(_ => _ = PeriodicHealthCheckAsync(), null, HealthCheckInterval, HealthCheckInterval);
    }

    [JSInvokable]
    public async void OnStatusChanged(bool isOnline)
    {
        if (!isOnline)
        {
            SetStatus(false);
            return;
        }

        // Browser came back online — verify API is reachable before reporting online
        var apiReachable = await CheckApiHealthAsync();
        SetStatus(apiReachable);
    }

    private void SetStatus(bool online)
    {
        if (IsOnline == online)
            return;

        IsOnline = online;
        OnConnectivityChanged.Invoke(online);
    }

    private async Task PeriodicHealthCheckAsync()
    {
        var apiReachable = await CheckApiHealthAsync();
        SetStatus(apiReachable);
    }

    private async Task<bool> CheckApiHealthAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.GetAsync("api/health", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_healthCheckTimer is not null)
        {
            await _healthCheckTimer.DisposeAsync();
            _healthCheckTimer = null;
        }

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
