using System.ComponentModel;
using ModelContextProtocol.Server;
using port.Orchestrators;

namespace port.Mcp;

[McpServerToolType]
public static class PortMcpTools
{
    [McpServerTool(Name = "run")]
    [Description(
        "Launch a configured Docker image as a container. Stops any container using the same host ports first. Both identifier and tag are required."
    )]
    public static Task<McpToolResponse<RunResult>> RunAsync(
        IRunOrchestrator orchestrator,
        [Description("Image identifier as defined in port config (e.g. 'Getting.Started')")]
            string identifier,
        [Description("Image tag to launch (e.g. 'latest')")] string tag,
        [Description("Recreate the container instead of restarting it")] bool reset = false,
        CancellationToken cancellationToken = default
    ) =>
        EventCollector.InvokeAsync(
            orchestrator.Events,
            () => orchestrator.ExecuteAsync(identifier, tag, reset, cancellationToken)
        );

    [McpServerTool(Name = "stop")]
    [Description("Stop a running container by its container name (e.g. 'Getting.Started.latest').")]
    public static Task<McpToolResponse<StopResult>> StopAsync(
        IStopOrchestrator orchestrator,
        [Description("Exact container name as shown by the list tool")] string containerName,
        CancellationToken cancellationToken = default
    ) =>
        EventCollector.InvokeAsync(
            orchestrator.Events,
            () => orchestrator.ExecuteAsync(containerName, cancellationToken)
        );

    [McpServerTool(Name = "reset")]
    [Description(
        "Recreate a running container from its image, discarding any in-container state. Container name is required (no auto-pick)."
    )]
    public static Task<McpToolResponse<ResetResult>> ResetAsync(
        IResetOrchestrator orchestrator,
        [Description("Exact container name to reset")] string containerName,
        CancellationToken cancellationToken = default
    ) =>
        EventCollector.InvokeAsync(
            orchestrator.Events,
            () => orchestrator.ExecuteAsync(containerName, cancellationToken)
        );

    [McpServerTool(Name = "commit")]
    [Description(
        "Create a snapshot image from a running container. The tag is required (no timestamp default)."
    )]
    public static Task<McpToolResponse<CommitResult>> CommitAsync(
        ICommitOrchestrator orchestrator,
        [Description("Exact container name to commit")] string containerName,
        [Description("Tag for the new snapshot image")] string tag,
        [Description("Overwrite the container's current tag")] bool overwrite = false,
        [Description("Stop the source container and switch to the new image")] bool @switch = false,
        CancellationToken cancellationToken = default
    ) =>
        EventCollector.InvokeAsync(
            orchestrator.Events,
            () => orchestrator.ExecuteAsync(containerName, tag, overwrite, @switch, cancellationToken)
        );

    [McpServerTool(Name = "pull")]
    [Description("Pull a configured image from its registry. Returns a summary, not per-layer events.")]
    public static Task<PullResult> PullAsync(
        IPullOrchestrator orchestrator,
        [Description("Image identifier as defined in port config")] string identifier,
        [Description("Optional image tag; pulls the configured tags if omitted")] string? tag = null,
        CancellationToken cancellationToken = default
    ) => EventCollector.InvokeAsync(() => orchestrator.ExecuteAsync(identifier, tag, cancellationToken));

    [McpServerTool(Name = "remove")]
    [Description(
        "Remove an image (and its dependent containers). Use recursive to also remove descendant snapshot images."
    )]
    public static Task<McpToolResponse<RemoveResult>> RemoveAsync(
        IRemoveOrchestrator orchestrator,
        [Description("Image identifier as defined in port config")] string identifier,
        [Description("Optional tag; if omitted, removes images for the configured tag")] string? tag = null,
        [Description("Also remove descendant snapshot images")] bool recursive = false,
        CancellationToken cancellationToken = default
    ) =>
        EventCollector.InvokeAsync(
            orchestrator.Events,
            () => orchestrator.ExecuteAsync(identifier, tag, recursive, cancellationToken)
        );

    [McpServerTool(Name = "prune")]
    [Description("Remove all dangling (digest-only) images. Optionally restrict to a single identifier.")]
    public static Task<McpToolResponse<PruneResult>> PruneAsync(
        IPruneOrchestrator orchestrator,
        [Description("Optional image identifier to restrict pruning")] string? identifier = null,
        CancellationToken cancellationToken = default
    ) =>
        EventCollector.InvokeAsync(
            orchestrator.Events,
            () => orchestrator.ExecuteAsync(identifier, cancellationToken)
        );

    [McpServerTool(Name = "list")]
    [Description(
        "List all configured images and their tags, snapshots, dangling images, and running containers."
    )]
    public static Task<ListResult> ListAsync(
        IListOrchestrator orchestrator,
        [Description("Optional image identifier to restrict the listing")] string? identifier = null,
        CancellationToken cancellationToken = default
    ) => EventCollector.InvokeAsync(() => orchestrator.ExecuteAsync(identifier, cancellationToken));

    [McpServerTool(Name = "config")]
    [Description("Return the absolute path to port's config file.")]
    public static ConfigResult Config(IConfigOrchestrator orchestrator) =>
        EventCollector.Invoke(orchestrator.Execute);
}
