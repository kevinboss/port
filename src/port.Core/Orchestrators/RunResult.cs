namespace port.Orchestrators;

public sealed record RunResult(
    string Identifier,
    string Tag,
    string ContainerId,
    string ContainerName
);
