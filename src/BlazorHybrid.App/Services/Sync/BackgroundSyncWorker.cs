using BlazorHybrid.App.Services.Connectivity;
using Microsoft.Extensions.Logging;

namespace BlazorHybrid.App.Services.Sync;

/// <summary>
/// Background worker that periodically processes the sync queue when connectivity is available.
/// </summary>
public class BackgroundSyncWorker : IDisposable
{
    private readonly ISyncService _syncService;
    private readonly IConnectivityService _connectivity;
    private readonly ILogger<BackgroundSyncWorker> _logger;
    private readonly PeriodicTimer _timer;
    private CancellationTokenSource? _cts;
    private Task? _workerTask;

    public BackgroundSyncWorker(
        ISyncService syncService,
        IConnectivityService connectivity,
        ILogger<BackgroundSyncWorker> logger,
        TimeSpan? interval = null)
    {
        _syncService = syncService;
        _connectivity = connectivity;
        _logger = logger;
        _timer = new PeriodicTimer(interval ?? TimeSpan.FromSeconds(30));
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _workerTask = RunAsync(_cts.Token);
        _logger.LogInformation("Background sync worker started");
    }

    public async Task StopAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
            if (_workerTask is not null)
            {
                await _workerTask;
            }
            _logger.LogInformation("Background sync worker stopped");
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _timer.WaitForNextTickAsync(cancellationToken);

                if (_connectivity.IsConnected)
                {
                    _logger.LogDebug("Connectivity available, processing sync queue");
                    await _syncService.ProcessQueueAsync(cancellationToken);
                }
                else
                {
                    _logger.LogDebug("No connectivity, skipping sync");
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background sync worker");
            }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _timer.Dispose();
    }
}
