namespace port.Orchestrators;

public interface IListOrchestrator : IOrchestrator
{
    Task<ListResult> ExecuteAsync(string? identifier, CancellationToken ct = default);
}
