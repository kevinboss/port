using System.Reactive.Subjects;

namespace port.Orchestrators;

public class PruneOrchestrator : IPruneOrchestrator
{
    private readonly IAllImagesQuery _allImagesQuery;
    private readonly IRemoveImagesCommand _removeImagesCommand;
    private readonly Subject<OrchestrationEvent> _events = new();

    public PruneOrchestrator(
        IAllImagesQuery allImagesQuery,
        IRemoveImagesCommand removeImagesCommand
    )
    {
        _allImagesQuery = allImagesQuery;
        _removeImagesCommand = removeImagesCommand;
    }

    public IObservable<OrchestrationEvent> Events => _events;

    public async Task<PruneResult> ExecuteAsync(
        string? identifier,
        CancellationToken ct = default
    )
    {
        var imageGroups = await _allImagesQuery
            .QueryAsync()
            .Where(g => identifier == null || g.Identifier == identifier)
            .ToListAsync(ct);

        var pruneableImages = imageGroups
            .SelectMany(g => g.Images)
            .Where(i => i.Existing && i.Tag != null && ImageNameHelper.IsDigest(i.Tag))
            .ToList();

        if (pruneableImages.Count == 0)
            return new PruneResult([]);

        _events.OnNext(new StatusEvent("Pruning dangling images"));
        var removals = await _removeImagesCommand.ExecuteAsync(
            pruneableImages.Select(i => i.Id!).ToList(),
            _events,
            ct
        );
        return new PruneResult(removals);
    }
}
