namespace port.Orchestrators;

public interface IResetOrchestrator : IOrchestrator
{
    Task<ResetResult> ExecuteAsync(string containerName, CancellationToken ct = default);
}
