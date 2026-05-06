using System.Reactive.Subjects;

namespace port.Orchestrators;

public class RemoveOrchestrator : IRemoveOrchestrator
{
    private readonly port.Config.Config _config;
    private readonly IGetImageIdQuery _getImageIdQuery;
    private readonly IAllImagesQuery _allImagesQuery;
    private readonly IRemoveImagesCommand _removeImagesCommand;
    private readonly Subject<OrchestrationEvent> _events = new();

    public RemoveOrchestrator(
        port.Config.Config config,
        IGetImageIdQuery getImageIdQuery,
        IAllImagesQuery allImagesQuery,
        IRemoveImagesCommand removeImagesCommand
    )
    {
        _config = config;
        _getImageIdQuery = getImageIdQuery;
        _allImagesQuery = allImagesQuery;
        _removeImagesCommand = removeImagesCommand;
    }

    public IObservable<OrchestrationEvent> Events => _events;

    public async Task<RemoveResult> ExecuteAsync(
        string identifier,
        string? tag,
        bool recursive,
        CancellationToken ct = default
    )
    {
        _events.OnNext(new StatusEvent($"Removing {ImageNameHelper.BuildImageName(identifier, tag)}"));
        var imageConfig = _config.GetImageConfigByIdentifier(identifier);
        var imageName = imageConfig.ImageName;

        var initialImageIds = new List<string>();
        if (tag is not null && !imageConfig.ImageTags.Contains(tag))
        {
            initialImageIds.AddRange(
                await _getImageIdQuery.QueryAsync(
                    imageName,
                    $"{TagPrefixHelper.GetTagPrefix(identifier)}{tag}"
                )
            );
        }

        initialImageIds.AddRange(await _getImageIdQuery.QueryAsync(imageName, tag));

        var imageIds = recursive
            ? await ResolveRecursiveAsync(initialImageIds, ct)
            : initialImageIds.ToList();

        if (imageIds.Count == 0)
            throw new InvalidOperationException("No images to remove found");

        var removals = await _removeImagesCommand.ExecuteAsync(imageIds, _events, ct);
        return new RemoveResult(removals);
    }

    private async Task<List<string>> ResolveRecursiveAsync(
        List<string> initialImageIds,
        CancellationToken ct
    )
    {
        var images = await _allImagesQuery.QueryAllImagesWithParentAsync().ToListAsync(ct);

        var imageIds = new List<string>();
        var imageIdsToAnalyze = initialImageIds.ToHashSet();
        while (imageIdsToAnalyze.Count != 0)
        {
            imageIds.AddRange(imageIdsToAnalyze);
            var analyze = imageIdsToAnalyze;
            imageIdsToAnalyze = images
                .Where(e => analyze.Contains(e.ParentId))
                .Select(e => e.Id)
                .ToHashSet();
        }

        imageIds.Reverse();
        return imageIds;
    }
}
