using System.Reactive.Subjects;

namespace port.Orchestrators;

public class RunOrchestrator : IRunOrchestrator
{
    private const char PortSeparator = ':';

    private readonly port.Config.Config _config;
    private readonly IGetImageQuery _getImageQuery;
    private readonly IGetContainersQuery _getContainersQuery;
    private readonly ICreateImageCommand _createImageCommand;
    private readonly ICreateContainerCommand _createContainerCommand;
    private readonly IRunContainerCommand _runContainerCommand;
    private readonly IStopContainerCommand _stopContainerCommand;
    private readonly IStopAndRemoveContainerCommand _stopAndRemoveContainerCommand;
    private readonly IRenameContainerCommand _renameContainerCommand;
    private readonly Subject<OrchestrationEvent> _events = new();

    public RunOrchestrator(
        port.Config.Config config,
        IGetImageQuery getImageQuery,
        IGetContainersQuery getContainersQuery,
        ICreateImageCommand createImageCommand,
        ICreateContainerCommand createContainerCommand,
        IRunContainerCommand runContainerCommand,
        IStopContainerCommand stopContainerCommand,
        IStopAndRemoveContainerCommand stopAndRemoveContainerCommand,
        IRenameContainerCommand renameContainerCommand
    )
    {
        _config = config;
        _getImageQuery = getImageQuery;
        _getContainersQuery = getContainersQuery;
        _createImageCommand = createImageCommand;
        _createContainerCommand = createContainerCommand;
        _runContainerCommand = runContainerCommand;
        _stopContainerCommand = stopContainerCommand;
        _stopAndRemoveContainerCommand = stopAndRemoveContainerCommand;
        _renameContainerCommand = renameContainerCommand;
    }

    public IObservable<OrchestrationEvent> Events => _events;

    public async Task<RunResult> ExecuteAsync(
        string identifier,
        string tag,
        bool reset,
        CancellationToken ct = default
    )
    {
        var imageConfig =
            _config.GetImageConfigByIdentifier(identifier)
            ?? throw new ArgumentException(
                $"There is no config defined for identifier '{identifier}'",
                nameof(identifier)
            );

        await TerminateOtherContainersAsync(imageConfig, ct);
        return await LaunchImageAsync(identifier, tag, reset, imageConfig, ct);
    }

    private async Task TerminateOtherContainersAsync(
        port.Config.Config.ImageConfig imageConfig,
        CancellationToken ct
    )
    {
        var hostPorts = imageConfig.Ports.Select(e => e.Split(PortSeparator)[0]).ToList();
        _events.OnNext(
            new StatusEvent(
                $"Terminating containers using host ports '{string.Join(", ", hostPorts)}'"
            )
        );
        var containers = GetRunningContainersUsingHostPortsAsync(hostPorts);
        await foreach (var container in containers.WithCancellation(ct))
            await _stopContainerCommand.ExecuteAsync(container.Id);
    }

    private IAsyncEnumerable<Container> GetRunningContainersUsingHostPortsAsync(
        IEnumerable<string> hostPorts
    )
    {
        return _getContainersQuery
            .QueryRunningAsync()
            .Where(container =>
            {
                if (container.PortBindings is null)
                    return false;
                var usedHostPorts = container.PortBindings.SelectMany(pb =>
                    pb.Value.Select(hp => hp.HostPort)
                );
                return container.PortBindings.Any(_ =>
                    hostPorts.Any(p => usedHostPorts.Contains(p))
                );
            });
    }

    private async Task<RunResult> LaunchImageAsync(
        string identifier,
        string tag,
        bool resetContainer,
        port.Config.Config.ImageConfig imageConfig,
        CancellationToken ct
    )
    {
        var constructedImageName = ImageNameHelper.BuildImageName(identifier, tag);
        var imageName = imageConfig.ImageName;
        var containerName = ContainerNameHelper.BuildContainerName(identifier, tag);

        _events.OnNext(new StatusEvent($"Query existing image: {constructedImageName}"));
        var existingImage = await QueryExistingAsync(imageConfig, imageName, identifier, tag);
        var resolvedTag = existingImage?.Tag ?? tag;

        if (existingImage is null)
        {
            await PullImageAsync(imageName, tag);
            _events.OnNext(new StatusEvent($"Re-query existing image: {constructedImageName}"));
            existingImage = await _getImageQuery.QueryAsync(imageName, tag);
            resolvedTag = existingImage?.Tag ?? tag;
        }

        var tagPrefix = existingImage?.GetLabel(Constants.TagPrefix);

        _events.OnNext(new StatusEvent($"Launching {constructedImageName}"));
        var containers = await _getContainersQuery
            .QueryByContainerNameAsync(containerName)
            .ToListAsync(ct);
        var ports = imageConfig.Ports;
        var environment = imageConfig.Environment;

        string runningId;
        if (containers.Count == 1)
        {
            var container = containers.Single();
            var imageChanged = existingImage?.Id != null && container.ImageId != existingImage.Id;
            if (resetContainer)
            {
                await _stopAndRemoveContainerCommand.ExecuteAsync(container.Id);
                runningId = await _createContainerCommand.ExecuteAsync(
                    identifier,
                    imageName,
                    tagPrefix,
                    resolvedTag,
                    ports,
                    environment
                );
                await _runContainerCommand.ExecuteAsync(runningId);
            }
            else if (imageChanged)
            {
                await _stopContainerCommand.ExecuteAsync(container.Id);
                var renamedContainerName = ContainerNameHelper.BuildContainerName(
                    identifier,
                    container.ImageId
                );
                await _renameContainerCommand.ExecuteAsync(container.Id, renamedContainerName);
                runningId = await _createContainerCommand.ExecuteAsync(
                    identifier,
                    imageName,
                    tagPrefix,
                    resolvedTag,
                    ports,
                    environment
                );
                await _runContainerCommand.ExecuteAsync(runningId);
            }
            else
            {
                runningId = container.Id;
                await _runContainerCommand.ExecuteAsync(runningId);
            }
        }
        else
        {
            runningId = await _createContainerCommand.ExecuteAsync(
                identifier,
                imageName,
                tagPrefix,
                resolvedTag,
                ports,
                environment
            );
            await _runContainerCommand.ExecuteAsync(runningId);
        }

        return new RunResult(identifier, tag, runningId, containerName);
    }

    private async Task<Image?> QueryExistingAsync(
        port.Config.Config.ImageConfig imageConfig,
        string imageName,
        string identifier,
        string tag
    )
    {
        if (imageConfig.ImageTags.Contains(tag))
            return await _getImageQuery.QueryAsync(imageName, tag);
        var existing = await _getImageQuery.QueryAsync(imageName, tag);
        if (existing is not null)
            return existing;
        var prefixed = $"{TagPrefixHelper.GetTagPrefix(identifier)}{tag}";
        return await _getImageQuery.QueryAsync(imageName, prefixed);
    }

    private async Task PullImageAsync(string imageName, string? tag)
    {
        using var subscription = _createImageCommand.ProgressObservable.Subscribe(
            progress => _events.OnNext(ToLayerEvent(progress, tag)),
            error => _events.OnError(error)
        );
        await _createImageCommand.ExecuteAsync(imageName, tag);
    }

    private static LayerProgressEvent ToLayerEvent(Progress progress, string? requestedTag)
    {
        var layerId = progress.Id == requestedTag ? Progress.NullId : progress.Id;
        var completed = progress.ProgressState == ProgressState.Finished;
        return new LayerProgressEvent(
            layerId,
            progress.Description,
            progress.CurrentProgress,
            progress.TotalProgress,
            completed
        );
    }
}
