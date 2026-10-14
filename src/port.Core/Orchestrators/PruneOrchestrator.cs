using System.Reactive.Subjects;

namespace port.Orchestrators;

public class PruneOrchestrator : IPruneOrchestrator
{
    private readonly IAllImagesQuery _allImagesQuery;
    private readonly IRemoveImagesCommand _removeImagesCommand;
    private readonly port.Config.Config _config;
    private readonly Subject<OrchestrationEvent> _events = new();

    public PruneOrchestrator(
        IAllImagesQuery allImagesQuery,
        IRemoveImagesCommand removeImagesCommand,
        port.Config.Config config
    )
    {
        _allImagesQuery = allImagesQuery;
        _removeImagesCommand = removeImagesCommand;
        _config = config;
    }

    public IObservable<OrchestrationEvent> Events => _events;

    public async Task<PruneResult> ExecuteAsync(
        string? identifier,
        CancellationToken cancellationToken
    )
    {
        var imageGroups = await _allImagesQuery
            .QueryAsync()
            .Where(g => identifier == null || g.Identifier == identifier)
            .ToListAsync(cancellationToken);

        var configuredDigests = _config
            .ImageConfigs.SelectMany(c =>
                c.ImageTags.Where(ImageNameHelper.IsDigest)
                    .Select(t => ImageNameHelper.BuildImageName(c.ImageName, t))
            )
            .ToHashSet();
        var pruneableImages = imageGroups
            .SelectMany(g => g.Images)
            .Where(i => i.Existing && i.Tag != null && ImageNameHelper.IsDigest(i.Tag))
            .Where(i => !configuredDigests.Contains(ImageNameHelper.BuildImageName(i.Name, i.Tag)))
            .ToList();

        if (pruneableImages.Count == 0)
            return new PruneResult([]);

        _events.OnNext(new StatusEvent("Pruning dangling images"));
        var removals = await _removeImagesCommand.ExecuteAsync(
            pruneableImages.Select(i => i.Id!).ToList(),
            _events,
            cancellationToken
        );
        return new PruneResult(removals);
    }
}
