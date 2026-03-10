using System.Net.Http.Json;
using BlazorHybrid.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace BlazorHybrid.App.Services.Api;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // Entities
    public async Task<List<EntityDto>> GetEntitiesAsync()
    {
        var response = await _httpClient.GetAsync("api/entities");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<EntityDto>>() ?? [];
    }

    public async Task<EntityDto?> GetEntityAsync(string id)
    {
        var response = await _httpClient.GetAsync($"api/entities/{id}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<EntityDto>();
    }

    public async Task<EntityDto> CreateEntityAsync(EntityDto entity)
    {
        var response = await _httpClient.PostAsJsonAsync("api/entities", entity);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EntityDto>()
            ?? throw new InvalidOperationException("Failed to deserialize created entity");
    }

    public async Task<EntityDto> UpdateEntityAsync(EntityDto entity)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/entities/{entity.Id}", entity);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EntityDto>()
            ?? throw new InvalidOperationException("Failed to deserialize updated entity");
    }

    public async Task DeleteEntityAsync(string id)
    {
        var response = await _httpClient.DeleteAsync($"api/entities/{id}");
        response.EnsureSuccessStatusCode();
    }

    // Sync
    public async Task<SyncResultDto> SyncBatchAsync(SyncBatchDto batch)
    {
        _logger.LogInformation("Syncing batch with {Count} operations", batch.Operations.Count);
        var response = await _httpClient.PostAsJsonAsync("api/sync", batch);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SyncResultDto>()
            ?? throw new InvalidOperationException("Failed to deserialize sync result");
    }

    // Media
    public async Task<SasTokenDto> GetUploadSasTokenAsync(string containerPath)
    {
        var response = await _httpClient.PostAsJsonAsync("api/media/sas-token", new { path = containerPath });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SasTokenDto>()
            ?? throw new InvalidOperationException("Failed to get SAS token");
    }

    public async Task<MediaUploadResultDto> UploadMediaAsync(string sasUri, Stream fileStream, string contentType)
    {
        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        using var request = new HttpRequestMessage(HttpMethod.Put, sasUri);
        request.Content = content;
        request.Headers.Add("x-ms-blob-type", "BlockBlob");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return new MediaUploadResultDto
        {
            BlobUrl = sasUri.Split('?')[0],
            FileName = Path.GetFileName(new Uri(sasUri).AbsolutePath),
            FileSizeBytes = fileStream.Length
        };
    }
}
