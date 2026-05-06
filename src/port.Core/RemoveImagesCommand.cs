using port.Orchestrators;

namespace port;

public class RemoveImagesCommand : IRemoveImagesCommand
{
    private readonly IGetContainersQuery _getContainersQuery;
    private readonly IStopAndRemoveContainerCommand _stopAndRemoveContainerCommand;
    private readonly IRemoveImageCommand _removeImageCommand;

    public RemoveImagesCommand(
        IGetContainersQuery getContainersQuery,
        IStopAndRemoveContainerCommand stopAndRemoveContainerCommand,
        IRemoveImageCommand removeImageCommand
    )
    {
        _getContainersQuery = getContainersQuery;
        _stopAndRemoveContainerCommand = stopAndRemoveContainerCommand;
        _removeImageCommand = removeImageCommand;
    }

    public async Task<List<ImageRemovalResult>> ExecuteAsync(
        List<string> imageIds,
        IObserver<OrchestrationEvent>? events = null,
        CancellationToken ct = default
    )
    {
        var result = new List<ImageRemovalResult>();
        foreach (var imageId in imageIds)
        {
            ct.ThrowIfCancellationRequested();
            var containers = await _getContainersQuery.QueryByImageIdAsync(imageId).ToListAsync(ct);
            events?.OnNext(new StatusEvent($"Removing containers using '{imageId}'"));
            foreach (var container in containers)
            {
                ct.ThrowIfCancellationRequested();
                await _stopAndRemoveContainerCommand.ExecuteAsync(container.Id);
            }

            events?.OnNext(new StatusEvent($"Containers using '{imageId}' removed"));
            result.Add(await _removeImageCommand.ExecuteAsync(imageId));
        }

        return result;
    }
}
