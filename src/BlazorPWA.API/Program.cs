using Azure.Identity;
using Azure.Messaging.ServiceBus;
using BlazorPWA.API.Data;
using BlazorPWA.API.Endpoints;
using BlazorPWA.API.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Azure Service Bus
builder.Services.AddSingleton(sp =>
{
    var connectionString = builder.Configuration["ServiceBus:ConnectionString"];
    if (!string.IsNullOrEmpty(connectionString))
        return new ServiceBusClient(connectionString);

    var fullyQualifiedNamespace = builder.Configuration["ServiceBus:FullyQualifiedNamespace"];
    return new ServiceBusClient(fullyQualifiedNamespace, new DefaultAzureCredential());
});
builder.Services.AddSingleton<IServiceBusPublisher, ServiceBusPublisher>();

// Services
builder.Services.AddSingleton<IConflictResolver, ConflictResolver>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["*"])
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();

// Map endpoints
app.MapReportEndpoints();
app.MapSyncEndpoints();
app.MapMediaEndpoints();
app.MapHealthEndpoints();

app.Run();
