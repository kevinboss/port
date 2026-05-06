namespace port.Orchestrators;

public sealed record PruneResult(IReadOnlyList<ImageRemovalResult> Removals);
