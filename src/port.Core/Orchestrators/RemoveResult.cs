namespace port.Orchestrators;

public sealed record RemoveResult(IReadOnlyList<ImageRemovalResult> Removals);
