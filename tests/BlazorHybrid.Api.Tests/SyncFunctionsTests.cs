using BlazorHybrid.Api.Functions;
using BlazorHybrid.Api.Services;
using BlazorHybrid.Shared.DTOs;
using BlazorHybrid.Shared.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BlazorHybrid.Api.Tests;

public class SyncFunctionsTests
{
    private readonly Mock<ICosmosDbService> _cosmosDbMock;
    private readonly Mock<ILogger<SyncFunctions>> _loggerMock;
    private readonly SyncFunctions _functions;

    public SyncFunctionsTests()
    {
        _cosmosDbMock = new Mock<ICosmosDbService>();
        _loggerMock = new Mock<ILogger<SyncFunctions>>();
        _functions = new SyncFunctions(_cosmosDbMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task SyncBatch_EmptyBody_ReturnsBadRequest()
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("null"));
        context.Request.ContentType = "application/json";

        var result = await _functions.SyncBatch(context.Request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SyncBatch_ValidBatch_ReturnsOk()
    {
        var batch = new SyncBatchDto
        {
            DeviceId = "device-1",
            CaseWorkerId = "worker-1",
            Timestamp = DateTimeOffset.UtcNow,
            Operations =
            [
                new SyncOperationDto
                {
                    Id = "op-1",
                    OperationType = OperationType.Create,
                    EntityType = Shared.Enums.EntityType.Entity,
                    EntityId = "entity-1",
                    PayloadJson = "{}",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        var expectedResult = new SyncResultDto
        {
            Success = true,
            ServerTimestamp = DateTimeOffset.UtcNow,
            Results = [new SyncOperationResultDto { OperationId = "op-1", Success = true }]
        };

        _cosmosDbMock.Setup(x => x.ProcessSyncBatchAsync(It.IsAny<SyncBatchDto>()))
            .ReturnsAsync(expectedResult);

        var json = System.Text.Json.JsonSerializer.Serialize(batch);
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        context.Request.ContentType = "application/json";

        var result = await _functions.SyncBatch(context.Request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var syncResult = Assert.IsType<SyncResultDto>(okResult.Value);
        Assert.True(syncResult.Success);
    }
}
