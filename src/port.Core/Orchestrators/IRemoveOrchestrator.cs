namespace port.Orchestrators;

public interface IRemoveOrchestrator : IOrchestrator
{
    Task<RemoveResult> ExecuteAsync(
        string identifier,
        string? tag,
        bool recursive,
        CancellationToken ct = default
    );
}
