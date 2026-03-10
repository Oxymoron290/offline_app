using BlazorHybrid.Shared.DTOs;
using BlazorHybrid.Shared.Enums;

namespace BlazorHybrid.App.Tests;

public class SharedModelsTests
{
    [Fact]
    public void EntityDto_DefaultValues_AreCorrect()
    {
        var dto = new EntityDto();

        Assert.Equal(string.Empty, dto.Id);
        Assert.Equal(string.Empty, dto.CaseWorkerId);
        Assert.Equal(string.Empty, dto.Name);
        Assert.False(dto.IsDeleted);
        Assert.NotNull(dto.FormFields);
        Assert.Empty(dto.FormFields);
    }

    [Fact]
    public void SyncBatchDto_CanAddOperations()
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
                    EntityId = "entity-1"
                },
                new SyncOperationDto
                {
                    Id = "op-2",
                    OperationType = OperationType.Update,
                    EntityType = Shared.Enums.EntityType.Photo,
                    EntityId = "photo-1"
                }
            ]
        };

        Assert.Equal(2, batch.Operations.Count);
        Assert.Equal(OperationType.Create, batch.Operations[0].OperationType);
        Assert.Equal(OperationType.Update, batch.Operations[1].OperationType);
    }

    [Fact]
    public void SyncResultDto_TracksSuccessAndFailure()
    {
        var result = new SyncResultDto
        {
            Success = false,
            ServerTimestamp = DateTimeOffset.UtcNow,
            Results =
            [
                new SyncOperationResultDto { OperationId = "op-1", Success = true },
                new SyncOperationResultDto { OperationId = "op-2", Success = false, ErrorMessage = "Conflict" }
            ]
        };

        Assert.False(result.Success);
        Assert.Equal(2, result.Results.Count);
        Assert.True(result.Results[0].Success);
        Assert.False(result.Results[1].Success);
        Assert.Equal("Conflict", result.Results[1].ErrorMessage);
    }

    [Theory]
    [InlineData(SyncStatus.Pending)]
    [InlineData(SyncStatus.InProgress)]
    [InlineData(SyncStatus.Completed)]
    [InlineData(SyncStatus.Failed)]
    [InlineData(SyncStatus.Cancelled)]
    public void SyncStatus_AllValuesAreDefined(SyncStatus status)
    {
        Assert.True(Enum.IsDefined(status));
    }

    [Theory]
    [InlineData(OperationType.Create)]
    [InlineData(OperationType.Update)]
    [InlineData(OperationType.Delete)]
    public void OperationType_AllValuesAreDefined(OperationType type)
    {
        Assert.True(Enum.IsDefined(type));
    }
}
