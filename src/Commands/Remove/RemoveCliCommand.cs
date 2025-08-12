using port.Commands.List;
using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Remove;

internal class RemoveCliCommand : AsyncCommand<RemoveSettings>
{
    private readonly IImageIdentifierPrompt _imageIdentifierPrompt;
    private readonly IGetImageIdQuery _getImageIdQuery;
    private readonly port.Config.Config _config;
    private readonly IImageIdentifierAndTagEvaluator _imageIdentifierAndTagEvaluator;
    private readonly IAllImagesQuery _allImagesQuery;
    private readonly IRemoveImagesCliDependentCommand _removeImagesCliDependentCommand;
    private readonly ConditionalListCliCommand _conditionalListCliCommand;

    public RemoveCliCommand(
        IImageIdentifierPrompt imageIdentifierPrompt,
        port.Config.Config config,
        IImageIdentifierAndTagEvaluator imageIdentifierAndTagEvaluator,
        IGetImageIdQuery getImageIdQuery,
        IAllImagesQuery allImagesQuery,
        IRemoveImagesCliDependentCommand removeImagesCliDependentCommand,
        ConditionalListCliCommand conditionalListCliCommand
    )
    {
        _imageIdentifierPrompt = imageIdentifierPrompt;
        _config = config;
        _imageIdentifierAndTagEvaluator = imageIdentifierAndTagEvaluator;
        _getImageIdQuery = getImageIdQuery;
        _allImagesQuery = allImagesQuery;
        _removeImagesCliDependentCommand = removeImagesCliDependentCommand;
        _conditionalListCliCommand = conditionalListCliCommand;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RemoveSettings settings)
    {
        var (identifier, tag) = await GetIdentifierAndTagAsync(settings);
        var result = await Spinner
            .StartAsync(
                $"Removing {ImageNameHelper.BuildImageName(identifier, tag)}",
                async ctx =>
                {
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

                    var imageIds = new List<string>();
                    if (settings.Recursive)
                    {
                        var images = (
                            await _allImagesQuery.QueryAllImagesWithParentAsync().ToListAsync()
                        )
                            .Where(e => e is { Id: not null, ParentId: not null })
                            .ToList();
                        var imageIdsToAnalyze = initialImageIds.ToHashSet();
                        while (imageIdsToAnalyze.Count != 0)
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
                        imageIds = initialImageIds.ToList();
                    }

                    if (imageIds.Count == 0)
                        throw new InvalidOperationException(
                            "No images to remove found".EscapeMarkup()
                        );
                    return _removeImagesCliDependentCommand.ExecuteAsync(imageIds, ctx);
                }
            )
            .Unwrap();
        foreach (
            var imageRemovalResult in result.Where(imageRemovalResult =>
                !imageRemovalResult.Successful
            )
        )
        {
            AnsiConsole.MarkupLine(
                $"[orange3]Unable to removed image with id '{imageRemovalResult.ImageId}'[/] because it has dependent children"
            );
            if (settings.Recursive)
                AnsiConsole.MarkupLine(
                    "That may be because an child image is based on an [red]unknown image[/] which can not be removed automatically, manually remove it and try again"
                );
        }

        await _conditionalListCliCommand.ExecuteAsync();

        return 0;
    }

    private async Task<(string identifier, string? tag)> GetIdentifierAndTagAsync(
        IImageIdentifierSettings settings
    )
    {
        if (settings.ImageIdentifier != null)
        {
            return _imageIdentifierAndTagEvaluator.Evaluate(settings.ImageIdentifier);
        }

        var identifierAndTag =
            await _imageIdentifierPrompt.GetDownloadedIdentifierAndTagFromUserAsync("remove");
        return (identifierAndTag.identifier, identifierAndTag.tag);
    }
}
