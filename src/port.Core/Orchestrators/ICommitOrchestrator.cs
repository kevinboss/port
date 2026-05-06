namespace port.Orchestrators;

public interface ICommitOrchestrator : IOrchestrator
{
    Task<CommitResult> ExecuteAsync(
        string containerName,
        string tag,
        bool overwrite,
        bool @switch,
        CancellationToken ct = default
    );
}
