namespace port.Orchestrators;

public interface IPruneOrchestrator : IOrchestrator
{
    Task<PruneResult> ExecuteAsync(string? identifier, CancellationToken ct = default);
}
