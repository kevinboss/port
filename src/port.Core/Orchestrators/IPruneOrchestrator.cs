namespace port.Orchestrators;

public interface IPruneOrchestrator : IOrchestrator
{
    Task<PruneResult> ExecuteAsync(string? identifier, CancellationToken cancellationToken);
}
