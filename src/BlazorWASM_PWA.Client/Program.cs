using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorWASM_PWA.Client;
using BlazorWASM_PWA.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HTTP client configured for the API backend
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress)
});

// Offline-first services
builder.Services.AddScoped<IIndexedDbService, IndexedDbService>();
builder.Services.AddScoped<IConnectivityService, ConnectivityService>();
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<BlobCacheService>();

var host = builder.Build();

// Initialize IndexedDB on startup
var indexedDb = host.Services.GetRequiredService<IIndexedDbService>();
await indexedDb.InitializeDatabaseAsync();

await host.RunAsync();
