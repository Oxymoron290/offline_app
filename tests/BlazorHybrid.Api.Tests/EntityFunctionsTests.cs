using BlazorHybrid.Api.Functions;
using BlazorHybrid.Api.Services;
using BlazorHybrid.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BlazorHybrid.Api.Tests;

public class EntityFunctionsTests
{
    private readonly Mock<ICosmosDbService> _cosmosDbMock;
    private readonly Mock<ILogger<EntityFunctions>> _loggerMock;
    private readonly EntityFunctions _functions;

    public EntityFunctionsTests()
    {
        _cosmosDbMock = new Mock<ICosmosDbService>();
        _loggerMock = new Mock<ILogger<EntityFunctions>>();
        _functions = new EntityFunctions(_cosmosDbMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetEntities_WithoutCaseWorkerId_ReturnsBadRequest()
    {
        var context = new DefaultHttpContext();
        var request = context.Request;

        var result = await _functions.GetEntities(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetEntities_WithCaseWorkerId_ReturnsOk()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?caseWorkerId=worker-1");

        var expectedEntities = new List<EntityDto>
        {
            new() { Id = "1", Name = "Test Entity", CaseWorkerId = "worker-1" }
        };
        _cosmosDbMock.Setup(x => x.GetEntitiesAsync("worker-1")).ReturnsAsync(expectedEntities);

        var result = await _functions.GetEntities(context.Request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var entities = Assert.IsType<List<EntityDto>>(okResult.Value);
        Assert.Single(entities);
    }

    [Fact]
    public async Task GetEntity_NotFound_Returns404()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?caseWorkerId=worker-1");

        _cosmosDbMock.Setup(x => x.GetEntityAsync("missing", "worker-1")).ReturnsAsync((EntityDto?)null);

        var result = await _functions.GetEntity(context.Request, "missing");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetEntity_Found_ReturnsEntity()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?caseWorkerId=worker-1");

        var entity = new EntityDto { Id = "1", Name = "Test", CaseWorkerId = "worker-1" };
        _cosmosDbMock.Setup(x => x.GetEntityAsync("1", "worker-1")).ReturnsAsync(entity);

        var result = await _functions.GetEntity(context.Request, "1");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<EntityDto>(okResult.Value);
        Assert.Equal("Test", returned.Name);
    }

    [Fact]
    public async Task DeleteEntity_WithoutCaseWorkerId_ReturnsBadRequest()
    {
        var context = new DefaultHttpContext();

        var result = await _functions.DeleteEntity(context.Request, "1");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteEntity_WithCaseWorkerId_ReturnsNoContent()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?caseWorkerId=worker-1");

        var result = await _functions.DeleteEntity(context.Request, "1");

        Assert.IsType<NoContentResult>(result);
        _cosmosDbMock.Verify(x => x.DeleteEntityAsync("1", "worker-1"), Times.Once);
    }
}
