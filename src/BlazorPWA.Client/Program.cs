using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorPWA.Client;
using BlazorPWA.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// Register services
builder.Services.AddScoped<IndexedDbService>();
builder.Services.AddScoped<ConnectivityService>();
builder.Services.AddScoped<SyncQueueService>();

var host = builder.Build();

// Initialize connectivity monitoring
var connectivity = host.Services.GetRequiredService<ConnectivityService>();
connectivity.RegisterAsInstance();
await connectivity.InitializeAsync();

await host.RunAsync();
