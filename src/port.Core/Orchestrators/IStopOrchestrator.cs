namespace port.Orchestrators;

public interface IStopOrchestrator : IOrchestrator
{
    Task<StopResult> ExecuteAsync(string containerName, CancellationToken ct = default);
}
