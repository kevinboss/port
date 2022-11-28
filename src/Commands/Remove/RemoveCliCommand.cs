using Spectre.Console;
using Spectre.Console.Cli;

namespace port.Commands.Remove;

internal class RemoveCliCommand : AsyncCommand<RemoveSettings>
{
    private readonly IImageIdentifierPrompt _imageIdentifierPrompt;
    private readonly IGetContainersQuery _getContainersQuery;
    private readonly IGetImageIdQuery _getImageIdQuery;
    private readonly IStopAndRemoveContainerCommand _stopAndRemoveContainerCommand;
    private readonly IRemoveImageCommand _removeImageCommand;
    private readonly Config.Config _config;
    private readonly IImageIdentifierAndTagEvaluator _imageIdentifierAndTagEvaluator;

    public RemoveCliCommand(IImageIdentifierPrompt imageIdentifierPrompt, IGetContainersQuery getContainersQuery,
        Config.Config config,
        IStopAndRemoveContainerCommand stopAndRemoveContainerCommand, IRemoveImageCommand removeImageCommand,
        IImageIdentifierAndTagEvaluator imageIdentifierAndTagEvaluator, IGetImageIdQuery getImageIdQuery)
    {
        _imageIdentifierPrompt = imageIdentifierPrompt;
        _getContainersQuery = getContainersQuery;
        _config = config;
        _stopAndRemoveContainerCommand = stopAndRemoveContainerCommand;
        _removeImageCommand = removeImageCommand;
        _imageIdentifierAndTagEvaluator = imageIdentifierAndTagEvaluator;
        _getImageIdQuery = getImageIdQuery;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RemoveSettings settings)
    {
        var (identifier, tag) = await GetIdentifierAndTagAsync(settings);
        var result = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Removing {ImageNameHelper.BuildImageName(identifier, tag)}",
                ctx => RemoveImageAsync(identifier, tag, ctx));
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

    private async Task<IEnumerable<ImageRemovalResult>> RemoveImageAsync(string identifier, string? tag,
        StatusContext ctx)
    {
        var imageConfig = _config.GetImageConfigByIdentifier(identifier);
        var imageName = imageConfig.ImageName;
        var containers = await _getContainersQuery.QueryByImageNameAndTagAsync(imageName, tag).ToListAsync();
        ctx.Status = $"Removing containers using image '{ImageNameHelper.BuildImageName(imageName, tag)}'";
        foreach (var container in containers)
        {
            await _stopAndRemoveContainerCommand.ExecuteAsync(container.Id);
        }

        ctx.Status = $"Containers using image '{ImageNameHelper.BuildImageName(imageName, tag)}' removed";

        var imageIds = (await _getImageIdQuery.QueryAsync(imageName, tag)).ToList();
        if (!imageIds.Any())
            throw new InvalidOperationException(
                $"No images for '{ImageNameHelper.BuildImageName(imageName, tag)}' do exist".EscapeMarkup());

        ctx.Status = $"Now removing {imageIds.Count} images";
        var result = new List<ImageRemovalResult>();
        foreach (var imageId in imageIds)
        {
            result.Add(await _removeImageCommand.ExecuteAsync(imageId));
        }

        return result;
    }
}