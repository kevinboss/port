using System.Reactive.Subjects;
using port.Commands.Commit;

namespace port.Orchestrators;

public class CommitOrchestrator : ICommitOrchestrator
{
    private readonly ICreateImageFromContainerCommand _createImageFromContainerCommand;
    private readonly IGetRunningContainersQuery _getRunningContainersQuery;
    private readonly IGetImageQuery _getImageQuery;
    private readonly IStopContainerCommand _stopContainerCommand;
    private readonly ICreateContainerCommand _createContainerCommand;
    private readonly IRunContainerCommand _runContainerCommand;
    private readonly IGetDigestsByIdQuery _getDigestsByIdQuery;
    private readonly IGetContainersQuery _getContainersQuery;
    private readonly IStopAndRemoveContainerCommand _stopAndRemoveContainerCommand;
    private readonly Subject<OrchestrationEvent> _events = new();

    public CommitOrchestrator(
        ICreateImageFromContainerCommand createImageFromContainerCommand,
        IGetRunningContainersQuery getRunningContainersQuery,
        IGetImageQuery getImageQuery,
        IStopContainerCommand stopContainerCommand,
        ICreateContainerCommand createContainerCommand,
        IRunContainerCommand runContainerCommand,
        IGetDigestsByIdQuery getDigestsByIdQuery,
        IGetContainersQuery getContainersQuery,
        IStopAndRemoveContainerCommand stopAndRemoveContainerCommand
    )
    {
        _createImageFromContainerCommand = createImageFromContainerCommand;
        _getRunningContainersQuery = getRunningContainersQuery;
        _getImageQuery = getImageQuery;
        _stopContainerCommand = stopContainerCommand;
        _createContainerCommand = createContainerCommand;
        _runContainerCommand = runContainerCommand;
        _getDigestsByIdQuery = getDigestsByIdQuery;
        _getContainersQuery = getContainersQuery;
        _stopAndRemoveContainerCommand = stopAndRemoveContainerCommand;
    }

    public IObservable<OrchestrationEvent> Events => _events;

    public async Task<CommitResult> ExecuteAsync(
        string containerName,
        string tag,
        bool overwrite,
        bool @switch,
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

        _events.OnNext(new StatusEvent("Committing container"));

        string newTag;
        string imageName;
        string tagPrefix;
        if (overwrite)
        {
            newTag =
                container.ImageTag
                ?? throw new InvalidOperationException(
                    "When using overwrite, container must have an image tag"
                );
            imageName = container.ImageIdentifier;
            tagPrefix = container.TagPrefix;
        }
        else
        {
            (imageName, tagPrefix, newTag) = await GetNewTagAsync(container, tag);
        }

        _events.OnNext(
            new StatusEvent($"Looking for existing container named '{container.ContainerName}'")
        );
        var containerWithSameTag = await _getContainersQuery
            .QueryByContainerIdentifierAndTagAsync(container.ContainerIdentifier, newTag)
            .ToListAsync(ct);

        _events.OnNext(
            new StatusEvent($"Creating image from running container '{container.ContainerName}'")
        );
        newTag = await _createImageFromContainerCommand.ExecuteAsync(
            container,
            imageName,
            tagPrefix,
            newTag
        );

        _events.OnNext(new StatusEvent($"Removing containers named '{container.ContainerName}'"));
        await Task.WhenAll(
            containerWithSameTag.Select(c => _stopAndRemoveContainerCommand.ExecuteAsync(c.Id))
        );

        if (overwrite)
        {
            if (container.ImageTag == null)
                throw new InvalidOperationException(
                    "Overwrite not supported when committing untagged container"
                );
            _events.OnNext(new StatusEvent("Launching new image"));
            var id = await _createContainerCommand.ExecuteAsync(container, tagPrefix, newTag);
            await _runContainerCommand.ExecuteAsync(id);
        }
        else if (@switch)
        {
            if (container.ImageTag == null)
                throw new InvalidOperationException(
                    "Switch not supported when committing untagged container"
                );
            _events.OnNext(
                new StatusEvent($"Stopping running container '{container.ContainerName}'")
            );
            await _stopContainerCommand.ExecuteAsync(container.Id);

            _events.OnNext(new StatusEvent("Launching new image"));
            var id = await _createContainerCommand.ExecuteAsync(container, tagPrefix, newTag);
            await _runContainerCommand.ExecuteAsync(id);
        }

        return new CommitResult(imageName, newTag);
    }

    private async Task<(string imageName, string tagPrefix, string newTag)> GetNewTagAsync(
        Container container,
        string tag
    )
    {
        var image = await _getImageQuery.QueryAsync(container.ImageIdentifier, container.ImageTag);
        string imageName;
        string? baseTag = null;
        if (image == null)
        {
            var digests = await _getDigestsByIdQuery.QueryAsync(container.ImageIdentifier);
            var digest = digests?.SingleOrDefault();
            if (digest == null || !DigestHelper.TryGetImageNameAndId(digest, out var nameAndId))
                throw new InvalidOperationException(
                    $"Unable to determine image name from running container '{container.ContainerName}'"
                );
            imageName = nameAndId.imageName;
        }
        else
        {
            imageName = image.Name;
            baseTag = image.BaseImage?.Tag;
        }

        baseTag = container.GetLabel(Constants.BaseTagLabel) ?? baseTag ?? image?.Tag;
        var tagPrefix = container.TagPrefix;
        var newTag = baseTag == null ? tag : $"{tagPrefix}{baseTag}-{tag}";
        return (imageName, tagPrefix, newTag);
    }
}
