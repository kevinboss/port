using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Remove;

internal class RemoveCliCommand : AsyncCommand<RemoveSettings>
{
    private readonly IImageIdentifierPrompt _imageIdentifierPrompt;
    private readonly IGetImageIdQuery _getImageIdQuery;
    private readonly Config.Config _config;
    private readonly IImageIdentifierAndTagEvaluator _imageIdentifierAndTagEvaluator;
    private readonly IAllImagesQuery _allImagesQuery;
    private readonly IRemoveImagesCliDependentCommand _removeImagesCliDependentCommand;

    public RemoveCliCommand(IImageIdentifierPrompt imageIdentifierPrompt, Config.Config config,
        IImageIdentifierAndTagEvaluator imageIdentifierAndTagEvaluator, IGetImageIdQuery getImageIdQuery,
        IAllImagesQuery allImagesQuery, IRemoveImagesCliDependentCommand removeImagesCliDependentCommand)
    {
        _imageIdentifierPrompt = imageIdentifierPrompt;
        _config = config;
        _imageIdentifierAndTagEvaluator = imageIdentifierAndTagEvaluator;
        _getImageIdQuery = getImageIdQuery;
        _allImagesQuery = allImagesQuery;
        _removeImagesCliDependentCommand = removeImagesCliDependentCommand;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RemoveSettings settings)
    {
        var (identifier, tag) = await GetIdentifierAndTagAsync(settings);
        var result = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Removing {ImageNameHelper.BuildImageName(identifier, tag)}",
                async ctx =>
                {
                    var imageConfig = _config.GetImageConfigByIdentifier(identifier);
                    var imageName = imageConfig.ImageName;
                    var imageIds = new List<string>();
                    if (settings.Reset)
                    {
                        var images = (await _allImagesQuery.QueryByImageConfigAsync(imageConfig))
                            .Where(e => e.Id != null && e.ParentId != null)
                            .Select(e => new
                            {
                                Id = e.Id!,
                                ParentId = e.ParentId!
                            })
                            .ToList();
                        var imageIdsToAnalyze = (await _getImageIdQuery.QueryAsync(imageName, tag)).ToHashSet();
                        while (imageIdsToAnalyze.Any())
                        {
                            imageIds.AddRange(imageIdsToAnalyze);
                            var analyze = imageIdsToAnalyze;
                            var childImageIds = images
                                .Where(e => analyze.Contains(e.ParentId))
                                .Select(e => e.Id)
                                .ToHashSet();
                            imageIdsToAnalyze = childImageIds;
                        }

                        imageIds.Reverse();
                    }
                    else
                    {
                        imageIds = (await _getImageIdQuery.QueryAsync(imageName, tag)).ToList();
                    }

                    if (!imageIds.Any())
                        throw new InvalidOperationException(
                            "No images to remove found".EscapeMarkup());
                    return _removeImagesCliDependentCommand.ExecuteAsync(imageIds, ctx);
                }).Unwrap();
        foreach (var imageRemovalResult in result)
        {
            if (imageRemovalResult.Successful)
                AnsiConsole.WriteLine($"Removed image with id '{imageRemovalResult.ImageId}'");
            else
                AnsiConsole.MarkupLine(
                    $"[orange3]Unable to removed image with id '{imageRemovalResult.ImageId}'[/] because it has dependent child images");
        }

        return 0;
    }

    private async Task<(string identifier, string? tag)> GetIdentifierAndTagAsync(IImageIdentifierSettings settings)
    {
        if (settings.ImageIdentifier != null)
        {
            return _imageIdentifierAndTagEvaluator.Evaluate(settings.ImageIdentifier);
        }

        var identifierAndTag = await _imageIdentifierPrompt.GetDownloadedIdentifierAndTagFromUserAsync("remove");
        return (identifierAndTag.identifier, identifierAndTag.tag);
    }
}