namespace port.Orchestrators;

public abstract record OrchestrationEvent;

public sealed record StatusEvent(string Message) : OrchestrationEvent;

public sealed record LayerProgressEvent(
    string LayerId,
    string? Description,
    long? Current,
    long? Total,
    bool Completed
) : OrchestrationEvent;

public sealed record WarningEvent(string Message) : OrchestrationEvent;
