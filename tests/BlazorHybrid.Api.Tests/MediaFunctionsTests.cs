using BlazorHybrid.Api.Functions;
using BlazorHybrid.Api.Services;
using BlazorHybrid.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BlazorHybrid.Api.Tests;

public class MediaFunctionsTests
{
    private readonly Mock<IBlobStorageService> _blobMock;
    private readonly Mock<ILogger<MediaFunctions>> _loggerMock;
    private readonly MediaFunctions _functions;

    public MediaFunctionsTests()
    {
        _blobMock = new Mock<IBlobStorageService>();
        _loggerMock = new Mock<ILogger<MediaFunctions>>();
        _functions = new MediaFunctions(_blobMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetUploadSasToken_EmptyBody_ReturnsBadRequest()
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("null"));
        context.Request.ContentType = "application/json";

        var result = await _functions.GetUploadSasToken(context.Request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetUploadSasToken_ValidPath_ReturnsSasToken()
    {
        var expectedToken = new SasTokenDto
        {
            SasUri = "https://storage.blob.core.windows.net/media/test.jpg?sv=2023&sig=xxx",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
        };

        _blobMock.Setup(x => x.GenerateUploadSasTokenAsync("photos/entity-1/test.jpg"))
            .ReturnsAsync(expectedToken);

        var json = System.Text.Json.JsonSerializer.Serialize(new { path = "photos/entity-1/test.jpg" });
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        context.Request.ContentType = "application/json";

        var result = await _functions.GetUploadSasToken(context.Request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var token = Assert.IsType<SasTokenDto>(okResult.Value);
        Assert.Contains("test.jpg", token.SasUri);
    }

    [Fact]
    public async Task UploadMedia_WithoutPath_ReturnsBadRequest()
    {
        var context = new DefaultHttpContext();

        var result = await _functions.UploadMedia(context.Request);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
