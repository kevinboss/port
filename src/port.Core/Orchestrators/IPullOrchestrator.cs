namespace port.Orchestrators;

public interface IPullOrchestrator : IOrchestrator
{
    Task<PullResult> ExecuteAsync(string identifier, string? tag, CancellationToken ct = default);
}
