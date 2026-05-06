namespace port.Orchestrators;

public interface IRunOrchestrator : IOrchestrator
{
    Task<RunResult> ExecuteAsync(
        string identifier,
        string tag,
        bool reset,
        CancellationToken ct = default
    );
}
