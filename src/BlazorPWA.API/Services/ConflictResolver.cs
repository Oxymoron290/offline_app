using BlazorPWA.Shared.Models;
using BlazorPWA.Shared.Enums;

namespace BlazorPWA.API.Services;

public interface IConflictResolver
{
    ConflictResolutionResult Resolve(SyncOperation operation, int currentServerVersion);
}

public class ConflictResolver : IConflictResolver
{
    private readonly ILogger<ConflictResolver> _logger;

    public ConflictResolver(ILogger<ConflictResolver> logger)
    {
        _logger = logger;
    }

    public ConflictResolutionResult Resolve(SyncOperation operation, int currentServerVersion)
    {
        // New records: client always wins
        if (operation.OperationType == OperationType.CreateReport)
        {
            return new ConflictResolutionResult
            {
                Action = ResolutionAction.AcceptClient,
                Reason = "New record — client wins"
            };
        }

        // No conflict if versions match
        if (operation.ServerVersion == currentServerVersion)
        {
            return new ConflictResolutionResult
            {
                Action = ResolutionAction.AcceptClient,
                Reason = "Versions match — no conflict"
            };
        }

        // Immutable operations: server wins
        if (operation.OperationType is OperationType.CompleteReport or OperationType.CompleteTask)
        {
            _logger.LogWarning("Conflict on immutable operation {OperationType} for entity {EntityId}. Server wins.",
                operation.OperationType, operation.EntityId);
            return new ConflictResolutionResult
            {
                Action = ResolutionAction.AcceptServer,
                Reason = "Immutable operation — server wins"
            };
        }

        // Additive operations: accept (no conflict possible)
        if (operation.OperationType is OperationType.AddPhoto or OperationType.AddVideo
            or OperationType.AddDocument or OperationType.AddNote)
        {
            return new ConflictResolutionResult
            {
                Action = ResolutionAction.AcceptClient,
                Reason = "Additive operation — no conflict"
            };
        }

        // Edits with version mismatch: manual resolution required
        _logger.LogWarning("Version conflict on {EntityId}: client={ClientVer}, server={ServerVer}. Manual resolution required.",
            operation.EntityId, operation.ServerVersion, currentServerVersion);
        return new ConflictResolutionResult
        {
            Action = ResolutionAction.ManualResolution,
            Reason = $"Version mismatch (client: {operation.ServerVersion}, server: {currentServerVersion})"
        };
    }
}

public class ConflictResolutionResult
{
    public ResolutionAction Action { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public enum ResolutionAction
{
    AcceptClient,
    AcceptServer,
    ManualResolution
}
