using BlazorHybrid.Api.Services;
using BlazorHybrid.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BlazorHybrid.Api.Functions;

public class SyncFunctions
{
    private readonly ICosmosDbService _cosmosDb;
    private readonly ILogger<SyncFunctions> _logger;

    public SyncFunctions(ICosmosDbService cosmosDb, ILogger<SyncFunctions> logger)
    {
        _cosmosDb = cosmosDb;
        _logger = logger;
    }

    [Function("SyncBatch")]
    public async Task<IActionResult> SyncBatch(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sync")] HttpRequest req)
    {
        var batch = await req.ReadFromJsonAsync<SyncBatchDto>();
        if (batch is null || batch.Operations.Count == 0)
            return new BadRequestObjectResult("Invalid or empty sync batch");

        _logger.LogInformation("Processing sync batch: {Count} operations from device {DeviceId}",
            batch.Operations.Count, batch.DeviceId);

        var result = await _cosmosDb.ProcessSyncBatchAsync(batch);

        _logger.LogInformation("Sync batch complete: {Success}/{Total} succeeded",
            result.Results.Count(r => r.Success), result.Results.Count);

        return new OkObjectResult(result);
    }
}
