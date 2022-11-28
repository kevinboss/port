using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Prune;

internal class PruneCliCommand : AsyncCommand<PruneSettings>
{
    private readonly IImageIdentifierAndTagEvaluator _imageIdentifierAndTagEvaluator;
    private readonly IImageIdentifierPrompt _imageIdentifierPrompt;
    private readonly IGetImageIdQuery _getImageIdQuery;
    private readonly Config.Config _config;
    private readonly IRemoveImageCommand _removeImageCommand;
    private readonly IGetContainersQuery _getContainersQuery;
    private readonly IStopAndRemoveContainerCommand _stopAndRemoveContainerCommand;
    private readonly IAllImagesQuery _allImagesQuery;

    public PruneCliCommand(IImageIdentifierAndTagEvaluator imageIdentifierAndTagEvaluator,
        IImageIdentifierPrompt imageIdentifierPrompt, IGetImageIdQuery getImageIdQuery, Config.Config config,
        IRemoveImageCommand removeImageCommand, IGetContainersQuery getContainersQuery,
        IStopAndRemoveContainerCommand stopAndRemoveContainerCommand, IAllImagesQuery allImagesQuery)
    {
        _imageIdentifierAndTagEvaluator = imageIdentifierAndTagEvaluator;
        _imageIdentifierPrompt = imageIdentifierPrompt;
        _getImageIdQuery = getImageIdQuery;
        _config = config;
        _removeImageCommand = removeImageCommand;
        _getContainersQuery = getContainersQuery;
        _stopAndRemoveContainerCommand = stopAndRemoveContainerCommand;
        _allImagesQuery = allImagesQuery;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, PruneSettings settings)
    {
        var identifiers = GetIdentifiersAsync(settings);
        await foreach (var identifier in identifiers)
        {
            var result = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"Removing untagged images for identifier '{identifier}'",
                    ctx => RemoveUntaggedImagesAsync(identifier, ctx));
            foreach (var imageRemovalResult in result)
            {
                if (imageRemovalResult.Successful)
                    AnsiConsole.WriteLine($"Removed image with id '{imageRemovalResult.ImageId}'");
                else
                    AnsiConsole.MarkupLine(
                        $"[orange3]Unable to removed image with id '{imageRemovalResult.ImageId}'[/] because it has dependent child images");
            }
        }


        return 0;
    }

    private async IAsyncEnumerable<string> GetIdentifiersAsync(IImageIdentifierSettings settings)
    {
        if (settings.ImageIdentifier != null)
        {
            yield return _imageIdentifierAndTagEvaluator.Evaluate(settings.ImageIdentifier).identifier;
        }

        await foreach (var identifier in _allImagesQuery
                           .QueryAsync()
                           .Where(e => e.Images.Any(i => i.Tag == null))
                           .Select(e => e.Identifier))
        {
            yield return identifier;
        }
    }

    private async Task<List<ImageRemovalResult>> RemoveUntaggedImagesAsync(string identifier, StatusContext ctx)
    {
        var imageConfig = _config.GetImageConfigByIdentifier(identifier);
        var imageName = imageConfig.ImageName;
        var imageIds = (await _getImageIdQuery.QueryAsync(imageName, null)).ToList();
        if (!imageIds.Any())
            throw new InvalidOperationException(
                $"No images for '{identifier}:<none>' do exist".EscapeMarkup());

        var result = new List<ImageRemovalResult>();
        foreach (var imageId in imageIds)
        {
            var containers = await _getContainersQuery.QueryByImageIdAsync(imageId).ToListAsync();
            ctx.Status = $"Removing containers using '{imageId}'".EscapeMarkup();
            foreach (var container in containers)
            {
                await _stopAndRemoveContainerCommand.ExecuteAsync(container.Id);
            }

            ctx.Status = $"Containers using '{imageId}' removed".EscapeMarkup();

            result.Add(await _removeImageCommand.ExecuteAsync(imageId));
        }

        return result;
    }
}