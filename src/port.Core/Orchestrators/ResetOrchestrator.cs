using System.Reactive.Subjects;

namespace port.Orchestrators;

public class ResetOrchestrator : IResetOrchestrator
{
    private readonly IGetRunningContainersQuery _getRunningContainersQuery;
    private readonly IStopAndRemoveContainerCommand _stopAndRemoveContainerCommand;
    private readonly ICreateContainerCommand _createContainerCommand;
    private readonly IRunContainerCommand _runContainerCommand;
    private readonly Subject<OrchestrationEvent> _events = new();

    public ResetOrchestrator(
        IGetRunningContainersQuery getRunningContainersQuery,
        IStopAndRemoveContainerCommand stopAndRemoveContainerCommand,
        ICreateContainerCommand createContainerCommand,
        IRunContainerCommand runContainerCommand
    )
    {
        _getRunningContainersQuery = getRunningContainersQuery;
        _stopAndRemoveContainerCommand = stopAndRemoveContainerCommand;
        _createContainerCommand = createContainerCommand;
        _runContainerCommand = runContainerCommand;
    }

    public IObservable<OrchestrationEvent> Events => _events;

    public async Task<ResetResult> ExecuteAsync(
        string containerName,
        CancellationToken ct = default
    )
    {
        _events.OnNext(new StatusEvent("Getting running containers"));
        var containers = await _getRunningContainersQuery.QueryAsync().ToListAsync(ct);
        var container =
            containers.SingleOrDefault(c => c.ContainerName == containerName)
            ?? throw new InvalidOperationException(
                $"No running container named '{containerName}' found"
            );

        _events.OnNext(new StatusEvent($"Resetting container '{container.ContainerName}'"));
        await _stopAndRemoveContainerCommand.ExecuteAsync(container.Id);
        var id = await _createContainerCommand.ExecuteAsync(container);
        await _runContainerCommand.ExecuteAsync(id);
        return new ResetResult(id, container.ContainerName);
    }
}
