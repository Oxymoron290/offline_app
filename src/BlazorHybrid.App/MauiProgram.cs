using BlazorHybrid.App.Data;
using BlazorHybrid.App.Data.Repositories;
using BlazorHybrid.App.Services.Api;
using BlazorHybrid.App.Services.Auth;
using BlazorHybrid.App.Services.Connectivity;
using BlazorHybrid.App.Services.Media;
using BlazorHybrid.App.Services.Sync;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace BlazorHybrid.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

        // Database
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "blazorhybrid.db3");
        var dbContext = new AppDbContext(dbPath);
        builder.Services.AddSingleton(dbContext);

        // Repositories
        builder.Services.AddSingleton<IEntityRepository, EntityRepository>();
        builder.Services.AddSingleton<ISyncOperationRepository, SyncOperationRepository>();
        builder.Services.AddSingleton<IMediaRepository, MediaRepository>();

        // Auth
        var authConfig = new AuthConfiguration
        {
            ClientId = "YOUR_CLIENT_ID",
            TenantId = "YOUR_TENANT_ID",
            Scopes = ["api://YOUR_API_CLIENT_ID/.default"],
            RedirectUri = "msauth://com.companyname.blazorhybrid.app"
        };
        builder.Services.AddSingleton(authConfig);

        var pca = PublicClientApplicationBuilder
            .Create(authConfig.ClientId)
            .WithAuthority($"https://login.microsoftonline.com/{authConfig.TenantId}")
            .WithRedirectUri(authConfig.RedirectUri)
            .Build();
        builder.Services.AddSingleton(pca);
        builder.Services.AddSingleton<IAuthService, AuthService>();

        // HTTP client with auth handler
        builder.Services.AddTransient<AuthenticatedHttpMessageHandler>();
        builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://YOUR_FUNCTION_APP.azurewebsites.net");
            client.Timeout = TimeSpan.FromSeconds(60);
        }).AddHttpMessageHandler<AuthenticatedHttpMessageHandler>();

        // Services
        builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();
        builder.Services.AddSingleton<IMediaService, MediaService>();
        builder.Services.AddSingleton<ISyncService, SyncService>();
        builder.Services.AddSingleton<BackgroundSyncWorker>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Initialize database and start background sync
        Task.Run(async () =>
        {
            var db = app.Services.GetRequiredService<AppDbContext>();
            await db.InitializeAsync();

            var syncWorker = app.Services.GetRequiredService<BackgroundSyncWorker>();
            syncWorker.Start();
        });

        return app;
    }
}
