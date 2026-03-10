using BlazorHybrid.Api.Services;
using BlazorHybrid.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BlazorHybrid.Api.Functions;

public class EntityFunctions
{
    private readonly ICosmosDbService _cosmosDb;
    private readonly ILogger<EntityFunctions> _logger;

    public EntityFunctions(ICosmosDbService cosmosDb, ILogger<EntityFunctions> logger)
    {
        _cosmosDb = cosmosDb;
        _logger = logger;
    }

    [Function("GetEntities")]
    public async Task<IActionResult> GetEntities(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "entities")] HttpRequest req)
    {
        var caseWorkerId = req.Query["caseWorkerId"].ToString();
        if (string.IsNullOrEmpty(caseWorkerId))
        {
            return new BadRequestObjectResult("caseWorkerId query parameter is required");
        }

        var entities = await _cosmosDb.GetEntitiesAsync(caseWorkerId);
        return new OkObjectResult(entities);
    }

    [Function("GetEntity")]
    public async Task<IActionResult> GetEntity(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "entities/{id}")] HttpRequest req,
        string id)
    {
        var caseWorkerId = req.Query["caseWorkerId"].ToString();
        if (string.IsNullOrEmpty(caseWorkerId))
        {
            return new BadRequestObjectResult("caseWorkerId query parameter is required");
        }

        var entity = await _cosmosDb.GetEntityAsync(id, caseWorkerId);
        if (entity is null)
            return new NotFoundResult();

        return new OkObjectResult(entity);
    }

    [Function("CreateEntity")]
    public async Task<IActionResult> CreateEntity(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "entities")] HttpRequest req)
    {
        var entity = await req.ReadFromJsonAsync<EntityDto>();
        if (entity is null)
            return new BadRequestObjectResult("Invalid entity payload");

        var created = await _cosmosDb.CreateEntityAsync(entity);
        _logger.LogInformation("Created entity {Id}", created.Id);
        return new CreatedResult($"/api/entities/{created.Id}", created);
    }

    [Function("UpdateEntity")]
    public async Task<IActionResult> UpdateEntity(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "entities/{id}")] HttpRequest req,
        string id)
    {
        var entity = await req.ReadFromJsonAsync<EntityDto>();
        if (entity is null)
            return new BadRequestObjectResult("Invalid entity payload");

        entity.Id = id;
        var updated = await _cosmosDb.UpdateEntityAsync(entity);
        return new OkObjectResult(updated);
    }

    [Function("DeleteEntity")]
    public async Task<IActionResult> DeleteEntity(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "entities/{id}")] HttpRequest req,
        string id)
    {
        var caseWorkerId = req.Query["caseWorkerId"].ToString();
        if (string.IsNullOrEmpty(caseWorkerId))
            return new BadRequestObjectResult("caseWorkerId query parameter is required");

        await _cosmosDb.DeleteEntityAsync(id, caseWorkerId);
        return new NoContentResult();
    }
}
