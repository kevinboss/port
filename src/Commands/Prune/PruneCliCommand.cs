using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Prune;

internal class PruneCliCommand : AsyncCommand<PruneSettings>
{
    private readonly IImageIdentifierAndTagEvaluator _imageIdentifierAndTagEvaluator;
    private readonly IGetImageIdQuery _getImageIdQuery;
    private readonly Config.Config _config;
    private readonly IAllImagesQuery _allImagesQuery;
    private readonly IRemoveImagesCliDependentCommand _removeImagesCliDependentCommand;

    public PruneCliCommand(IImageIdentifierAndTagEvaluator imageIdentifierAndTagEvaluator,
        IGetImageIdQuery getImageIdQuery, Config.Config config,
        IAllImagesQuery allImagesQuery,
        IRemoveImagesCliDependentCommand removeImagesCliDependentCommand)
    {
        _imageIdentifierAndTagEvaluator = imageIdentifierAndTagEvaluator;
        _getImageIdQuery = getImageIdQuery;
        _config = config;
        _allImagesQuery = allImagesQuery;
        _removeImagesCliDependentCommand = removeImagesCliDependentCommand;
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
                "No images to remove found".EscapeMarkup());
        return await _removeImagesCliDependentCommand.ExecuteAsync(imageIds, ctx);
    }
}