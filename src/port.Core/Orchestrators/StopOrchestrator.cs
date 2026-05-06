using System.Reactive.Subjects;

namespace port.Orchestrators;

public class StopOrchestrator : IStopOrchestrator
{
    private readonly IGetRunningContainersQuery _getRunningContainersQuery;
    private readonly IStopContainerCommand _stopContainerCommand;
    private readonly Subject<OrchestrationEvent> _events = new();

    public StopOrchestrator(
        IGetRunningContainersQuery getRunningContainersQuery,
        IStopContainerCommand stopContainerCommand
    )
    {
        _getRunningContainersQuery = getRunningContainersQuery;
        _stopContainerCommand = stopContainerCommand;
    }

    public IObservable<OrchestrationEvent> Events => _events;

    public async Task<StopResult> ExecuteAsync(
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

        _events.OnNext(new StatusEvent($"Stopping container '{container.ContainerName}'"));
        await _stopContainerCommand.ExecuteAsync(container.Id);
        return new StopResult(container.Id, container.ContainerName);
    }
}
