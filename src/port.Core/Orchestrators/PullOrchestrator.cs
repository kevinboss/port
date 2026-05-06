using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace port.Orchestrators;

public class PullOrchestrator : IPullOrchestrator
{
    private readonly port.Config.Config _config;
    private readonly ICreateImageCommand _createImageCommand;
    private readonly Subject<OrchestrationEvent> _events = new();

    public PullOrchestrator(port.Config.Config config, ICreateImageCommand createImageCommand)
    {
        _config = config;
        _createImageCommand = createImageCommand;
    }

    public IObservable<OrchestrationEvent> Events => _events;

    public async Task<PullResult> ExecuteAsync(
        string identifier,
        string? tag,
        CancellationToken ct = default
    )
    {
        var imageConfig =
            _config.GetImageConfigByIdentifier(identifier)
            ?? throw new ArgumentException(
                $"There is no config defined for identifier '{identifier}'",
                nameof(identifier)
            );

        var imageName = imageConfig.ImageName;
        _events.OnNext(new StatusEvent($"Pulling {ImageNameHelper.BuildImageName(identifier, tag)}"));

        using var subscription = _createImageCommand.ProgressObservable.Subscribe(
            progress => _events.OnNext(ToLayerEvent(progress, tag)),
            error => _events.OnError(error)
        );

        await _createImageCommand.ExecuteAsync(imageName, tag);
        return new PullResult(imageName, tag);
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
