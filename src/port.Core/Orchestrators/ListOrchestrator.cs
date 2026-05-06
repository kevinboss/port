using System.Reactive.Subjects;

namespace port.Orchestrators;

public class ListOrchestrator : IListOrchestrator
{
    private readonly IAllImagesQuery _allImagesQuery;
    private readonly Subject<OrchestrationEvent> _events = new();

    public ListOrchestrator(IAllImagesQuery allImagesQuery)
    {
        _allImagesQuery = allImagesQuery;
    }

    public IObservable<OrchestrationEvent> Events => _events;

    public async Task<ListResult> ExecuteAsync(string? identifier, CancellationToken ct = default)
    {
        _events.OnNext(new StatusEvent("Loading images"));
        var groups = (await _allImagesQuery.QueryAsync().ToListAsync(ct))
            .Where(g => identifier == null || g.Identifier == identifier)
            .OrderBy(g => g.Identifier)
            .ToList();
        return new ListResult(groups);
    }
}
